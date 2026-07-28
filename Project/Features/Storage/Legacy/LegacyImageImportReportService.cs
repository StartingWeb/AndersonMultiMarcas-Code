using System.Globalization;
using System.Text;
using System.Text.Json;
using Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Project.Shared;

namespace Project.Features.Storage.Legacy;

public sealed class LegacyImageImportReportService(ApplicationDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<LegacyImageImportDashboardSnapshot> GetDashboardAsync(LegacyImageImportFilters filters, CancellationToken ct)
    {
        var jobs = await ApplyJobFilters(db.ImportJobs.AsNoTracking(), filters).ToListAsync(ct);
        var jobIds = jobs.Select(x => x.Id).ToArray();
        var itemsQuery = ApplyItemFilters(db.ImportJobItems.AsNoTracking().Where(x => jobIds.Contains(x.ImportJobId)), filters);

        var totalItems = await itemsQuery.CountAsync(ct);
        var imported = await itemsQuery.CountAsync(x => x.Status == LegacyImageImportItemStatus.Completed, ct);
        var ignored = await itemsQuery.CountAsync(x => x.Status == LegacyImageImportItemStatus.Ignored, ct);
        var failed = await itemsQuery.CountAsync(x => x.Status == LegacyImageImportItemStatus.Failed || x.Status == LegacyImageImportItemStatus.Review, ct);
        var pending = await itemsQuery.CountAsync(x => x.Status == LegacyImageImportItemStatus.Pending || x.Status == LegacyImageImportItemStatus.Running, ct);
        var retries = await itemsQuery.SumAsync(x => Math.Max(0, x.Tentativas - 1), ct);

        var activeJobs = jobs.Count(x => LegacyImageImportJobStatus.Active.Contains(x.Status));
        var elapsed = SumElapsed(jobs);
        var vehiclesProcessed = jobs.Sum(x => x.VeiculosProcessados);
        var vehiclesRemaining = jobs.Sum(x => Math.Max(0, x.TotalVeiculos - x.VeiculosProcessados));

        var itemTimes = await itemsQuery
            .Where(x => x.IniciadoEm.HasValue && x.FinalizadoEm.HasValue)
            .Select(x => new { x.IniciadoEm, x.FinalizadoEm })
            .ToListAsync(ct);
        var durations = itemTimes.Select(x => Duration(x.IniciadoEm, x.FinalizadoEm)).Where(x => x > TimeSpan.Zero).ToList();

        var windowStart = DateTime.UtcNow.AddMinutes(-15);
        var recentImages = await itemsQuery.CountAsync(x => x.FinalizadoEm.HasValue
            && x.FinalizadoEm.Value >= windowStart
            && x.Status == LegacyImageImportItemStatus.Completed, ct);
        var recentVehicles = await itemsQuery
            .Where(x => x.FinalizadoEm.HasValue
                && x.FinalizadoEm.Value >= windowStart
                && LegacyImageImportItemStatus.Terminal.Contains(x.Status))
            .Select(x => x.VeiculoId)
            .Distinct()
            .CountAsync(ct);

        var windowMinutes = 15d;
        var imagesPerMinute = recentImages > 0
            ? recentImages / windowMinutes
            : Rate(imported, elapsed);
        var vehiclesPerMinute = recentVehicles > 0
            ? recentVehicles / windowMinutes
            : Rate(vehiclesProcessed, elapsed);

        var eta = vehiclesPerMinute > 0
            ? TimeSpan.FromMinutes(vehiclesRemaining / vehiclesPerMinute)
            : TimeSpan.Zero;

        return new LegacyImageImportDashboardSnapshot(
            activeJobs,
            jobs.Count(x => x.Status == LegacyImageImportJobStatus.Completed),
            jobs.Count(x => x.Status is LegacyImageImportJobStatus.Failed or LegacyImageImportJobStatus.CompletedWithFailures),
            vehiclesProcessed,
            vehiclesRemaining,
            imported,
            pending,
            failed,
            ignored,
            elapsed,
            eta,
            vehiclesPerMinute,
            imagesPerMinute,
            AverageDurationPerVehicle(jobs),
            Average(durations),
            durations.Count == 0 ? TimeSpan.Zero : durations.Max(),
            durations.Count == 0 ? TimeSpan.Zero : durations.Min(),
            Percent(imported, Math.Max(1, imported + ignored + failed)),
            Percent(failed, Math.Max(1, totalItems)),
            Percent(retries, Math.Max(1, totalItems)));
    }

    public async Task<LegacyImageImportJobDetails?> GetJobDetailsAsync(int jobId, LegacyImageImportFilters filters, int? afterLogIndex, CancellationToken ct)
    {
        var snapshot = await BuildSnapshotAsync(jobId, afterLogIndex, ct);
        if (snapshot is null)
        {
            return null;
        }

        var job = await db.ImportJobs.AsNoTracking().FirstAsync(x => x.Id == jobId, ct);
        var images = await BuildImageDetailsAsync(jobId, filters, ct);
        var vehicles = images
            .GroupBy(x => new { x.VehicleId, x.VehicleName })
            .Select(x => new LegacyImageImportVehicleDetail(
                x.Key.VehicleId,
                x.Key.VehicleName,
                null,
                null,
                x.Count(),
                x.Count(i => i.Status == LegacyImageImportItemStatus.Completed),
                x.Count(i => i.Status is LegacyImageImportItemStatus.Pending or LegacyImageImportItemStatus.Running),
                x.Count(i => i.Status is LegacyImageImportItemStatus.Failed or LegacyImageImportItemStatus.Review),
                x.Count(i => i.Status == LegacyImageImportItemStatus.Ignored),
                VehicleStatus(x),
                SumDurations(x.Select(i => i.ProcessingTime))))
            .OrderBy(x => x.VehicleId)
            .ToList();

        var logs = await BuildLogsAsync(jobId, afterLogIndex, ct);
        var history = await BuildHistoryAsync(jobId, ct);
        var metrics = await BuildMetricsAsync(jobId, ct);

        return new LegacyImageImportJobDetails(
            snapshot,
            vehicles,
            images,
            logs,
            images.Where(x => x.Status is LegacyImageImportItemStatus.Failed or LegacyImageImportItemStatus.Review).ToList(),
            history,
            metrics,
            job.RelatorioConsolidadoJson);
    }

    public async Task<string?> StoreConsolidatedReportAsync(int jobId, CancellationToken ct)
    {
        var report = await BuildConsolidatedReportAsync(jobId, ct);
        if (report is null)
        {
            return null;
        }

        var job = await db.ImportJobs.FirstAsync(x => x.Id == jobId, ct);
        var json = JsonSerializer.Serialize(report, JsonOptions);
        job.SetConsolidatedReport(json);
        await db.SaveChangesAsync(ct);
        return json;
    }

    public async Task<LegacyImageImportConsolidatedReport?> BuildConsolidatedReportAsync(int jobId, CancellationToken ct)
    {
        var job = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == jobId, ct);
        if (job is null)
        {
            return null;
        }

        var metrics = await BuildMetricsAsync(jobId, ct);
        var totalVehiclesImported = await db.ImportJobItems
            .AsNoTracking()
            .Where(x => x.ImportJobId == jobId && x.Status == LegacyImageImportItemStatus.Completed)
            .Select(x => x.VeiculoId)
            .Distinct()
            .CountAsync(ct);

        return new LegacyImageImportConsolidatedReport(
            job.Id,
            job.TotalVeiculos,
            totalVehiclesImported,
            job.TotalImagens,
            metrics.Imported,
            metrics.Ignored,
            metrics.Failures,
            metrics.TotalTime,
            metrics.UploadsPerMinute,
            metrics.SuccessRate,
            metrics.ErrorRate,
            metrics.EstimatedLocalStorageSavingsBytes,
            ToOffset(job.IniciadoEm),
            ToOffset(job.FinalizadoEm),
            job.UsuarioNome);
    }

    public async Task<byte[]?> ExportJobAsync(int jobId, string format, CancellationToken ct)
    {
        var details = await GetJobDetailsAsync(jobId, new LegacyImageImportFilters(), null, ct);
        if (details is null)
        {
            return null;
        }

        var payload = new LegacyImageImportExportPayload(
            details.Summary,
            details.Vehicles,
            details.Images,
            details.Logs,
            details.History,
            details.Metrics,
            details.ConsolidatedReportJson);

        return format.Trim().ToLowerInvariant() switch
        {
            "json" => JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions),
            "xlsx" => ExportJobXlsx(payload),
            _ => Encoding.UTF8.GetBytes(ExportJobCsv(payload))
        };
    }

    public async Task<byte[]?> ExportReportAsync(int jobId, string format, CancellationToken ct)
    {
        var report = await BuildConsolidatedReportAsync(jobId, ct);
        if (report is null)
        {
            return null;
        }

        return format.Trim().ToLowerInvariant() switch
        {
            "json" => JsonSerializer.SerializeToUtf8Bytes(report, JsonOptions),
            "xlsx" => SimpleSpreadsheetExporter.CreateWorkbook(new[] { new SpreadsheetSheet("Relatorio", ReportRows(report)) }),
            _ => Encoding.UTF8.GetBytes(ToCsv(ReportRows(report)))
        };
    }

    private async Task<LegacyImageImportSnapshot?> BuildSnapshotAsync(int jobId, int? afterLogIndex, CancellationToken ct)
    {
        var job = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == jobId, ct);
        if (job is null)
        {
            return null;
        }

        var totalImages = Math.Max(job.TotalImagens, await db.ImportJobItems.CountAsync(x => x.ImportJobId == jobId, ct));
        var imagesImported = await db.ImportJobItems.CountAsync(x => x.ImportJobId == jobId && x.Status == LegacyImageImportItemStatus.Completed, ct);
        var imagesSkipped = await db.ImportJobItems.CountAsync(x => x.ImportJobId == jobId && x.Status == LegacyImageImportItemStatus.Ignored, ct);
        var imagesWithError = await db.ImportJobItems.CountAsync(x => x.ImportJobId == jobId
            && (x.Status == LegacyImageImportItemStatus.Failed || x.Status == LegacyImageImportItemStatus.Review), ct);
        var imagesPending = await db.ImportJobItems.CountAsync(x => x.ImportJobId == jobId
            && (x.Status == LegacyImageImportItemStatus.Pending || x.Status == LegacyImageImportItemStatus.Running), ct);
        var vehiclesProcessed = Math.Max(job.VeiculosProcessados, await CountProcessedVehiclesAsync(jobId, ct));
        var vehiclesWithImportedImages = await CountVehiclesByAnyItemStatusesAsync(jobId, [LegacyImageImportItemStatus.Completed], ct);
        var vehiclesSkipped = await CountVehiclesByAnyItemStatusesAsync(jobId, [LegacyImageImportItemStatus.Ignored], ct);
        var vehiclesWithError = await CountVehiclesByAnyItemStatusesAsync(jobId, [LegacyImageImportItemStatus.Failed, LegacyImageImportItemStatus.Review], ct);
        var elapsed = Elapsed(job);
        var remaining = Math.Max(0, job.TotalVeiculos - vehiclesProcessed);
        var eta = vehiclesProcessed > 0 && job.Status == LegacyImageImportJobStatus.Running
            ? TimeSpan.FromTicks(elapsed.Ticks / Math.Max(1, vehiclesProcessed) * remaining)
            : TimeSpan.Zero;

        var logs = await BuildLogsAsync(jobId, afterLogIndex, ct);

        return new LegacyImageImportSnapshot(
            job.Id,
            job.Status,
            job.VeiculoAtualId,
            job.TotalVeiculos,
            vehiclesProcessed,
            remaining,
            vehiclesWithImportedImages,
            vehiclesSkipped,
            vehiclesWithError,
            imagesImported,
            imagesImported,
            imagesSkipped,
            imagesWithError,
            elapsed,
            eta,
            Rate(imagesImported, elapsed),
            logs,
            totalImages,
            imagesPending);
    }

    private async Task<IReadOnlyList<LegacyImageImportImageDetail>> BuildImageDetailsAsync(int jobId, LegacyImageImportFilters filters, CancellationToken ct)
    {
        var query = ApplyItemFilters(db.ImportJobItems
            .AsNoTracking()
            .Include(x => x.Veiculo)
                .ThenInclude(x => x.Marca)
            .Where(x => x.ImportJobId == jobId), filters);

        return await query
            .OrderBy(x => x.VeiculoId)
            .ThenBy(x => x.Ordem)
            .Select(x => new LegacyImageImportImageDetail(
                x.Id,
                x.VeiculoId,
                x.Ordem + 1,
                x.Veiculo.Titulo + " " + x.Veiculo.Modelo,
                x.UrlLegada,
                x.UrlDestino,
                x.BlobNameDestino,
                x.Status,
                x.ContentType,
                x.TamanhoBytes,
                x.FinalizadoEm.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(x.FinalizadoEm.Value, DateTimeKind.Utc)) : null,
                Duration(x.IniciadoEm, x.FinalizadoEm),
                x.Tentativas,
                x.MaxTentativas,
                x.Erro))
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<LegacyImageImportLogEntry>> BuildLogsAsync(int jobId, int? afterLogIndex, CancellationToken ct)
    {
        var query = db.ImportJobLogs.AsNoTracking().Where(x => x.ImportJobId == jobId);
        if (afterLogIndex.HasValue)
        {
            query = query.Where(x => x.Id > afterLogIndex.Value);
        }

        return await query
            .OrderByDescending(x => x.Id)
            .Take(500)
            .OrderBy(x => x.Id)
            .Select(x => new LegacyImageImportLogEntry(
                x.Id,
                new DateTimeOffset(DateTime.SpecifyKind(x.CriadoEm, DateTimeKind.Utc)),
                x.VeiculoId,
                x.ImagemOrdem,
                x.Etapa,
                x.Status,
                x.Mensagem,
                x.UrlLegada))
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<LegacyImageImportHistoryEntry>> BuildHistoryAsync(int jobId, CancellationToken ct)
    {
        var history = await db.ImportJobHistory
            .AsNoTracking()
            .Where(x => x.ImportJobId == jobId)
            .OrderBy(x => x.CriadoEm)
            .ToListAsync(ct);

        return history
            .Select(x => new LegacyImageImportHistoryEntry(
                x.Id,
                x.Tipo,
                new DateTimeOffset(DateTime.SpecifyKind(x.CriadoEm, DateTimeKind.Utc)),
                x.UsuarioNome,
                x.Quantidade,
                x.DuracaoMs.HasValue ? TimeSpan.FromMilliseconds(x.DuracaoMs.Value) : null,
                x.Resultado,
                x.Mensagem))
            .ToList();
    }

    private async Task<LegacyImageImportMetrics> BuildMetricsAsync(int jobId, CancellationToken ct)
    {
        var job = await db.ImportJobs.AsNoTracking().FirstAsync(x => x.Id == jobId, ct);
        var items = await db.ImportJobItems
            .AsNoTracking()
            .Where(x => x.ImportJobId == jobId)
            .Select(x => new
            {
                x.Status,
                x.Tentativas,
                x.TamanhoBytes,
                x.IniciadoEm,
                x.FinalizadoEm,
                x.VeiculoId
            })
            .ToListAsync(ct);

        var durations = items.Select(x => Duration(x.IniciadoEm, x.FinalizadoEm)).Where(x => x > TimeSpan.Zero).ToList();
        var totalTime = Elapsed(job);
        var imported = items.Count(x => x.Status == LegacyImageImportItemStatus.Completed);
        var ignored = items.Count(x => x.Status == LegacyImageImportItemStatus.Ignored);
        var failed = items.Count(x => x.Status is LegacyImageImportItemStatus.Failed or LegacyImageImportItemStatus.Review);
        var cancelled = job.Status == LegacyImageImportJobStatus.Cancelled
            ? items.Count(x => x.Status == LegacyImageImportItemStatus.Pending || x.Status == LegacyImageImportItemStatus.Running)
            : 0;
        var retries = items.Sum(x => Math.Max(0, x.Tentativas - 1));
        var terminal = Math.Max(1, imported + ignored + failed);
        var downloads = await db.ImportJobLogs.CountAsync(x => x.ImportJobId == jobId && x.Etapa == "Download" && x.Status == "Sucesso", ct);

        return new LegacyImageImportMetrics(
            totalTime,
            AverageDurationPerVehicle(job, items.GroupBy(x => x.VeiculoId).Select(x => SumDurations(x.Select(i => Duration(i.IniciadoEm, i.FinalizadoEm))))),
            Average(durations),
            durations.Count == 0 ? TimeSpan.Zero : durations.Max(),
            durations.Count == 0 ? TimeSpan.Zero : durations.Min(),
            Rate(imported, totalTime),
            Rate(downloads, totalTime),
            retries,
            failed,
            ignored,
            imported,
            cancelled,
            Percent(imported, terminal),
            Percent(failed, terminal),
            Percent(retries, Math.Max(1, items.Count)),
            items.Where(x => x.Status == LegacyImageImportItemStatus.Completed).Sum(x => x.TamanhoBytes ?? 0));
    }

    private static IQueryable<ImportJob> ApplyJobFilters(IQueryable<ImportJob> query, LegacyImageImportFilters filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            query = query.Where(x => x.Status == filters.Status);
        }

        if (!string.IsNullOrWhiteSpace(filters.User))
        {
            var user = filters.User.Trim();
            query = query.Where(x => x.UsuarioNome != null && x.UsuarioNome.Contains(user));
        }

        if (filters.PeriodStart.HasValue)
        {
            query = query.Where(x => x.CriadoEm >= filters.PeriodStart.Value.Date);
        }

        if (filters.PeriodEnd.HasValue)
        {
            query = query.Where(x => x.CriadoEm < filters.PeriodEnd.Value.Date.AddDays(1));
        }

        return query;
    }

    private static IQueryable<ImportJobItem> ApplyItemFilters(IQueryable<ImportJobItem> query, LegacyImageImportFilters filters)
    {
        if (filters.VehicleId.HasValue)
        {
            query = query.Where(x => x.VeiculoId == filters.VehicleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            query = query.Where(x => x.Status == filters.Status);
        }

        if (filters.OnlyErrors)
        {
            query = query.Where(x => x.Status == LegacyImageImportItemStatus.Failed || x.Status == LegacyImageImportItemStatus.Review);
        }

        if (filters.OnlyPending)
        {
            query = query.Where(x => x.Status == LegacyImageImportItemStatus.Pending || x.Status == LegacyImageImportItemStatus.Running);
        }

        if (filters.OnlyCompleted)
        {
            query = query.Where(x => x.Status == LegacyImageImportItemStatus.Completed);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(x => x.VeiculoId.ToString().Contains(search)
                || x.BlobNameDestino.Contains(search)
                || x.UrlLegada.Contains(search)
                || (x.Erro != null && x.Erro.Contains(search))
                || x.Veiculo.Titulo.Contains(search)
                || x.Veiculo.Modelo.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filters.Marca))
        {
            var brand = filters.Marca.Trim();
            query = query.Where(x => x.Veiculo.Marca.Nome.Contains(brand));
        }

        if (!string.IsNullOrWhiteSpace(filters.Modelo))
        {
            var model = filters.Modelo.Trim();
            query = query.Where(x => x.Veiculo.Modelo.Contains(model));
        }

        if (filters.PeriodStart.HasValue)
        {
            query = query.Where(x => x.DataCadastro >= filters.PeriodStart.Value.Date);
        }

        if (filters.PeriodEnd.HasValue)
        {
            query = query.Where(x => x.DataCadastro < filters.PeriodEnd.Value.Date.AddDays(1));
        }

        return query;
    }

    private static string ExportJobCsv(LegacyImageImportExportPayload payload)
    {
        var rows = new List<IReadOnlyList<string?>>
        {
            new[] { "Secao", "Campo", "Valor" },
            new[] { "Resumo", "Job", payload.Job.ToString() },
            new[] { "Metricas", "Importados", payload.Metrics.Imported.ToString(CultureInfo.InvariantCulture) },
            new[] { "Metricas", "Falhas", payload.Metrics.Failures.ToString(CultureInfo.InvariantCulture) },
            new[] { "Metricas", "Ignorados", payload.Metrics.Ignored.ToString(CultureInfo.InvariantCulture) },
            Array.Empty<string?>()
        };

        rows.AddRange(ImageRows(payload.Images));
        rows.Add(Array.Empty<string?>());
        rows.AddRange(LogRows(payload.Logs));
        rows.Add(Array.Empty<string?>());
        rows.AddRange(HistoryRows(payload.History));

        return ToCsv(rows);
    }

    private static byte[] ExportJobXlsx(LegacyImageImportExportPayload payload)
        => SimpleSpreadsheetExporter.CreateWorkbook(new SpreadsheetSheet[]
        {
            new("Resumo", SummaryRows(payload)),
            new("Veiculos", VehicleRows(payload.Vehicles)),
            new("Imagens", ImageRows(payload.Images)),
            new("Erros", ImageRows(payload.Images.Where(x => x.Status is LegacyImageImportItemStatus.Failed or LegacyImageImportItemStatus.Review).ToList())),
            new("Logs", LogRows(payload.Logs)),
            new("Historico", HistoryRows(payload.History))
        });

    private static IReadOnlyList<IReadOnlyList<string?>> SummaryRows(LegacyImageImportExportPayload payload)
        => new List<IReadOnlyList<string?>>
        {
            new[] { "Campo", "Valor" },
            new[] { "Importados", payload.Metrics.Imported.ToString(CultureInfo.InvariantCulture) },
            new[] { "Ignorados", payload.Metrics.Ignored.ToString(CultureInfo.InvariantCulture) },
            new[] { "Falhas", payload.Metrics.Failures.ToString(CultureInfo.InvariantCulture) },
            new[] { "Retries", payload.Metrics.Retries.ToString(CultureInfo.InvariantCulture) },
            new[] { "Tempo total", payload.Metrics.TotalTime.ToString() },
            new[] { "Uploads/min", payload.Metrics.UploadsPerMinute.ToString("N2", CultureInfo.InvariantCulture) },
            new[] { "Downloads/min", payload.Metrics.DownloadsPerMinute.ToString("N2", CultureInfo.InvariantCulture) },
            new[] { "Taxa sucesso", payload.Metrics.SuccessRate.ToString("N2", CultureInfo.InvariantCulture) },
            new[] { "Taxa erro", payload.Metrics.ErrorRate.ToString("N2", CultureInfo.InvariantCulture) }
        };

    private static IReadOnlyList<IReadOnlyList<string?>> VehicleRows(IEnumerable<LegacyImageImportVehicleDetail> vehicles)
    {
        var rows = new List<IReadOnlyList<string?>>
        {
            new[] { "VeiculoId", "Nome", "Total", "Importadas", "Pendentes", "Erros", "Ignoradas", "Status", "Tempo" }
        };
        rows.AddRange(vehicles.Select(x => new[]
        {
            x.VehicleId.ToString(CultureInfo.InvariantCulture),
            x.Title,
            x.TotalImages.ToString(CultureInfo.InvariantCulture),
            x.ImportedImages.ToString(CultureInfo.InvariantCulture),
            x.PendingImages.ToString(CultureInfo.InvariantCulture),
            x.ErrorImages.ToString(CultureInfo.InvariantCulture),
            x.SkippedImages.ToString(CultureInfo.InvariantCulture),
            x.Status,
            x.ProcessingTime.ToString()
        }));
        return rows;
    }

    private static IReadOnlyList<IReadOnlyList<string?>> ImageRows(IEnumerable<LegacyImageImportImageDetail> images)
    {
        var rows = new List<IReadOnlyList<string?>>
        {
            new[] { "ItemId", "VeiculoId", "Ordem", "Veiculo", "Origem", "Destino", "Blob", "Status", "ContentType", "Tamanho", "ImportadoEm", "Tempo", "Tentativas", "Erro" }
        };
        rows.AddRange(images.Select(x => new[]
        {
            x.ItemId.ToString(CultureInfo.InvariantCulture),
            x.VehicleId.ToString(CultureInfo.InvariantCulture),
            x.Order.ToString(CultureInfo.InvariantCulture),
            x.VehicleName,
            x.SourceUrl,
            x.StoredUrl,
            x.BlobName,
            x.Status,
            x.ContentType,
            x.SizeBytes?.ToString(CultureInfo.InvariantCulture),
            x.ImportedAt?.ToString("O", CultureInfo.InvariantCulture),
            x.ProcessingTime.ToString(),
            x.Attempts.ToString(CultureInfo.InvariantCulture),
            x.Error
        }));
        return rows;
    }

    private static IReadOnlyList<IReadOnlyList<string?>> LogRows(IEnumerable<LegacyImageImportLogEntry> logs)
    {
        var rows = new List<IReadOnlyList<string?>>
        {
            new[] { "Id", "TimestampUtc", "Veiculo", "Imagem", "Etapa", "Status", "Mensagem", "Url" }
        };
        rows.AddRange(logs.Select(x => new[]
        {
            x.Index.ToString(CultureInfo.InvariantCulture),
            x.TimestampUtc.ToString("O", CultureInfo.InvariantCulture),
            x.VehicleId?.ToString(CultureInfo.InvariantCulture),
            x.ImageIndex?.ToString(CultureInfo.InvariantCulture),
            x.Stage,
            x.Status,
            x.Message,
            x.ImageUrl
        }));
        return rows;
    }

    private static IReadOnlyList<IReadOnlyList<string?>> HistoryRows(IEnumerable<LegacyImageImportHistoryEntry> history)
    {
        var rows = new List<IReadOnlyList<string?>>
        {
            new[] { "Id", "Tipo", "Quando", "Usuario", "Quantidade", "Duracao", "Resultado", "Mensagem" }
        };
        rows.AddRange(history.Select(x => new[]
        {
            x.Id.ToString(CultureInfo.InvariantCulture),
            x.Type,
            x.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
            x.UserName,
            x.Quantity?.ToString(CultureInfo.InvariantCulture),
            x.Duration?.ToString(),
            x.Result,
            x.Message
        }));
        return rows;
    }

    private static IReadOnlyList<IReadOnlyList<string?>> ReportRows(LegacyImageImportConsolidatedReport report)
        => new List<IReadOnlyList<string?>>
        {
            new[] { "Campo", "Valor" },
            new[] { "Job", report.JobId.ToString(CultureInfo.InvariantCulture) },
            new[] { "Total veiculos analisados", report.TotalVehiclesAnalyzed.ToString(CultureInfo.InvariantCulture) },
            new[] { "Total veiculos importados", report.TotalVehiclesImported.ToString(CultureInfo.InvariantCulture) },
            new[] { "Total imagens processadas", report.TotalImagesProcessed.ToString(CultureInfo.InvariantCulture) },
            new[] { "Total imagens importadas", report.TotalImagesImported.ToString(CultureInfo.InvariantCulture) },
            new[] { "Total imagens ignoradas", report.TotalImagesIgnored.ToString(CultureInfo.InvariantCulture) },
            new[] { "Total erros", report.TotalErrors.ToString(CultureInfo.InvariantCulture) },
            new[] { "Tempo total", report.TotalTime.ToString() },
            new[] { "Velocidade media", report.AverageSpeedImagesPerMinute.ToString("N2", CultureInfo.InvariantCulture) },
            new[] { "Taxa sucesso", report.SuccessRate.ToString("N2", CultureInfo.InvariantCulture) },
            new[] { "Taxa erro", report.ErrorRate.ToString("N2", CultureInfo.InvariantCulture) },
            new[] { "Economia armazenamento bytes", report.EstimatedLocalStorageSavingsBytes.ToString(CultureInfo.InvariantCulture) },
            new[] { "Inicio", report.StartedAt?.ToString("O", CultureInfo.InvariantCulture) },
            new[] { "Fim", report.FinishedAt?.ToString("O", CultureInfo.InvariantCulture) },
            new[] { "Usuario", report.ResponsibleUser }
        };

    private static string ToCsv(IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(';', row.Select(Escape)));
        }

        return sb.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private async Task<int> CountProcessedVehiclesAsync(int jobId, CancellationToken ct)
    {
        var vehiclesWithItems = await db.ImportJobItems
            .Where(x => x.ImportJobId == jobId)
            .Select(x => x.VeiculoId)
            .Distinct()
            .ToListAsync(ct);
        var vehiclesWithOpenItems = await db.ImportJobItems
            .Where(x => x.ImportJobId == jobId && !LegacyImageImportItemStatus.Terminal.Contains(x.Status))
            .Select(x => x.VeiculoId)
            .Distinct()
            .ToListAsync(ct);

        return vehiclesWithItems.Except(vehiclesWithOpenItems).Count();
    }

    private async Task<int> CountVehiclesByAnyItemStatusesAsync(int jobId, string[] statuses, CancellationToken ct)
        => await db.ImportJobItems
            .Where(x => x.ImportJobId == jobId && statuses.Contains(x.Status))
            .Select(x => x.VeiculoId)
            .Distinct()
            .CountAsync(ct);

    private static TimeSpan AverageDurationPerVehicle(IEnumerable<ImportJob> jobs)
    {
        var durations = jobs
            .Where(x => x.TotalVeiculos > 0)
            .Select(x => TimeSpan.FromTicks(Elapsed(x).Ticks / Math.Max(1, x.TotalVeiculos)))
            .Where(x => x > TimeSpan.Zero)
            .ToList();
        return Average(durations);
    }

    private static TimeSpan AverageDurationPerVehicle(ImportJob job, IEnumerable<TimeSpan> vehicleDurations)
    {
        var durations = vehicleDurations.Where(x => x > TimeSpan.Zero).ToList();
        if (durations.Count > 0)
        {
            return Average(durations);
        }

        return job.TotalVeiculos > 0 ? TimeSpan.FromTicks(Elapsed(job).Ticks / Math.Max(1, job.TotalVeiculos)) : TimeSpan.Zero;
    }

    private static string VehicleStatus(IEnumerable<LegacyImageImportImageDetail> images)
    {
        var list = images.ToList();
        if (list.Any(x => x.Status is LegacyImageImportItemStatus.Failed or LegacyImageImportItemStatus.Review))
        {
            return LegacyImageImportItemStatus.Failed;
        }

        if (list.Any(x => x.Status is LegacyImageImportItemStatus.Pending or LegacyImageImportItemStatus.Running))
        {
            return LegacyImageImportItemStatus.Pending;
        }

        if (list.Any(x => x.Status == LegacyImageImportItemStatus.Completed))
        {
            return LegacyImageImportItemStatus.Completed;
        }

        return LegacyImageImportItemStatus.Ignored;
    }

    private static TimeSpan Duration(DateTime? start, DateTime? end)
    {
        if (!start.HasValue)
        {
            return TimeSpan.Zero;
        }

        var finish = end ?? DateTime.UtcNow;
        return finish > start.Value ? finish - start.Value : TimeSpan.Zero;
    }

    private static TimeSpan Elapsed(ImportJob job)
    {
        var start = job.IniciadoEm ?? job.CriadoEm;
        var end = job.FinalizadoEm ?? DateTime.UtcNow;
        return end > start ? end - start : TimeSpan.Zero;
    }

    private static TimeSpan SumElapsed(IEnumerable<ImportJob> jobs)
        => TimeSpan.FromTicks(jobs.Select(Elapsed).Sum(x => x.Ticks));

    private static TimeSpan SumDurations(IEnumerable<TimeSpan> durations)
        => TimeSpan.FromTicks(durations.Sum(x => x.Ticks));

    private static TimeSpan Average(IReadOnlyList<TimeSpan> durations)
        => durations.Count == 0 ? TimeSpan.Zero : TimeSpan.FromTicks((long)durations.Average(x => x.Ticks));

    private static double Rate(int quantity, TimeSpan elapsed)
        => elapsed.TotalMinutes <= 0 ? 0 : quantity / elapsed.TotalMinutes;

    private static double Percent(int quantity, int total)
        => total <= 0 ? 0 : quantity * 100d / total;

    private static DateTimeOffset? ToOffset(DateTime? value)
        => value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null;
}
