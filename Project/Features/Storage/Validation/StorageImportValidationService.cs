using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Core.Storage;
using Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Project.Shared;

namespace Project.Features.Storage.Validation;

public sealed class StorageImportValidationService(
    ApplicationDbContext db,
    R2StorageService r2,
    IHttpClientFactory httpClientFactory,
    ILogger<StorageImportValidationService> logger)
{
    public const string HttpClientName = "storage-import-validation";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<StorageImportValidationReport> ValidateAsync(StorageImportValidationRequest request, CancellationToken ct)
    {
        var query = db.VeiculoMidias
            .AsNoTracking()
            .Include(x => x.Veiculo)
                .ThenInclude(x => x.Marca)
            .Where(x => x.Ativo && x.Tipo == TipoMidia.Imagem);

        if (request.VehicleId.HasValue)
        {
            query = query.Where(x => x.VeiculoId == request.VehicleId.Value);
        }

        if (string.Equals(request.Scope, StorageImportValidationScopes.Errors, StringComparison.OrdinalIgnoreCase))
        {
            var vehicleIds = await db.ImportJobItems
                .AsNoTracking()
                .Where(x => x.Status == "Erro" || x.Status == "PendenteRevisao")
                .Select(x => x.VeiculoId)
                .Distinct()
                .ToListAsync(ct);
            query = query.Where(x => vehicleIds.Contains(x.VeiculoId));
        }

        if (string.Equals(request.Scope, StorageImportValidationScopes.Pending, StringComparison.OrdinalIgnoreCase))
        {
            var vehicleIds = await db.ImportJobItems
                .AsNoTracking()
                .Where(x => x.Status == "Pendente" || x.Status == "EmExecucao")
                .Select(x => x.VeiculoId)
                .Distinct()
                .ToListAsync(ct);
            query = query.Where(x => vehicleIds.Contains(x.VeiculoId));
        }

        if (request.MaxRecords is > 0)
        {
            query = query.Take(request.MaxRecords.Value);
        }

        var media = await query
            .OrderBy(x => x.VeiculoId)
            .ThenBy(x => x.Ordem)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        var results = new List<StorageImportValidationResult>(media.Count);
        foreach (var item in media)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(await ValidateMediaAsync(item.Id, item.VeiculoId, item.Veiculo.NomeCompleto, item.BlobName, item.ContentType, item.TamanhoBytes, ct));
        }

        return new StorageImportValidationReport(
            DateTimeOffset.UtcNow,
            request,
            new StorageImportValidationSummary(
                results.Count,
                results.Count(x => x.Status == StorageImportValidationStatus.Ok),
                results.Count(x => x.Status == StorageImportValidationStatus.Missing),
                results.Count(x => x.Status == StorageImportValidationStatus.Invalid),
                results.Count(x => x.Status == StorageImportValidationStatus.ContentTypeMismatch),
                results.Count(x => x.Status == StorageImportValidationStatus.SizeMismatch),
                results.Count(x => x.Status == StorageImportValidationStatus.AccessError),
                results.Count(x => x.Status == StorageImportValidationStatus.InvalidUrl)),
            results);
    }

    public byte[] Export(StorageImportValidationReport report, string format)
        => format.Trim().ToLowerInvariant() switch
        {
            "json" => JsonSerializer.SerializeToUtf8Bytes(report, JsonOptions),
            "xlsx" => SimpleSpreadsheetExporter.CreateWorkbook(new[] { new SpreadsheetSheet("Validacao", Rows(report.Results)) }),
            _ => Encoding.UTF8.GetBytes(ToCsv(Rows(report.Results)))
        };

    private async Task<StorageImportValidationResult> ValidateMediaAsync(
        int mediaId,
        int vehicleId,
        string vehicleName,
        string? blobName,
        string? expectedContentType,
        long? expectedSize,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return Result(mediaId, vehicleId, vehicleName, blobName, null, expectedContentType, null, expectedSize, null, false, StorageImportValidationStatus.Missing, "Registro sem BlobName.");
        }

        StorageObjectMetadata? metadata;
        try
        {
            metadata = await r2.GetMetadataAsync(blobName, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Erro ao validar BlobName {BlobName}.", blobName);
            return Result(mediaId, vehicleId, vehicleName, blobName, null, expectedContentType, null, expectedSize, null, false, StorageImportValidationStatus.AccessError, ex.Message);
        }

        if (metadata is null)
        {
            return Result(mediaId, vehicleId, vehicleName, blobName, r2.GetPublicUrl(blobName), expectedContentType, null, expectedSize, null, false, StorageImportValidationStatus.Missing, "Objeto ausente no R2.");
        }

        var publicUrl = r2.GetPublicUrl(blobName);
        var urlStatus = await ValidatePublicUrlAsync(publicUrl, ct);
        var status = Classify(metadata, expectedContentType, expectedSize, urlStatus);
        var message = status switch
        {
            StorageImportValidationStatus.Ok => "OK",
            StorageImportValidationStatus.InvalidUrl => "URL publica invalida ou nao absoluta.",
            StorageImportValidationStatus.AccessError => "URL publica nao respondeu com sucesso.",
            StorageImportValidationStatus.ContentTypeMismatch => "Content-Type divergente.",
            StorageImportValidationStatus.SizeMismatch => "Content-Length divergente ou invalido.",
            _ => "Registro invalido."
        };

        return Result(
            mediaId,
            vehicleId,
            vehicleName,
            blobName,
            publicUrl,
            expectedContentType,
            metadata.ContentType,
            expectedSize,
            metadata.SizeBytes,
            urlStatus.Responded,
            status,
            message);
    }

    private async Task<PublicUrlStatus> ValidatePublicUrlAsync(string publicUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return new PublicUrlStatus(false, false);
        }

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Head, uri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            return new PublicUrlStatus(true, response.IsSuccessStatusCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Erro ao validar URL publica {PublicUrl}.", publicUrl);
            return new PublicUrlStatus(true, false);
        }
    }

    private static string Classify(StorageObjectMetadata metadata, string? expectedContentType, long? expectedSize, PublicUrlStatus urlStatus)
    {
        if (!urlStatus.IsValid)
        {
            return StorageImportValidationStatus.InvalidUrl;
        }

        if (!urlStatus.Responded)
        {
            return StorageImportValidationStatus.AccessError;
        }

        if (!string.IsNullOrWhiteSpace(expectedContentType)
            && !string.IsNullOrWhiteSpace(metadata.ContentType)
            && !metadata.ContentType.StartsWith(expectedContentType, StringComparison.OrdinalIgnoreCase)
            && !expectedContentType.StartsWith(metadata.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return StorageImportValidationStatus.ContentTypeMismatch;
        }

        if (!metadata.SizeBytes.HasValue || metadata.SizeBytes.Value <= 0)
        {
            return StorageImportValidationStatus.Invalid;
        }

        if (expectedSize.HasValue && metadata.SizeBytes.Value != expectedSize.Value)
        {
            return StorageImportValidationStatus.SizeMismatch;
        }

        return StorageImportValidationStatus.Ok;
    }

    private static StorageImportValidationResult Result(
        int mediaId,
        int vehicleId,
        string vehicleName,
        string? blobName,
        string? publicUrl,
        string? expectedContentType,
        string? actualContentType,
        long? expectedSize,
        long? actualSize,
        bool publicUrlResponded,
        string status,
        string message)
        => new(
            mediaId,
            vehicleId,
            vehicleName,
            blobName,
            publicUrl,
            expectedContentType,
            actualContentType,
            expectedSize,
            actualSize,
            publicUrlResponded,
            status,
            message);

    private static IReadOnlyList<IReadOnlyList<string?>> Rows(IEnumerable<StorageImportValidationResult> results)
    {
        var rows = new List<IReadOnlyList<string?>>
        {
            new[] { "MidiaId", "VeiculoId", "Veiculo", "BlobName", "URL Publica", "Content-Type esperado", "Content-Type R2", "Tamanho esperado", "Tamanho R2", "URL respondeu", "Status", "Mensagem" }
        };

        rows.AddRange(results.Select(x => new[]
        {
            x.MediaId.ToString(CultureInfo.InvariantCulture),
            x.VehicleId.ToString(CultureInfo.InvariantCulture),
            x.VehicleName,
            x.BlobName,
            x.PublicUrl,
            x.ExpectedContentType,
            x.ActualContentType,
            x.ExpectedSizeBytes?.ToString(CultureInfo.InvariantCulture),
            x.ActualSizeBytes?.ToString(CultureInfo.InvariantCulture),
            x.PublicUrlResponded ? "Sim" : "Nao",
            x.Status,
            x.Message
        }));

        return rows;
    }

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

    private sealed record PublicUrlStatus(bool IsValid, bool Responded);
}

