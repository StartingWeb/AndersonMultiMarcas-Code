using Data;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Project.Shared;
using System.Globalization;
using System.Linq;

namespace Project.Pages.Admin;

[Authorize]
public class IndexModel(ApplicationDbContext db) : PageModel
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

    public async Task OnGetAsync()
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

        RankingVendedores = await db.Veiculos
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
            .Select(x => new SellerRankingItem(
                x.Id,
                x.Nome,
                SellerImageHelper.Normalize(x.FotoUrl),
                x.TotalVendas))
            .ToListAsync();

        var engagementItems = await db.Veiculos
            .AsNoTracking()
            .Where(x => x.Ativo)
            .Select(x => new
            {
                x.Id,
                Nome = string.IsNullOrWhiteSpace(x.Versao)
                    ? $"{x.Titulo} {x.Modelo}"
                    : $"{x.Titulo} {x.Modelo} {x.Versao}",
                Imagem = x.Midias
                    .Where(m => m.Ativo && m.Tipo == TipoMidia.Imagem)
                    .OrderByDescending(m => m.Capa)
                    .ThenBy(m => m.Ordem)
                    .Select(m => m.Url)
                    .FirstOrDefault(),
                Preco = x.PrecoVenda.Valor,
                Cliques = x.QuantidadeCliques,
                Visualizacoes = x.QuantidadeVisualizacoes
            })
            .ToListAsync();

        VeiculosMaisCliques = engagementItems
            .OrderByDescending(x => x.Cliques)
            .ThenByDescending(x => x.Visualizacoes)
            .ThenByDescending(x => x.Id)
            .Take(6)
            .Select(x => new VehicleEngagementItem(
                x.Id,
                x.Nome,
                VehicleImageHelper.Normalize(x.Imagem),
                x.Preco,
                x.Cliques,
                x.Visualizacoes))
            .ToList();

        VeiculosMaisVisualizacoes = engagementItems
            .OrderByDescending(x => x.Visualizacoes)
            .ThenByDescending(x => x.Cliques)
            .ThenByDescending(x => x.Id)
            .Take(6)
            .Select(x => new VehicleEngagementItem(
                x.Id,
                x.Nome,
                VehicleImageHelper.Normalize(x.Imagem),
                x.Preco,
                x.Cliques,
                x.Visualizacoes))
            .ToList();

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
                Imagem = x.Midias
                    .Where(m => m.Ativo && m.Tipo == TipoMidia.Imagem)
                    .OrderByDescending(m => m.Capa)
                    .ThenBy(m => m.Ordem)
                    .Select(m => m.Url)
                    .FirstOrDefault()
            })
            .ToListAsync();

        VeiculosNoPatio = veiculos
            .Select(x =>
            {
                var diasNoPatio = Math.Max(0, (hoje - x.DataCadastro.Date).Days);
                var imagem = VehicleImageHelper.Normalize(x.Imagem);

                return new VehicleStockItem(
                    x.Id,
                    string.IsNullOrWhiteSpace(x.Versao) ? $"{x.Titulo} {x.Modelo}" : $"{x.Titulo} {x.Modelo} {x.Versao}",
                    x.Loja,
                    x.AnoFabricacao.HasValue ? $"{x.AnoFabricacao}/{x.AnoModelo}" : x.AnoModelo.ToString(CultureInfo.InvariantCulture),
                    x.Quilometragem,
                    x.PrecoVenda.Valor,
                    x.DataCadastro,
                    diasNoPatio,
                    imagem);
            })
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
}
