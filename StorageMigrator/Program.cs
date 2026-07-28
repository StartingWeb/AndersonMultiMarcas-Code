using Core.Storage;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StorageMigrator;

var builder = Host.CreateApplicationBuilder(args);

foreach (var appsettings in CandidateAppSettings())
{
    builder.Configuration.AddJsonFile(appsettings, optional: true, reloadOnChange: false);
}

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<StorageMigrationOptions>(builder.Configuration.GetSection(StorageMigrationOptions.SectionName));
builder.Services.AddSingleton<R2StorageService>();
builder.Services.AddTransient<StorageMigrationRunner>();

var migrationOptions = builder.Configuration.GetSection(StorageMigrationOptions.SectionName).Get<StorageMigrationOptions>() ?? new StorageMigrationOptions();
var productionConnectionString = builder.Configuration.GetConnectionString("DefaultConnectionProd");
ValidateProductionConnectionString(productionConnectionString, migrationOptions.AllowLocalDatabase);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(productionConnectionString);
});

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("StorageMigrator");

logger.LogWarning("Este executavel nao aplica migrations de banco e nao migra imagens durante deploy. Ele deve ser executado manualmente no servidor de producao.");

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
