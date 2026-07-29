namespace StorageMigrator;

public sealed class StorageAuditOptions
{
    public const string SectionName = "StorageAudit";

    public bool Execute { get; set; }
    public string? Prefix { get; set; }
    public bool ValidatePublicUrls { get; set; } = true;
    public int MetadataParallelism { get; set; } = 24;
    public int PublicUrlParallelism { get; set; } = 16;
    public string BaseUrl { get; set; } = "https://andersonmultimarcas.com.br";
    public bool UploadProbe { get; set; }
    public bool KeepProbeObject { get; set; }
    public string? ProbeKey { get; set; }
    public bool TestImport { get; set; }
    public int? TestVehicleId { get; set; }
    public bool ReimportOrphans { get; set; }
    public int? MaxReimportVehicles { get; set; }
    public string? OutputPath { get; set; }

    public int SafeMetadataParallelism => Math.Clamp(MetadataParallelism, 1, 64);
    public int SafePublicUrlParallelism => Math.Clamp(PublicUrlParallelism, 1, 64);
}
