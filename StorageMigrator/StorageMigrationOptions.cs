namespace StorageMigrator;

public sealed class StorageMigrationOptions
{
    public const string SectionName = "StorageMigration";

    public bool Execute { get; set; }
    public string? WebRootPath { get; set; }
    public int BatchSize { get; set; } = 100;
    public int? StartId { get; set; }
    public int? Limit { get; set; }
    public bool AllowLocalDatabase { get; set; }
}
