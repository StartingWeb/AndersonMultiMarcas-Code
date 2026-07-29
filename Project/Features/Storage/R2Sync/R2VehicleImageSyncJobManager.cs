using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Project.Features.Storage.R2Sync;

public sealed class R2VehicleImageSyncJobManager(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime applicationLifetime,
    ILogger<R2VehicleImageSyncJobManager> logger)
{
    private const int MaxStoredLogs = 1000;
    private readonly object gate = new();
    private MutableState state = MutableState.Idle();

    public Task<bool> StartAsync(string? userId, string? userName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        CancellationTokenSource cts;
        lock (gate)
        {
            if (R2VehicleImageSyncStatus.IsActive(state.Status))
            {
                return Task.FromResult(false);
            }

            cts = CancellationTokenSource.CreateLinkedTokenSource(applicationLifetime.ApplicationStopping);
            state = MutableState.Start(userId, userName, cts);
            AddLogUnsafe(null, null, "Inicio", "Pendente", "Sincronizacao enfileirada.");
        }

        _ = Task.Run(() => RunAsync(cts.Token), CancellationToken.None);
        return Task.FromResult(true);
    }

    public bool Cancel()
    {
        lock (gate)
        {
            if (!R2VehicleImageSyncStatus.IsActive(state.Status) || state.Cancellation is null)
            {
                return false;
            }

            state.Status = R2VehicleImageSyncStatus.Cancelling;
            AddLogUnsafe(state.CurrentVehicleId, null, "Sistema", "Cancelamento", "Cancelamento solicitado pelo usuario.");
            state.Cancellation.Cancel();
            return true;
        }
    }

    public R2VehicleImageSyncSnapshot GetSnapshot(int? afterLogIndex)
    {
        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            var finished = state.FinishedAtUtc;
            var elapsed = state.StartedAtUtc.HasValue
                ? (finished ?? now) - state.StartedAtUtc.Value
                : TimeSpan.Zero;

            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }

            var logs = state.Logs
                .Where(x => !afterLogIndex.HasValue || x.Index > afterLogIndex.Value)
                .ToList();

            var progressPercent = state.TotalVehicles <= 0
                ? 0
                : Math.Round(state.VehiclesProcessed / (double)state.TotalVehicles * 100, 1);

            return new R2VehicleImageSyncSnapshot(
                state.RunId,
                state.Status,
                R2VehicleImageSyncStatus.IsActive(state.Status),
                state.StartedAtUtc,
                state.FinishedAtUtc,
                state.StartedBy,
                state.CurrentVehicleId,
                state.TotalVehicles,
                state.VehiclesProcessed,
                state.VehiclesFound,
                state.VehiclesWithoutImages,
                state.VehiclesSynchronized,
                state.RecordsCorrected,
                state.Errors,
                elapsed,
                progressPercent,
                logs);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            SetRunning();

            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<R2VehicleImageSyncService>();
            await service.SynchronizeAsync(ReportProgress, AddLog, ct);
            Complete();
        }
        catch (OperationCanceledException)
        {
            CompleteCancelled();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na sincronizacao de imagens R2.");
            Fail(ex.Message);
        }
        finally
        {
            lock (gate)
            {
                state.Cancellation?.Dispose();
                state.Cancellation = null;
            }
        }
    }

    private void SetRunning()
    {
        lock (gate)
        {
            state.Status = R2VehicleImageSyncStatus.Running;
            state.StartedAtUtc ??= DateTimeOffset.UtcNow;
            AddLogUnsafe(null, null, "Sistema", "Execucao", "Sincronizacao iniciada.");
        }
    }

    private void ReportProgress(R2VehicleImageSyncProgress progress)
    {
        lock (gate)
        {
            state.TotalVehicles = progress.TotalVehicles;
            state.VehiclesProcessed = progress.VehiclesProcessed;
            state.VehiclesFound = progress.VehiclesFound;
            state.VehiclesWithoutImages = progress.VehiclesWithoutImages;
            state.VehiclesSynchronized = progress.VehiclesSynchronized;
            state.RecordsCorrected = progress.RecordsCorrected;
            state.Errors = progress.Errors;
            state.CurrentVehicleId = progress.CurrentVehicleId;
        }
    }

    private void AddLog(int? vehicleId, int? legacyVehicleId, string stage, string status, string message)
    {
        lock (gate)
        {
            AddLogUnsafe(vehicleId, legacyVehicleId, stage, status, message);
        }
    }

    private void Complete()
    {
        lock (gate)
        {
            state.Status = state.Errors > 0
                ? R2VehicleImageSyncStatus.CompletedWithErrors
                : R2VehicleImageSyncStatus.Completed;
            state.FinishedAtUtc = DateTimeOffset.UtcNow;
            state.CurrentVehicleId = null;
            AddLogUnsafe(null, null, "Fim", state.Status, "Sincronizacao finalizada.");
        }
    }

    private void CompleteCancelled()
    {
        lock (gate)
        {
            state.Status = R2VehicleImageSyncStatus.Cancelled;
            state.FinishedAtUtc = DateTimeOffset.UtcNow;
            state.CurrentVehicleId = null;
            AddLogUnsafe(null, null, "Sistema", "Cancelado", "Sincronizacao cancelada.");
        }
    }

    private void Fail(string message)
    {
        lock (gate)
        {
            state.Status = R2VehicleImageSyncStatus.Failed;
            state.Errors++;
            state.FinishedAtUtc = DateTimeOffset.UtcNow;
            state.CurrentVehicleId = null;
            AddLogUnsafe(null, null, "Sistema", "Erro", message);
        }
    }

    private void AddLogUnsafe(int? vehicleId, int? legacyVehicleId, string stage, string status, string message)
    {
        var entry = new R2VehicleImageSyncLogEntry(
            ++state.LastLogIndex,
            DateTimeOffset.UtcNow,
            vehicleId,
            legacyVehicleId,
            stage,
            status,
            message);

        state.Logs.Add(entry);
        if (state.Logs.Count > MaxStoredLogs)
        {
            state.Logs.RemoveRange(0, state.Logs.Count - MaxStoredLogs);
        }

        logger.LogInformation(
            "Sincronizacao R2: {Stage}/{Status}. Veiculo={VehicleId}; IdLegado={LegacyVehicleId}; Mensagem={Message}",
            stage,
            status,
            vehicleId,
            legacyVehicleId,
            message);
    }

    private sealed class MutableState
    {
        public Guid? RunId { get; set; }
        public string Status { get; set; } = R2VehicleImageSyncStatus.Idle;
        public DateTimeOffset? StartedAtUtc { get; set; }
        public DateTimeOffset? FinishedAtUtc { get; set; }
        public string? StartedBy { get; set; }
        public int? CurrentVehicleId { get; set; }
        public int TotalVehicles { get; set; }
        public int VehiclesProcessed { get; set; }
        public int VehiclesFound { get; set; }
        public int VehiclesWithoutImages { get; set; }
        public int VehiclesSynchronized { get; set; }
        public int RecordsCorrected { get; set; }
        public int Errors { get; set; }
        public int LastLogIndex { get; set; }
        public List<R2VehicleImageSyncLogEntry> Logs { get; } = [];
        public CancellationTokenSource? Cancellation { get; set; }

        public static MutableState Idle()
            => new();

        public static MutableState Start(string? userId, string? userName, CancellationTokenSource cancellation)
            => new()
            {
                RunId = Guid.NewGuid(),
                Status = R2VehicleImageSyncStatus.Pending,
                StartedAtUtc = DateTimeOffset.UtcNow,
                StartedBy = string.IsNullOrWhiteSpace(userName) ? userId : userName,
                Cancellation = cancellation
            };
    }
}
