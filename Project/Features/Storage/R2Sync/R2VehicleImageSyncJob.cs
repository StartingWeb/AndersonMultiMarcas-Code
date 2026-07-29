namespace Project.Features.Storage.R2Sync;

public static class R2VehicleImageSyncStatus
{
    public const string Idle = "Aguardando";
    public const string Pending = "Pendente";
    public const string Running = "EmExecucao";
    public const string Cancelling = "CancelamentoSolicitado";
    public const string Cancelled = "Cancelado";
    public const string Completed = "Concluido";
    public const string CompletedWithErrors = "ConcluidoComErros";
    public const string Failed = "Falha";

    public static bool IsActive(string? status)
        => status is Pending or Running or Cancelling;
}

public sealed record R2VehicleImageSyncProgress(
    int TotalVehicles,
    int VehiclesProcessed,
    int VehiclesFound,
    int VehiclesWithoutImages,
    int VehiclesSynchronized,
    int ImagesLinked,
    int RecordsCorrected,
    int Errors,
    int? CurrentVehicleId);

public sealed record R2VehicleImageSyncLogEntry(
    int Index,
    DateTimeOffset TimestampUtc,
    int? VehicleId,
    int? LegacyVehicleId,
    string Stage,
    string Status,
    string Message);

public sealed record R2VehicleImageSyncSnapshot(
    Guid? RunId,
    string Status,
    bool IsActive,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string? StartedBy,
    int? CurrentVehicleId,
    int TotalVehicles,
    int VehiclesProcessed,
    int VehiclesFound,
    int VehiclesWithoutImages,
    int VehiclesSynchronized,
    int ImagesLinked,
    int RecordsCorrected,
    int Errors,
    TimeSpan Elapsed,
    double ProgressPercent,
    IReadOnlyList<R2VehicleImageSyncLogEntry> Logs);
