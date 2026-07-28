using Core.Storage;
using Data;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Project.Infrastructure.Storage;
using Project.Shared;
using System.Globalization;
using System.Linq;

namespace Project.Pages.Admin;

[Authorize]
public class IndexModel(ApplicationDbContext db, IStorageImageResolver imageResolver) : PageModel
{
    public int TotalVeiculosAtivos { get; private set; }
    public decimal ValorTotalEstoque { get; private set; }
    public int VendasHoje { get; private set; }
    public int EstoqueCritico { get; private set; }
    public int TotalLojas { get; private set; }
    public int LojasAtivas { get; private set; }
    public int VendedoresAtivos { get; private set; }
    public int VeiculosDestaque { get; private set; }
    public string MesReferenciaRanking { get; private set; } = string.Empty;
    public IReadOnlyList<VehicleStockItem> VeiculosNoPatio { get; private set; } = [];
    public IReadOnlyList<VehicleEngagementItem> VeiculosMaisCliques { get; private set; } = [];
    public IReadOnlyList<VehicleEngagementItem> VeiculosMaisVisualizacoes { get; private set; } = [];
    public IReadOnlyList<SellerRankingItem> RankingVendedores { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Painel administrativo";
        ViewData["Robots"] = "noindex,nofollow";

        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var inicioProximoMes = inicioMes.AddMonths(1);
        MesReferenciaRanking = inicioMes.ToString("MMMM/yyyy", new CultureInfo("pt-BR"));

        var veiculosAtivos = db.Veiculos
            .AsNoTracking()
            .Where(x => x.Ativo && !x.Vendido);

        TotalVeiculosAtivos = await veiculosAtivos.CountAsync();
        ValorTotalEstoque = (await veiculosAtivos
            .Select(x => x.PrecoVenda)
            .ToListAsync())
            .Sum(x => x.Valor);
        VendasHoje = await db.Veiculos.AsNoTracking()
            .CountAsync(x => x.Vendido && x.DataVenda.HasValue && x.DataVenda.Value.Date == hoje);
        EstoqueCritico = await veiculosAtivos.CountAsync(x => x.DataCadastro <= hoje.AddDays(-100));
        TotalLojas = await db.Lojas.AsNoTracking().CountAsync();
        LojasAtivas = await db.Lojas.AsNoTracking().CountAsync(x => x.Ativo);
        VendedoresAtivos = await db.Vendedores.AsNoTracking().CountAsync(x => x.Ativo);
        VeiculosDestaque = await veiculosAtivos.CountAsync(x => x.Destaque);

        var rankingVendedores = await db.Veiculos
            .AsNoTracking()
            .Where(x =>
                x.Vendido &&
                x.DataVenda.HasValue &&
                x.DataVenda.Value >= inicioMes &&
                x.DataVenda.Value < inicioProximoMes &&
                x.VendedorId.HasValue)
            .Select(x => new
            {
                Id = x.VendedorId!.Value,
                Nome = x.Vendedor != null ? x.Vendedor.Nome : string.Empty,
                FotoUrl = x.Vendedor != null ? x.Vendedor.FotoUrl : null
            })
            .GroupBy(x => new { x.Id, x.Nome, x.FotoUrl })
            .Select(x => new
            {
                x.Key.Id,
                x.Key.Nome,
                x.Key.FotoUrl,
                TotalVendas = x.Count()
            })
            .OrderByDescending(x => x.TotalVendas)
            .ThenBy(x => x.Nome)
            .Take(5)
            .ToListAsync(ct);

        var rankingItems = new List<SellerRankingItem>();
        foreach (var vendedor in rankingVendedores)
        {
            rankingItems.Add(new SellerRankingItem(
                vendedor.Id,
                vendedor.Nome,
                await imageResolver.ResolveSellerPhotoAsync(vendedor.FotoUrl, ct),
                vendedor.TotalVendas));
        }

        RankingVendedores = rankingItems;

        var engagementItems = await veiculosAtivos
            .Select(x => new
            {
                x.Id,
                Nome = BuildNomeSemDuplicacao(x.Titulo, x.Modelo, x.Versao),
                Midias = x.Midias
                    .Where(m => m.Ativo && m.Tipo == TipoMidia.Imagem)
                    .OrderByDescending(m => m.Capa)
                    .ThenBy(m => m.Ordem)
                    .ThenBy(m => m.Id)
                    .Select(m => new MediaProjection
                    {
                        Url = m.Url,
                        BlobName = m.BlobName,
                        Container = m.Container,
                        NomeArquivo = m.NomeArquivo,
                        ContentType = m.ContentType,
                        TamanhoBytes = m.TamanhoBytes
                    })
                    .ToList(),
                Preco = x.PrecoVenda.Valor,
                Cliques = x.QuantidadeCliques,
                Visualizacoes = x.QuantidadeVisualizacoes
            })
            .ToListAsync(ct);

        var veiculosMaisCliques = new List<VehicleEngagementItem>();
        foreach (var x in engagementItems
            .OrderByDescending(x => x.Cliques)
            .ThenByDescending(x => x.Visualizacoes)
            .ThenByDescending(x => x.Id)
            .Take(6))
        {
            veiculosMaisCliques.Add(new VehicleEngagementItem(
                x.Id,
                x.Nome,
                await SelectDashboardVehicleCoverAsync(x.Midias, ct),
                x.Preco,
                x.Cliques,
                x.Visualizacoes));
        }

        VeiculosMaisCliques = veiculosMaisCliques;

        var veiculosMaisVisualizacoes = new List<VehicleEngagementItem>();
        foreach (var x in engagementItems
            .OrderByDescending(x => x.Visualizacoes)
            .ThenByDescending(x => x.Cliques)
            .ThenByDescending(x => x.Id)
            .Take(6))
        {
            veiculosMaisVisualizacoes.Add(new VehicleEngagementItem(
                x.Id,
                x.Nome,
                await SelectDashboardVehicleCoverAsync(x.Midias, ct),
                x.Preco,
                x.Cliques,
                x.Visualizacoes));
        }

        VeiculosMaisVisualizacoes = veiculosMaisVisualizacoes;

        var veiculos = await veiculosAtivos
            .Include(x => x.Loja)
            .Include(x => x.Midias)
            .OrderBy(x => x.DataCadastro)
            .Take(24)
            .Select(x => new
            {
                x.Id,
                x.Titulo,
                x.Modelo,
                x.Versao,
                x.AnoFabricacao,
                x.AnoModelo,
                x.Quilometragem,
                x.DataCadastro,
                x.PrecoVenda,
                Loja = x.Loja.Nome,
                Midias = x.Midias
                    .Where(m => m.Ativo && m.Tipo == TipoMidia.Imagem)
                    .OrderByDescending(m => m.Capa)
                    .ThenBy(m => m.Ordem)
                    .ThenBy(m => m.Id)
                    .Select(m => new MediaProjection
                    {
                        Url = m.Url,
                        BlobName = m.BlobName,
                        Container = m.Container,
                        NomeArquivo = m.NomeArquivo,
                        ContentType = m.ContentType,
                        TamanhoBytes = m.TamanhoBytes
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        var veiculosNoPatio = new List<VehicleStockItem>();
        foreach (var x in veiculos)
        {
            var diasNoPatio = Math.Max(0, (hoje - x.DataCadastro.Date).Days);
            var imagem = await SelectDashboardVehicleCoverAsync(x.Midias, ct);

            veiculosNoPatio.Add(new VehicleStockItem(
                x.Id,
                BuildNomeSemDuplicacao(x.Titulo, x.Modelo, x.Versao),
                x.Loja,
                x.AnoFabricacao.HasValue ? $"{x.AnoFabricacao}/{x.AnoModelo}" : x.AnoModelo.ToString(CultureInfo.InvariantCulture),
                x.Quilometragem,
                x.PrecoVenda.Valor,
                x.DataCadastro,
                diasNoPatio,
                imagem));
        }

        VeiculosNoPatio = veiculosNoPatio
            .OrderByDescending(x => x.DiasNoPatio)
            .ToList();
    }

    public sealed record VehicleStockItem(
        int Id,
        string Nome,
        string Loja,
        string Ano,
        int? Quilometragem,
        decimal Preco,
        DateTime DataEntrada,
        int DiasNoPatio,
        string Imagem);

    public sealed record VehicleEngagementItem(
        int Id,
        string Nome,
        string? Imagem,
        decimal Preco,
        int Cliques,
        int Visualizacoes);

    public sealed record SellerRankingItem(
        int Id,
        string Nome,
        string? FotoUrl,
        int TotalVendas);

    private static string BuildNomeSemDuplicacao(string? titulo, string? modelo, string? versao)
    {
        var tituloLimpo = (titulo ?? string.Empty).Trim();
        var modeloLimpo = (modelo ?? string.Empty).Trim();
        var versaoLimpa = (versao ?? string.Empty).Trim();

        var incluirModelo = !string.IsNullOrWhiteSpace(modeloLimpo)
            && !string.Equals(tituloLimpo, modeloLimpo, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(versaoLimpa))
        {
            return incluirModelo ? $"{tituloLimpo} {modeloLimpo}" : tituloLimpo;
        }

        return incluirModelo
            ? $"{tituloLimpo} {modeloLimpo} {versaoLimpa}"
            : $"{tituloLimpo} {versaoLimpa}";
    }

    private Task<string> SelectDashboardVehicleCoverAsync(IEnumerable<MediaProjection> sources, CancellationToken ct)
        => imageResolver.SelectVehicleCoverAsync(sources.Select(ToStorageReference), ct);

    private static StorageImageReference ToStorageReference(MediaProjection media)
        => new(media.Url, media.BlobName, media.Container, media.NomeArquivo, media.ContentType, media.TamanhoBytes);

    private sealed class MediaProjection
    {
        public string? Url { get; init; }
        public string? BlobName { get; init; }
        public string? Container { get; init; }
        public string? NomeArquivo { get; init; }
        public string? ContentType { get; init; }
        public long? TamanhoBytes { get; init; }
    }
}
