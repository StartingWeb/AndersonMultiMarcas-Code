using System.Collections.Concurrent;
using Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Project.Features.Storage.Legacy;

public sealed class LegacyImageImportWorker(
    IServiceScopeFactory scopeFactory,
    LegacyImageImportQueue queue,
    LegacyImportCancellationRegistry cancellationRegistry,
    IOptions<LegacyImageImportOptions> importOptions,
    ILogger<LegacyImageImportWorker> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<int, byte> activeJobs = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (importOptions.Value.RecoverPendingJobsOnStartup)
        {
            await RecoverPendingJobsAsync(stoppingToken);
        }
        else
        {
            logger.LogInformation("Retomada automatica de jobs de importacao legado desativada.");
        }

        await foreach (var jobId in queue.ReadAllAsync(stoppingToken))
        {
            if (!activeJobs.TryAdd(jobId, 0))
            {
                continue;
            }

            try
            {
                await ProcessJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Worker de importacao de imagens legado finalizado pelo host.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha inesperada no worker da importacao legado {JobId}.", jobId);
            }
            finally
            {
                activeJobs.TryRemove(jobId, out _);
            }
        }
    }

    private async Task RecoverPendingJobsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var jobs = await db.ImportJobs
            .Where(x => LegacyImageImportJobStatus.Recoverable.Contains(x.Status))
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        foreach (var job in jobs)
        {
            if (job.Status == LegacyImageImportJobStatus.Running)
            {
                job.MarkQueued("Job retomado apos reinicio da aplicacao.");
                await ResetRunningItemsAsync(db, job.Id, ct);
                db.ImportJobLogs.Add(Log(job.Id, null, null, null, "Sistema", "Retomada", "Job retomado apos reinicio da aplicacao."));
            }

            await queue.QueueAsync(job.Id, ct);
        }

        if (jobs.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task ProcessJobAsync(int jobId, CancellationToken stoppingToken)
    {
        using var linked = cancellationRegistry.Register(jobId, stoppingToken);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<LegacyVehicleImageImportService>();
            var workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";
            await importer.RunAsync(jobId, workerId, linked.Token);
        }
        finally
        {
            cancellationRegistry.Unregister(jobId);
        }
    }

    private static async Task ResetRunningItemsAsync(ApplicationDbContext db, int jobId, CancellationToken ct)
    {
        var runningItems = await db.ImportJobItems
            .Where(x => x.ImportJobId == jobId && x.Status == LegacyImageImportItemStatus.Running)
            .ToListAsync(ct);

        foreach (var item in runningItems)
        {
            item.MarkPending("Retomado apos reinicio.");
        }
    }

    private static ImportJobLog Log(int jobId, int? itemId, int? vehicleId, int? imageIndex, string stage, string status, string message, string? imageUrl = null)
        => new(jobId, itemId, vehicleId, imageIndex, imageUrl, stage, status, message);
}
