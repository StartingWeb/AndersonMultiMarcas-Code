using Core.Interfaces;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Project.Pages;

public class CatalogoModel : PageModel
{
    private readonly IVeiculoService _veiculoService;

    public CatalogoModel(IVeiculoService veiculoService)
    {
        _veiculoService = veiculoService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Busca { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Marca { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Cambio { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Combustivel { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? PrecoMinimo { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? PrecoMaximo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? AnoMinimo { get; set; }

    public IReadOnlyList<CatalogVehicleItem> Vehicles { get; private set; } = [];
    public IReadOnlyList<CatalogVehicleItem> RecentVehicles { get; private set; } = [];
    public IReadOnlyList<CatalogVehicleItem> RemainingVehicles { get; private set; } = [];

    public IReadOnlyList<string> AvailableBrands { get; private set; } = [];
    public IReadOnlyList<string> AvailableGearboxes { get; private set; } = [];
    public IReadOnlyList<string> AvailableFuels { get; private set; } = [];
    public IReadOnlyList<int> AvailableYears { get; private set; } = [];

    public int TotalEncontrado => Vehicles.Count;

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Busca) ||
        !string.IsNullOrWhiteSpace(Marca) ||
        !string.IsNullOrWhiteSpace(Cambio) ||
        !string.IsNullOrWhiteSpace(Combustivel) ||
        PrecoMinimo.HasValue ||
        PrecoMaximo.HasValue ||
        AnoMinimo.HasValue;

    public async Task OnGetAsync()
    {
        ViewData["ShowHero"] = false;

        if (PrecoMinimo.HasValue && PrecoMaximo.HasValue && PrecoMinimo > PrecoMaximo)
        {
            (PrecoMinimo, PrecoMaximo) = (PrecoMaximo, PrecoMinimo);
        }

        var response = await _veiculoService.ListarAtivosAsync();
        var veiculos = (response.Data ?? new List<Veiculo>())
            .Where(veiculo => !veiculo.Vendido)
            .ToList();

        AvailableBrands = veiculos
            .Select(veiculo => veiculo.Marca?.Nome)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .Cast<string>()
            .ToList();

        AvailableGearboxes = veiculos
            .Select(veiculo => veiculo.Cambio)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .Cast<string>()
            .ToList();

        AvailableFuels = veiculos
            .Select(veiculo => veiculo.Combustivel)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .Cast<string>()
            .ToList();

        AvailableYears = veiculos
            .Select(ObterAnoPrincipal)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .OrderByDescending(value => value)
            .ToList();

        IEnumerable<Veiculo> query = veiculos;

        if (!string.IsNullOrWhiteSpace(Busca))
        {
            var busca = Busca.Trim();
            query = query.Where(veiculo =>
                Contem(veiculo.Titulo, busca) ||
                Contem(veiculo.Modelo, busca) ||
                Contem(veiculo.Versao, busca) ||
                Contem(veiculo.Marca?.Nome, busca) ||
                Contem(veiculo.Combustivel, busca) ||
                Contem(veiculo.Cambio, busca) ||
                Contem(veiculo.Cor, busca) ||
                (ObterAnoPrincipal(veiculo)?.ToString().Contains(busca, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(Marca))
        {
            query = query.Where(veiculo => string.Equals(veiculo.Marca?.Nome, Marca, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(Cambio))
        {
            query = query.Where(veiculo => string.Equals(veiculo.Cambio, Cambio, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(Combustivel))
        {
            query = query.Where(veiculo => string.Equals(veiculo.Combustivel, Combustivel, StringComparison.OrdinalIgnoreCase));
        }

        if (PrecoMinimo.HasValue)
        {
            query = query.Where(veiculo => (ObterPrecoPrincipal(veiculo) ?? 0m) >= PrecoMinimo.Value);
        }

        if (PrecoMaximo.HasValue)
        {
            query = query.Where(veiculo => (ObterPrecoPrincipal(veiculo) ?? decimal.MaxValue) <= PrecoMaximo.Value);
        }

        if (AnoMinimo.HasValue)
        {
            query = query.Where(veiculo => (ObterAnoPrincipal(veiculo) ?? 0) >= AnoMinimo.Value);
        }

        Vehicles = query
            .OrderByDescending(veiculo => veiculo.Destaque)
            .ThenByDescending(veiculo => ObterAnoPrincipal(veiculo) ?? 0)
            .ThenByDescending(veiculo => veiculo.DataCadastro)
            .Select(CatalogVehicleItem.From)
            .ToList();

        var orderedVehicles = veiculos
            .OrderByDescending(veiculo => ObterAnoPrincipal(veiculo) ?? 0)
            .ThenByDescending(veiculo => veiculo.DataCadastro)
            .ToList();

        RecentVehicles = orderedVehicles
            .Take(3)
            .Select(CatalogVehicleItem.From)
            .ToList();

        var recentIds = RecentVehicles
            .Select(vehicle => vehicle.Id)
            .ToHashSet();

        RemainingVehicles = orderedVehicles
            .Where(veiculo => !recentIds.Contains(veiculo.Id))
            .Select(CatalogVehicleItem.From)
            .ToList();
    }

    private static bool Contem(string? value, string busca)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(busca, StringComparison.OrdinalIgnoreCase);
    }

    private static decimal? ObterPrecoPrincipal(Veiculo veiculo)
    {
        return veiculo.PrecoPromocional ?? veiculo.PrecoVenda ?? veiculo.PrecoFipe;
    }

    private static int? ObterAnoPrincipal(Veiculo veiculo)
    {
        return veiculo.AnoModelo ?? veiculo.AnoFabricacao;
    }

    public sealed class CatalogVehicleItem
    {
        public int Id { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Marca { get; init; } = string.Empty;
        public string Modelo { get; init; } = string.Empty;
        public string Versao { get; init; } = string.Empty;
        public string Cambio { get; init; } = "-";
        public string Combustivel { get; init; } = "-";
        public string Cor { get; init; } = string.Empty;
        public int? Ano { get; init; }
        public int? Quilometragem { get; init; }
        public decimal? Preco { get; init; }
        public decimal? PrecoDe { get; init; }
        public string Tag { get; init; } = "Disponivel";
        public string Highlight { get; init; } = string.Empty;
        public string ImageUrl { get; init; } = string.Empty;
        public string WhatsappUrl { get; init; } = string.Empty;

        public static CatalogVehicleItem From(Veiculo veiculo)
        {
            var titulo = string.IsNullOrWhiteSpace(veiculo.Titulo)
                ? string.Join(" ", new[] { veiculo.Marca?.Nome, veiculo.Modelo, veiculo.Versao }
                    .Where(value => !string.IsNullOrWhiteSpace(value)))
                : veiculo.Titulo;

            var precoPrincipal = ObterPrecoPrincipal(veiculo);
            var precoDe = veiculo.PrecoPromocional.HasValue &&
                          veiculo.PrecoVenda.HasValue &&
                          veiculo.PrecoPromocional.Value < veiculo.PrecoVenda.Value
                ? veiculo.PrecoVenda
                : null;

            return new CatalogVehicleItem
            {
                Id = veiculo.Id,
                Titulo = string.IsNullOrWhiteSpace(titulo) ? $"Veiculo #{veiculo.Id}" : titulo,
                Marca = veiculo.Marca?.Nome ?? "Sem marca",
                Modelo = veiculo.Modelo ?? string.Empty,
                Versao = veiculo.Versao ?? string.Empty,
                Cambio = string.IsNullOrWhiteSpace(veiculo.Cambio) ? "-" : veiculo.Cambio!,
                Combustivel = string.IsNullOrWhiteSpace(veiculo.Combustivel) ? "-" : veiculo.Combustivel!,
                Cor = veiculo.Cor ?? string.Empty,
                Ano = ObterAnoPrincipal(veiculo),
                Quilometragem = veiculo.Quilometragem,
                Preco = precoPrincipal,
                PrecoDe = precoDe,
                Tag = veiculo.Destaque ? "Destaque" : precoDe.HasValue ? "Promocao" : "Disponivel",
                Highlight = MontarHighlight(veiculo),
                ImageUrl = ObterImagem(veiculo),
                WhatsappUrl = $"https://wa.me/551632523490?text={Uri.EscapeDataString($"Ola, quero mais detalhes do {titulo}.")}"
            };
        }

        private static string MontarHighlight(Veiculo veiculo)
        {
            var partes = new[]
            {
                veiculo.Cor,
                veiculo.AnoModelo?.ToString() ?? veiculo.AnoFabricacao?.ToString()
            };

            return string.Join(" • ", partes.Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        private static string ObterImagem(Veiculo veiculo)
        {
            var midia = veiculo.Midias
                .Where(item => item.Ativo && !string.IsNullOrWhiteSpace(item.Url))
                .OrderByDescending(item => item.Capa)
                .ThenBy(item => item.Ordem)
                .FirstOrDefault();

            if (midia == null)
            {
                return "/img/carroDefault.png";
            }

            if (Uri.TryCreate(midia.Url, UriKind.Absolute, out _))
            {
                return midia.Url;
            }

            return midia.Url.StartsWith('/') ? midia.Url : $"/{midia.Url.TrimStart('/')}";
        }
    }
}
