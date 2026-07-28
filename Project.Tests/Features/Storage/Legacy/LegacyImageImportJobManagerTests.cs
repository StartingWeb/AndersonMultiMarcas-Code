using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Project.Features.Storage.Legacy;
using Xunit;

namespace Project.Tests.Features.Storage.Legacy;

public sealed class LegacyImageImportJobManagerTests
{
    [Fact]
    public async Task StartAsync_DevePersistirJobELog()
    {
        await using var db = CreateDbContext();
        var manager = CreateManager(db);

        var job = await manager.StartAsync(new LegacyImageImportRequest
        {
            BaseUrl = "https://andersonmultimarcas.com.br",
            DryRun = true,
            OnlyWithoutBlobName = true
        }, "user-1", "Operador", CancellationToken.None);

        Assert.True(job.Id > 0);
        Assert.Equal(LegacyImageImportJobStatus.Pending, job.Status);
        Assert.True(await db.ImportJobs.AnyAsync(x => x.Id == job.Id));
        Assert.True(await db.ImportJobLogs.AnyAsync(x => x.ImportJobId == job.Id && x.Etapa == "Inicio"));
    }

    [Fact]
    public async Task StartAsync_DeveBloquearQuandoExisteJobAtivo()
    {
        await using var db = CreateDbContext();
        var manager = CreateManager(db);

        await manager.StartAsync(new LegacyImageImportRequest
        {
            BaseUrl = "https://andersonmultimarcas.com.br",
            DryRun = true
        }, null, null, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.StartAsync(new LegacyImageImportRequest
        {
            BaseUrl = "https://andersonmultimarcas.com.br",
            DryRun = true
        }, null, null, CancellationToken.None));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static LegacyImageImportJobManager CreateManager(ApplicationDbContext db)
    {
        var queue = new LegacyImageImportQueue();
        var cancellationRegistry = new LegacyImportCancellationRegistry();
        var options = Options.Create(new LegacyImageImportOptions());
        return new LegacyImageImportJobManager(db, queue, cancellationRegistry, options);
    }
}
