using Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Project.Features.Storage.Legacy;
using Project.Shared;
using Xunit;

namespace Project.Tests.Features.Storage.Legacy;

public sealed class LegacyImageImportReportServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_DeveCalcularRetriesComExpressaoTraduzivel()
    {
        await using var db = CreateDbContext();
        var job = new ImportJob("https://andersonmultimarcas.com.br", dryRun: false, somenteSemBlobName: true, sobrescrever: false, idInicial: null, quantidadeMaxima: null, usuarioId: "1", usuarioNome: "Operador");
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        var first = new ImportJobItem(job.Id, 10, null, 0, true, "https://andersonmultimarcas.com.br/a.jpg", "a.jpg", "legacy/a.jpg", 3);
        first.IncrementAttempt();
        first.IncrementAttempt();
        first.MarkSucceeded();

        var second = new ImportJobItem(job.Id, 11, null, 0, true, "https://andersonmultimarcas.com.br/b.jpg", "b.jpg", "legacy/b.jpg", 3);
        second.IncrementAttempt();
        second.MarkFailed("Falha no download.");

        var third = new ImportJobItem(job.Id, 12, null, 0, true, "https://andersonmultimarcas.com.br/c.jpg", "c.jpg", "legacy/c.jpg", 3);
        third.IncrementAttempt();
        third.IncrementAttempt();
        third.IncrementAttempt();
        third.RequestReview("Associacao pendente.");

        db.ImportJobItems.AddRange(first, second, third);
        await db.SaveChangesAsync();

        var service = new LegacyImageImportReportService(db);
        var dashboard = await service.GetDashboardAsync(new LegacyImageImportFilters(), CancellationToken.None);

        Assert.Equal(3, dashboard.ImagesImported + dashboard.ImagesWithError);
        Assert.Equal(100d, dashboard.RetryRate);
    }

    [Fact]
    public void DashboardRetriesExpression_DeveGerarCaseWhenParaSqlServer()
    {
        using var db = CreateSqlServerTranslationDbContext();

        var sql = db.ImportJobItems
            .GroupBy(x => 1)
            .Select(x => x.Sum(i => i.Tentativas > 1 ? i.Tentativas - 1 : 0))
            .ToQueryString();

        Assert.Contains("CASE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildConsolidatedReportAsync_DeveCalcularResumoPersistido()
    {
        await using var db = CreateDbContext();
        var job = new ImportJob("https://andersonmultimarcas.com.br", dryRun: false, somenteSemBlobName: true, sobrescrever: false, idInicial: null, quantidadeMaxima: null, usuarioId: "1", usuarioNome: "Operador");
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        job.MarkRunning("worker", DateTime.UtcNow.AddMinutes(5));
        job.SetTotals(3, 4);
        job.UpdateProgress(3, importedImages: 2, skippedImages: 1, failedImages: 1);
        job.MarkCompleted();
        await db.SaveChangesAsync();

        var service = new LegacyImageImportReportService(db);
        var report = await service.BuildConsolidatedReportAsync(job.Id, CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal(3, report!.TotalVehiclesAnalyzed);
        Assert.Equal(4, report.TotalImagesProcessed);
        Assert.Equal("Operador", report.ResponsibleUser);
    }

    [Fact]
    public void SimpleSpreadsheetExporter_DeveGerarArquivoXlsx()
    {
        var bytes = SimpleSpreadsheetExporter.CreateWorkbook(new[]
        {
            new SpreadsheetSheet("Resumo", new List<IReadOnlyList<string?>>
            {
                new[] { "Campo", "Valor" },
                new[] { "Total", "1" }
            })
        });

        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSqlServerTranslationDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=TranslationOnly;User Id=sa;Password=Password123!;TrustServerCertificate=True")
            .Options;

        return new ApplicationDbContext(options);
    }
}
