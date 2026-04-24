using Core.Dtos;
using Core.Enums;
using Core.Interfaces;
using Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Project.Pages.Admin;

public class IndexModel : PageModel
{
    private const int DiasEstoqueCritico = 90;
    private const int DiasEstoqueAcao = 100;

    private readonly ILojaService _lojaService;
    private readonly IVendedorService _vendedorService;
    private readonly IVeiculoService _veiculoService;

    public IndexModel(
        ILojaService lojaService,
        IVendedorService vendedorService,
        IVeiculoService veiculoService)
    {
        _lojaService = lojaService;
        _vendedorService = vendedorService;
        _veiculoService = veiculoService;
    }

    public DashboardViewModel Dashboard { get; private set; } = DashboardViewModel.Empty;
    public bool IsRestrictedDashboard { get; private set; }
    public string? DashboardLoadMessage { get; private set; }

    public async Task OnGetAsync()
    {
        IsRestrictedDashboard =
            User.IsInRole("AdminConcessionaria") &&
            !User.IsInRole("Administrador") &&
            !User.IsInRole("Desenvolvedor");

        try
        {
            var hoje = DateTime.Today;
            var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
            var mensagensErro = new List<string>();

            // Os services compartilham o mesmo DbContext scoped por request.
            // Executar em paralelo dispara concorrência no EF Core.
            var lojasResult = await _lojaService.ListarAsync();
            var vendedoresResult = await _vendedorService.ListarAsync();
            var veiculosResult = await _veiculoService.ListarAsync();

            var lojas = lojasResult.Status == PackageStatus.Success
                ? (lojasResult.Data ?? [])
                : [];

            var vendedores = vendedoresResult.Status == PackageStatus.Success
                ? (vendedoresResult.Data ?? [])
                : [];

            var veiculos = veiculosResult.Status == PackageStatus.Success
                ? (veiculosResult.Data ?? [])
                : [];

            if (lojasResult.Status != PackageStatus.Success)
            {
                mensagensErro.Add("lojas");
            }

            if (vendedoresResult.Status != PackageStatus.Success)
            {
                mensagensErro.Add("vendedores");
            }

            if (veiculosResult.Status != PackageStatus.Success)
            {
                mensagensErro.Add("veículos");
            }

            var veiculosAtivos = veiculos
                .Where(veiculo => veiculo.Ativo && !veiculo.Vendido)
                .ToList();

            var valorEmEstoque = veiculosAtivos.Sum(ObterPrecoPrincipal);
            var vendidosHoje = veiculos.Count(veiculo =>
                veiculo.Vendido &&
                veiculo.DataVenda.HasValue &&
                veiculo.DataVenda.Value.Date == hoje);

            var veiculosCriticos = veiculosAtivos
                .Select(veiculo => new
                {
                    Veiculo = veiculo,
                    DiasParado = CalcularDiasEmEstoque(veiculo, hoje)
                })
                .Where(item => item.DiasParado >= DiasEstoqueCritico)
                .ToList();

            var rankingVendedores = veiculos
                .Where(veiculo =>
                    veiculo.Vendido &&
                    veiculo.DataVenda.HasValue &&
                    veiculo.DataVenda.Value.Date >= inicioMes &&
                    veiculo.DataVenda.Value.Date <= hoje &&
                    veiculo.VendedorId.HasValue)
                .GroupBy(veiculo => veiculo.VendedorId!.Value)
                .Select(group =>
                {
                    var primeiro = group
                        .OrderByDescending(veiculo => veiculo.DataVenda)
                        .First();

                    var vendedor = primeiro.Vendedor ?? vendedores.FirstOrDefault(item => item.Id == group.Key);

                    return new SellerRankingItem
                    {
                        VendedorId = group.Key,
                        Nome = !string.IsNullOrWhiteSpace(vendedor?.Nome)
                            ? vendedor.Nome
                            : $"Vendedor #{group.Key}",
                        FotoUrl = NormalizarFotoVendedor(vendedor?.FotoUrl),
                        Loja = vendedor?.Loja?.Nome,
                        VeiculosVendidos = group.Count()
                    };
                })
                .OrderByDescending(item => item.VeiculosVendidos)
                .ThenBy(item => item.Nome)
                .Take(5)
                .ToList();

            var veiculosParaAcao = veiculosCriticos
                .Where(item => item.DiasParado >= DiasEstoqueAcao)
                .OrderByDescending(item => item.DiasParado)
                .ThenByDescending(item => ObterPrecoPrincipal(item.Veiculo))
                .Take(6)
                .Select(item => DashboardVehicleItem.From(item.Veiculo, item.DiasParado))
                .ToList();

            Dashboard = new DashboardViewModel
            {
                DataReferencia = hoje,
                TotalVeiculosNoSite = veiculosAtivos.Count,
                ValorEmEstoque = valorEmEstoque,
                VendidosHoje = vendidosHoje,
                VeiculosParados90Dias = veiculosCriticos.Count,
                TotalLojas = lojas.Count,
                LojasAtivas = lojas.Count(loja => loja.Ativo),
                VendedoresAtivos = vendedores.Count(vendedor => vendedor.Ativo),
                VeiculosEmDestaque = veiculosAtivos.Count(veiculo => veiculo.Destaque),
                RankingVendedoresMes = rankingVendedores,
                VeiculosParaAcao = veiculosParaAcao,
                FalhaAoCarregarDados = false
            };

            DashboardLoadMessage = mensagensErro.Count > 0
                ? $"Alguns dados nao puderam ser carregados ({string.Join(", ", mensagensErro)}), mas o dashboard foi montado com o restante."
                : null;
        }
        catch (Exception)
        {
            DashboardLoadMessage = "Nao foi possivel carregar os indicadores do dashboard.";
            Dashboard = new DashboardViewModel
            {
                DataReferencia = DateTime.Today,
                FalhaAoCarregarDados = true
            };
        }
    }

