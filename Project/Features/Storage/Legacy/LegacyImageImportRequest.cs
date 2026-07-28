namespace Project.Features.Storage.Legacy;

public sealed class LegacyImageImportRequest
{
    public string BaseUrl { get; init; } = "https://andersonmultimarcas.com.br";
    public bool OnlyWithoutBlobName { get; init; } = true;
    public bool OverwriteExisting { get; init; }
    public bool DryRun { get; init; } = true;
    public int? MaxVehicles { get; init; }
    public int? StartId { get; init; }
}

public sealed record LegacyImageWorkItem(
    int Index,
    string SourceUrl,
    string FileName,
    string StorageKey);
