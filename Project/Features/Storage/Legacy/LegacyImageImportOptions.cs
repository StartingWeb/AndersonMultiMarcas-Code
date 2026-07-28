namespace Project.Features.Storage.Legacy;

public sealed class LegacyImageImportOptions
{
    public const string SectionName = "LegacyImageImport";

    public int MaxParallelDownloads { get; set; } = 4;
    public int MaxParallelUploads { get; set; } = 2;
    public int MaxAttempts { get; set; } = 3;
    public int DownloadTimeoutSeconds { get; set; } = 45;
    public int PageTimeoutSeconds { get; set; } = 45;
    public int LockTimeoutMinutes { get; set; } = 10;
    public string[] AllowedHosts { get; set; } =
    [
        "andersonmultimarcas.com.br",
        "www.andersonmultimarcas.com.br"
    ];

    public int SafeMaxParallelDownloads => Math.Clamp(MaxParallelDownloads, 1, 16);
    public int SafeMaxParallelUploads => Math.Clamp(MaxParallelUploads, 1, 8);
    public int SafeMaxAttempts => Math.Clamp(MaxAttempts, 1, 10);
    public TimeSpan SafeDownloadTimeout => TimeSpan.FromSeconds(Math.Clamp(DownloadTimeoutSeconds, 5, 300));
    public TimeSpan SafePageTimeout => TimeSpan.FromSeconds(Math.Clamp(PageTimeoutSeconds, 5, 300));
    public TimeSpan SafeLockTimeout => TimeSpan.FromMinutes(Math.Clamp(LockTimeoutMinutes, 1, 120));
}
