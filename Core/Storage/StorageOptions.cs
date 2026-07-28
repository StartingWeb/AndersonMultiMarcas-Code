namespace Core.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = StorageProviders.Local;
    public bool DualReadEnabled { get; set; } = true;
    public string? PublicBaseUrl { get; set; }
    public R2StorageOptions R2 { get; set; } = new();

    public bool UseR2ForWrites =>
        string.Equals(Provider, StorageProviders.R2, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Provider, "CloudflareR2", StringComparison.OrdinalIgnoreCase);
}

public static class StorageProviders
{
    public const string Local = "Local";
    public const string R2 = "R2";
}

public sealed class R2StorageOptions
{
    public string? AccountId { get; set; }
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public string? BucketName { get; set; }
    public string? PublicBaseUrl { get; set; }
    public string? ServiceUrl { get; set; }
    public string Region { get; set; } = "auto";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccessKeyId)
        && !string.IsNullOrWhiteSpace(SecretAccessKey)
        && !string.IsNullOrWhiteSpace(BucketName)
        && (!string.IsNullOrWhiteSpace(ServiceUrl) || !string.IsNullOrWhiteSpace(AccountId));

    public string ResolveServiceUrl()
    {
        if (!string.IsNullOrWhiteSpace(ServiceUrl))
        {
            return ServiceUrl.TrimEnd('/');
        }

        if (string.IsNullOrWhiteSpace(AccountId))
        {
            throw new InvalidOperationException("Configure Storage:R2:AccountId ou Storage:R2:ServiceUrl para usar Cloudflare R2.");
        }

        return $"https://{AccountId.Trim()}.r2.cloudflarestorage.com";
    }
}