    private static int CalcularDiasEmEstoque(Veiculo veiculo, DateTime hoje)
    {
        return Math.Max(0, (hoje - veiculo.DataCadastro.Date).Days);
    }

    private static decimal ObterPrecoPrincipal(Veiculo veiculo)
    {
        if (veiculo.PrecoVenda.HasValue && veiculo.PrecoVenda.Value > 0)
        {
            return veiculo.PrecoVenda.Value;
        }

        return 0m;
    }

    private static string NormalizarFotoVendedor(string? fotoUrl)
    {
        if (string.IsNullOrWhiteSpace(fotoUrl))
        {
            return "/img/logo.png";
        }

        if (Uri.TryCreate(fotoUrl, UriKind.Absolute, out _))
        {
            return fotoUrl;
        }

        return fotoUrl.StartsWith('/')
            ? fotoUrl
            : $"/{fotoUrl.TrimStart('/')}";
    }

    public sealed class DashboardViewModel
    {
        public static DashboardViewModel Empty { get; } = new()
        {
            DataReferencia = DateTime.Today
        };

        public DateTime DataReferencia { get; init; }
        public int TotalVeiculosNoSite { get; init; }
        public decimal ValorEmEstoque { get; init; }
        public int VendidosHoje { get; init; }
        public int VeiculosParados90Dias { get; init; }
        public int TotalLojas { get; init; }
        public int LojasAtivas { get; init; }
        public int VendedoresAtivos { get; init; }
        public int VeiculosEmDestaque { get; init; }
        public bool FalhaAoCarregarDados { get; init; }
        public IReadOnlyList<SellerRankingItem> RankingVendedoresMes { get; init; } = [];
        public IReadOnlyList<DashboardVehicleItem> VeiculosParaAcao { get; init; } = [];
    }

    public sealed class SellerRankingItem
    {
        public int VendedorId { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string FotoUrl { get; init; } = "/img/logo.png";
        public string? Loja { get; init; }
        public int VeiculosVendidos { get; init; }
    }

    public sealed class DashboardVehicleItem
    {
        public int Id { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Marca { get; init; } = string.Empty;
        public string? Loja { get; init; }
        public string ImagemUrl { get; init; } = "/img/carroDefault.png";
        public decimal Preco { get; init; }
        public int DiasParado { get; init; }
        public string Recomendacao { get; init; } = string.Empty;
        public string RecomendacaoTipo { get; init; } = "warning";

        public static DashboardVehicleItem From(Veiculo veiculo, int diasParado)
        {
            return new DashboardVehicleItem
            {
                Id = veiculo.Id,
                Titulo = MontarTitulo(veiculo),
                Marca = veiculo.Marca?.Nome ?? "Sem marca",
                Loja = veiculo.Loja?.Nome,
                ImagemUrl = ObterImagem(veiculo),
                Preco = ObterPrecoPrincipal(veiculo),
                DiasParado = diasParado,
                Recomendacao = ObterRecomendacao(diasParado),
                RecomendacaoTipo = diasParado >= 120 ? "danger" : "warning"
            };
        }

        private static string MontarTitulo(Veiculo veiculo)
        {
            var marcaModelo = string.Join(" ", new[] { veiculo.Marca?.Nome, veiculo.Modelo }
                .Where(parte => !string.IsNullOrWhiteSpace(parte)));

            if (!string.IsNullOrWhiteSpace(marcaModelo))
            {
                return marcaModelo;
            }

            if (!string.IsNullOrWhiteSpace(veiculo.Titulo))
            {
                return veiculo.Titulo;
            }

            var partes = new[]
            {
                veiculo.Marca?.Nome,
                veiculo.Modelo,
                veiculo.Versao
            };

            var titulo = string.Join(" ", partes.Where(parte => !string.IsNullOrWhiteSpace(parte)));
            return string.IsNullOrWhiteSpace(titulo) ? $"Veículo #{veiculo.Id}" : titulo;
        }

        private static string ObterImagem(Veiculo veiculo)
        {
            var midia = veiculo.Midias
                .Where(midia => midia.Ativo)
                .OrderByDescending(midia => midia.Capa)
                .ThenBy(midia => midia.Ordem)
                .FirstOrDefault();

            if (midia is null || string.IsNullOrWhiteSpace(midia.Url))
            {
                return "/img/carroDefault.png";
            }

            if (Uri.TryCreate(midia.Url, UriKind.Absolute, out _))
            {
                return midia.Url;
            }

            return midia.Url.StartsWith('/')
                ? midia.Url
                : $"/{midia.Url.TrimStart('/')}";
        }

        private static string ObterRecomendacao(int diasParado)
        {
            if (diasParado >= 120)
            {
                return "Revisar preço e reforçar campanha deste anúncio.";
            }

            return "Atualizar mídia e reposicionar o anúncio na vitrine.";
        }
    }
}