public sealed class StorageImportValidationRequest
{
    public int? VehicleId { get; init; }
    public string Scope { get; init; } = StorageImportValidationScopes.All;
    public int? MaxRecords { get; init; }
}

public static class StorageImportValidationScopes
{
    public const string All = "Todos";
    public const string Errors = "Erros";
    public const string Pending = "Pendentes";
}

public static class StorageImportValidationStatus
{
    public const string Ok = "OK";
    public const string Missing = "Ausente";
    public const string Invalid = "Invalido";
    public const string ContentTypeMismatch = "Content-Type divergente";
    public const string SizeMismatch = "Tamanho divergente";
    public const string AccessError = "Erro de acesso";
    public const string InvalidUrl = "URL invalida";
}

public sealed record StorageImportValidationReport(
    DateTimeOffset GeneratedAt,
    StorageImportValidationRequest Request,
    StorageImportValidationSummary Summary,
    IReadOnlyList<StorageImportValidationResult> Results);

public sealed record StorageImportValidationSummary(
    int Total,
    int Ok,
    int Missing,
    int Invalid,
    int ContentTypeMismatch,
    int SizeMismatch,
    int AccessError,
    int InvalidUrl);

public sealed record StorageImportValidationResult(
    int MediaId,
    int VehicleId,
    string VehicleName,
    string? BlobName,
    string? PublicUrl,
    string? ExpectedContentType,
    string? ActualContentType,
    long? ExpectedSizeBytes,
    long? ActualSizeBytes,
    bool PublicUrlResponded,
    string Status,
    string Message);
