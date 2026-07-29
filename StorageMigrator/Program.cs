using Core.Storage;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Features.Storage.Legacy;
using StorageMigrator;

var builder = Host.CreateApplicationBuilder(args);

foreach (var appsettings in CandidateAppSettings())
{
    builder.Configuration.AddJsonFile(appsettings, optional: true, reloadOnChange: false);
}

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);
AddLegacyStorageCompatibility(builder.Configuration);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<StorageMigrationOptions>(builder.Configuration.GetSection(StorageMigrationOptions.SectionName));
builder.Services.Configure<StorageAuditOptions>(builder.Configuration.GetSection(StorageAuditOptions.SectionName));
builder.Services.Configure<LegacyImageImportOptions>(builder.Configuration.GetSection(LegacyImageImportOptions.SectionName));
builder.Services.AddSingleton<R2StorageService>();
builder.Services.AddSingleton<IStorageService>(services => services.GetRequiredService<R2StorageService>());
builder.Services.AddSingleton<LegacyImageImportQueue>();
builder.Services.AddSingleton<LegacyImportCancellationRegistry>();
builder.Services.AddScoped<LegacyImageImportJobManager>();
builder.Services.AddScoped<LegacyVehicleJsonLdParser>();
builder.Services.AddScoped<ILegacyImageStorageVerifier, LegacyImageR2StorageVerifier>();
builder.Services.AddScoped<LegacyVehicleImageImportService>();
builder.Services.AddScoped<LegacyVehicleImageImportItemProcessor>();
builder.Services.AddScoped<LegacyImageImportReportService>();
builder.Services.AddTransient<StorageMigrationRunner>();
builder.Services.AddTransient<StorageAuditRunner>();
builder.Services.AddHttpClient(LegacyVehicleImageImportService.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AndersonMultimarcasStorageImporter/1.0");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false
});
builder.Services.AddHttpClient("storage-audit-public-url", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AndersonMultimarcasStorageAuditor/1.0");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false
});

var migrationOptions = builder.Configuration.GetSection(StorageMigrationOptions.SectionName).Get<StorageMigrationOptions>() ?? new StorageMigrationOptions();
var auditOptions = builder.Configuration.GetSection(StorageAuditOptions.SectionName).Get<StorageAuditOptions>() ?? new StorageAuditOptions();
var productionConnectionString = builder.Configuration.GetConnectionString("DefaultConnectionProd");
ValidateProductionConnectionString(productionConnectionString, migrationOptions.AllowLocalDatabase);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(productionConnectionString);
});

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("StorageMigrator");

logger.LogWarning("Este executavel nao aplica migrations de banco e nao migra imagens durante deploy. Ele deve ser executado manualmente no servidor de producao.");

if (auditOptions.Execute)
{
    var auditRunner = host.Services.GetRequiredService<StorageAuditRunner>();
    return await auditRunner.RunAsync(CancellationToken.None);
}

var runner = host.Services.GetRequiredService<StorageMigrationRunner>();
return await runner.RunAsync(CancellationToken.None);

static IEnumerable<string> CandidateAppSettings()
{
    var current = Directory.GetCurrentDirectory();
    yield return Path.Combine(current, "appsettings.json");
    yield return Path.Combine(current, "Project", "appsettings.json");
    yield return Path.Combine(current, "..", "Project", "appsettings.json");
    yield return Path.Combine(AppContext.BaseDirectory, "appsettings.json");
}

static void ValidateProductionConnectionString(string? connectionString, bool allowLocalDatabase)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:DefaultConnectionProd obrigatoria para o migrador.");
    }

    if (!allowLocalDatabase
        && (connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("MSSQLLocalDB", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("O migrador recusou LocalDB. Use ConnectionStrings:DefaultConnectionProd do servidor de producao.");
    }
}

static void AddLegacyStorageCompatibility(ConfigurationManager configuration)
{
    var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    AddFallback(values, configuration, "Storage:R2:AccessKeyId", "Storage:AccessKey");
    AddFallback(values, configuration, "Storage:R2:SecretAccessKey", "Storage:SecretKey");
    AddFallback(values, configuration, "Storage:R2:BucketName", "Storage:Bucket");
    AddFallback(values, configuration, "Storage:R2:ServiceUrl", "Storage:Endpoint");
    AddFallback(values, configuration, "Storage:R2:PublicBaseUrl", "Storage:PublicBaseUrl");

    if (values.Count > 0)
    {
        configuration.AddInMemoryCollection(values);
    }
}

static void AddFallback(
    IDictionary<string, string?> values,
    IConfiguration configuration,
    string targetKey,
    string sourceKey)
{
    if (!string.IsNullOrWhiteSpace(configuration[targetKey])
        || string.IsNullOrWhiteSpace(configuration[sourceKey]))
    {
        return;
    }

    values[targetKey] = configuration[sourceKey];
}
