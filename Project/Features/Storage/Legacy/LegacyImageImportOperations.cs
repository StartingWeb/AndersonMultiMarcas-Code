namespace Project.Features.Storage.Legacy;

public sealed class LegacyImageImportFilters
{
    public string? Status { get; init; }
    public string? Search { get; init; }
    public int? VehicleId { get; init; }
    public string? Marca { get; init; }
    public string? Modelo { get; init; }
    public DateTime? PeriodStart { get; init; }
    public DateTime? PeriodEnd { get; init; }
    public string? User { get; init; }
    public bool OnlyErrors { get; init; }
    public bool OnlyPending { get; init; }
    public bool OnlyCompleted { get; init; }
}

public sealed record LegacyImageImportDashboardSnapshot(
    int ActiveJobs,
    int CompletedJobs,
    int FailedJobs,
    int VehiclesProcessed,
    int VehiclesRemaining,
    int ImagesImported,
    int ImagesPending,
    int ImagesWithError,
    int ImagesSkipped,
    TimeSpan Elapsed,
    TimeSpan EstimatedRemaining,
    double VehiclesPerMinute,
    double ImagesPerMinute,
    TimeSpan AverageVehicleTime,
    TimeSpan AverageImageTime,
    TimeSpan MaxProcessingTime,
    TimeSpan MinProcessingTime,
    double SuccessRate,
    double ErrorRate,
    double RetryRate);

public sealed record LegacyImageImportJobDetails(
    LegacyImageImportSnapshot Summary,
    IReadOnlyList<LegacyImageImportVehicleDetail> Vehicles,
    IReadOnlyList<LegacyImageImportImageDetail> Images,
    IReadOnlyList<LegacyImageImportLogEntry> Logs,
    IReadOnlyList<LegacyImageImportImageDetail> Errors,
    IReadOnlyList<LegacyImageImportHistoryEntry> History,
    LegacyImageImportMetrics Metrics,
    string? ConsolidatedReportJson);

public sealed record LegacyImageImportVehicleDetail(
    int VehicleId,
    string? Title,
    string? Brand,
    string? Model,
    int TotalImages,
    int ImportedImages,
    int PendingImages,
    int ErrorImages,
    int SkippedImages,
    string Status,
    TimeSpan ProcessingTime);

public sealed record LegacyImageImportImageDetail(
    int ItemId,
    int VehicleId,
    int Order,
    string? VehicleName,
    string SourceUrl,
    string? StoredUrl,
    string BlobName,
    string Status,
    string? ContentType,
    long? SizeBytes,
    DateTimeOffset? ImportedAt,
    TimeSpan ProcessingTime,
    int Attempts,
    int MaxAttempts,
    string? Error);

public sealed record LegacyImageImportHistoryEntry(
    int Id,
    string Type,
    DateTimeOffset CreatedAt,
    string? UserName,
    int? Quantity,
    TimeSpan? Duration,
    string? Result,
    string? Message);

public sealed record LegacyImageImportMetrics(
    TimeSpan TotalTime,
    TimeSpan AverageVehicleTime,
    TimeSpan AverageImageTime,
    TimeSpan MaxProcessingTime,
    TimeSpan MinProcessingTime,
    double UploadsPerMinute,
    double DownloadsPerMinute,
    int Retries,
    int Failures,
    int Ignored,
    int Imported,
    int Cancelled,
    double SuccessRate,
    double ErrorRate,
    double RetryRate,
    long EstimatedLocalStorageSavingsBytes);

public sealed record LegacyImageImportConsolidatedReport(
    int JobId,
    int TotalVehiclesAnalyzed,
    int TotalVehiclesImported,
    int TotalImagesProcessed,
    int TotalImagesImported,
    int TotalImagesIgnored,
    int TotalErrors,
    TimeSpan TotalTime,
    double AverageSpeedImagesPerMinute,
    double SuccessRate,
    double ErrorRate,
    long EstimatedLocalStorageSavingsBytes,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? ResponsibleUser);

public sealed record LegacyImageImportExportPayload(
    object Job,
    IReadOnlyList<LegacyImageImportVehicleDetail> Vehicles,
    IReadOnlyList<LegacyImageImportImageDetail> Images,
    IReadOnlyList<LegacyImageImportLogEntry> Logs,
    IReadOnlyList<LegacyImageImportHistoryEntry> History,
    LegacyImageImportMetrics Metrics,
    string? ConsolidatedReportJson);
