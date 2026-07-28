namespace Project.Features.Storage.Legacy;

public static class LegacyImageImportJobStatus
{
    public const string Pending = "Pendente";
    public const string Running = "EmExecucao";
    public const string Cancelling = "CancelamentoSolicitado";
    public const string Cancelled = "Cancelado";
    public const string Completed = "Concluido";
    public const string CompletedWithFailures = "ConcluidoComFalhas";
    public const string Failed = "Falha";

    public static readonly string[] Active =
    [
        Pending,
        Running,
        Cancelling
    ];

    public static readonly string[] Recoverable =
    [
        Pending,
        Running
    ];

    public static bool IsTerminal(string? status)
        => status is Cancelled or Completed or CompletedWithFailures or Failed;
}

public static class LegacyImageImportItemStatus
{
    public const string Pending = "Pendente";
    public const string Running = "EmExecucao";
    public const string Completed = "Concluido";
    public const string Ignored = "Ignorado";
    public const string Failed = "Erro";
    public const string Review = "PendenteRevisao";

    public static readonly string[] Terminal =
    [
        Completed,
        Ignored,
        Failed,
        Review
    ];
}

public sealed record LegacyImageImportLogEntry(
    int Index,
    DateTimeOffset TimestampUtc,
    int? VehicleId,
    int? ImageIndex,
    string Stage,
    string Status,
    string Message,
    string? ImageUrl);

public sealed record LegacyImageImportSnapshot(
    int Id,
    string Status,
    int? CurrentVehicleId,
    int TotalVehicles,
    int VehiclesProcessed,
    int VehiclesRemaining,
    int VehiclesImported,
    int VehiclesSkipped,
    int VehiclesWithError,
    int ImagesDownloaded,
    int ImagesUploaded,
    int ImagesSkipped,
    int Failures,
    TimeSpan Elapsed,
    TimeSpan EstimatedRemaining,
    double AverageImportRate,
    IReadOnlyList<LegacyImageImportLogEntry> Logs,
    int TotalImages = 0,
    int ImagesPending = 0);

public sealed record LegacyImageImportJobListItem(
    int Id,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? UserName,
    bool DryRun,
    bool OnlyWithoutBlobName,
    bool OverwriteExisting,
    int TotalVehicles,
    int VehiclesProcessed,
    int TotalImages,
    int ImagesImported,
    int ImagesSkipped,
    int ImagesWithError,
    string? LastMessage);
