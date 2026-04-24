using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Project.Pages;

public class CatalogoModel : PageModel
{
    private const int RecentVehiclesCount = 3;
    private readonly ApplicationDbContext _context;

    public CatalogoModel(ApplicationDbContext context)
    {
        _context = context;
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
    public bool? ZeroKm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Condicao { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? MotoEletrica { get; set; }

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
        !string.IsNullOrWhiteSpace(Condicao) ||
        PrecoMinimo.HasValue ||
        PrecoMaximo.HasValue ||
        ZeroKm.HasValue ||
        MotoEletrica == true;

    public async Task OnGetAsync()
    {
        ViewData["ShowHero"] = false;

        if (PrecoMinimo.HasValue && PrecoMaximo.HasValue && PrecoMinimo > PrecoMaximo)
        {
            (PrecoMinimo, PrecoMaximo) = (PrecoMaximo, PrecoMinimo);
        }

        var baseQuery = _context.Veiculos
            .AsNoTracking()
            .Where(veiculo => veiculo.Ativo && !veiculo.Vendido);

        var filterMetadata = await baseQuery
            .Select(veiculo => new VehicleFilterMetadataItem
            {
                Titulo = veiculo.Titulo,
                Marca = veiculo.Marca != null ? veiculo.Marca.Nome : null,
                Modelo = veiculo.Modelo,
                Versao = veiculo.Versao,
                Cambio = veiculo.Cambio,
                Combustivel = veiculo.Combustivel,
                MotoEletrica = veiculo.MotoEletrica,
                Ano = veiculo.AnoModelo ?? veiculo.AnoFabricacao
            })
            .ToListAsync();

        if (MotoEletrica == true)
        {
            filterMetadata = filterMetadata
                .Where(item => item.MotoEletrica)
                .ToList();
        }
        else
        {
            filterMetadata = filterMetadata
                .Where(item => !IsMotorcycle(item))
                .ToList();
        }

        var filteredQuery = AplicarFiltros(baseQuery);

        var vehicles = await ProjetarVeiculos(filteredQuery)
            .OrderByDescending(veiculo => veiculo.Destaque)
            .ThenByDescending(veiculo => veiculo.Ano)
            .ThenByDescending(veiculo => veiculo.DataCadastro)
            .ToListAsync();

        if (MotoEletrica == true)
        {
            vehicles = vehicles
                .Where(item => item.MotoEletrica)
                .ToList();
        }
        else
        {
            vehicles = vehicles
                .Where(item => !IsMotorcycle(item))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(Combustivel))
        {
            vehicles = vehicles
                .Where(item => CombustivelCompativel(item.Combustivel, Combustivel))
                .ToList();
        }

        AvailableBrands = filterMetadata
            .Select(item => item.Marca)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        AvailableGearboxes = filterMetadata
            .Select(item => item.Cambio)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        AvailableFuels = filterMetadata
            .Select(item => item.Combustivel)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .GroupBy(value => NormalizarCombustivelChave(value), StringComparer.OrdinalIgnoreCase)
            .Select(group => NormalizarCombustivelExibicao(group.Key))
            .OrderBy(value => value)
            .ToList();

        AvailableYears = filterMetadata
            .Select(item => item.Ano)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .OrderByDescending(value => value)
            .ToList();

        Vehicles = vehicles
            .Select(MapToCatalogVehicleItem)
            .ToList();

        if (!HasActiveFilters)
        {
            RecentVehicles = Vehicles
                .Take(RecentVehiclesCount)
                .ToList();

            var recentIds = RecentVehicles
                .Select(vehicle => vehicle.Id)
                .ToHashSet();

            RemainingVehicles = Vehicles
                .Where(veiculo => !recentIds.Contains(veiculo.Id))
                .ToList();
        }
    }

    private IQueryable<Domain.Veiculo> AplicarFiltros(IQueryable<Domain.Veiculo> query)
    {
        if (!string.IsNullOrWhiteSpace(Busca))
        {
            var termosBusca = Busca
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var termo in termosBusca)
            {
                var like = $"%{termo}%";

                query = query.Where(veiculo =>
                    EF.Functions.Like(veiculo.Titulo, like) ||
                    (veiculo.Modelo != null && EF.Functions.Like(veiculo.Modelo, like)) ||
                    (veiculo.Versao != null && EF.Functions.Like(veiculo.Versao, like)) ||
                    (veiculo.Combustivel != null && EF.Functions.Like(veiculo.Combustivel, like)) ||
                    (veiculo.Cambio != null && EF.Functions.Like(veiculo.Cambio, like)) ||
                    (veiculo.Cor != null && EF.Functions.Like(veiculo.Cor, like)) ||
                    (veiculo.Marca != null && EF.Functions.Like(veiculo.Marca.Nome, like)) ||
                    (veiculo.AnoModelo.HasValue && veiculo.AnoModelo.Value.ToString().Contains(termo)) ||
                    (veiculo.AnoFabricacao.HasValue && veiculo.AnoFabricacao.Value.ToString().Contains(termo)));
            }
        }

        if (!string.IsNullOrWhiteSpace(Marca))
        {
            var marcaSelecionada = Marca.Trim().ToUpper();
            query = query.Where(veiculo =>
                veiculo.Marca != null &&
                veiculo.Marca.Nome != null &&
                veiculo.Marca.Nome.Trim().ToUpper() == marcaSelecionada);
        }

        if (!string.IsNullOrWhiteSpace(Cambio))
        {
            query = query.Where(veiculo => veiculo.Cambio == Cambio);
        }
        if (!string.IsNullOrWhiteSpace(Condicao))
        {
            var condicao = Condicao.Trim().ToLowerInvariant();

            if (condicao == "seminovo")
            {
                query = query.Where(veiculo => veiculo.Seminovo);
            }
            else if (condicao == "zerokm" || condicao == "novo")
            {
                query = query.Where(veiculo => !veiculo.Seminovo);
            }
        }

        if (PrecoMinimo.HasValue)
        {
            query = query.Where(veiculo =>
                veiculo.PrecoVenda.HasValue &&
                veiculo.PrecoVenda.Value > 0m &&
                veiculo.PrecoVenda.Value >= PrecoMinimo.Value);
        }

        if (PrecoMaximo.HasValue)
        {
            query = query.Where(veiculo =>
                veiculo.PrecoVenda.HasValue &&
                veiculo.PrecoVenda.Value > 0m &&
                veiculo.PrecoVenda.Value <= PrecoMaximo.Value);
        }

        if (ZeroKm == true)
        {
            query = query.Where(veiculo => !veiculo.Seminovo);
        }

        if (MotoEletrica == true)
        {
            query = query.Where(veiculo => veiculo.MotoEletrica);
        }

        return query;
    }

    private static IQueryable<VehicleCardQueryItem> ProjetarVeiculos(IQueryable<Domain.Veiculo> query)
    {
        return query.Select(veiculo => new VehicleCardQueryItem
        {
            Id = veiculo.Id,
            Titulo = veiculo.Titulo,
            Marca = veiculo.Marca != null ? veiculo.Marca.Nome : null,
            Modelo = veiculo.Modelo,
            Versao = veiculo.Versao,
            Cambio = veiculo.Cambio,
            Combustivel = veiculo.Combustivel,
            Cor = veiculo.Cor,
            Ano = veiculo.AnoModelo ?? veiculo.AnoFabricacao,
            Seminovo = veiculo.Seminovo,
            MotoEletrica = veiculo.MotoEletrica,
            Quilometragem = veiculo.Quilometragem,
            PrecoVenda = veiculo.PrecoVenda,
            Destaque = veiculo.Destaque,
            DataCadastro = veiculo.DataCadastro,
            ImageUrl = veiculo.Midias
                .Where(item => item.Ativo && item.Url != null && item.Url != string.Empty)
                .OrderByDescending(item => item.Capa)
                .ThenBy(item => item.Ordem)
                .Select(item => item.Url)
                .FirstOrDefault()
        });
    }

    private static CatalogVehicleItem MapToCatalogVehicleItem(VehicleCardQueryItem item)
    {
        var titulo = !string.IsNullOrWhiteSpace(item.Marca) || !string.IsNullOrWhiteSpace(item.Modelo)
            ? string.Join(" ", new[] { item.Marca, item.Modelo }.Where(value => !string.IsNullOrWhiteSpace(value)))
            : string.IsNullOrWhiteSpace(item.Titulo)
                ? string.Join(" ", new[] { item.Marca, item.Versao }
                    .Where(value => !string.IsNullOrWhiteSpace(value)))
                : item.Titulo!;

        if (string.IsNullOrWhiteSpace(titulo))
        {
            titulo = $"Veiculo #{item.Id}";
        }

        var precoPrincipal = ObterPrecoPrincipal(item.PrecoVenda);
        var precoDe = null as decimal?;

        return new CatalogVehicleItem
        {
            Id = item.Id,
            Titulo = titulo,
            Marca = item.Marca ?? "Sem marca",
            Modelo = item.Modelo ?? string.Empty,
            Versao = item.Versao ?? string.Empty,
            Cambio = string.IsNullOrWhiteSpace(item.Cambio) ? "-" : item.Cambio,
            Combustivel = string.IsNullOrWhiteSpace(item.Combustivel) ? "-" : item.Combustivel,
            Cor = item.Cor ?? string.Empty,
            Ano = item.Ano,
            Seminovo = item.Seminovo,
            Quilometragem = item.Quilometragem,
            Preco = precoPrincipal,
            PrecoDe = precoDe,
            Tag = item.Destaque ? "Destaque" : precoDe.HasValue ? "Promocao" : "Disponivel",
            Highlight = string.Join(" • ", new[]
            {
                item.Cor,
                item.Ano?.ToString()
            }.Where(value => !string.IsNullOrWhiteSpace(value))),
            ImageUrl = NormalizarImagem(item.ImageUrl),
            WhatsappUrl = $"https://wa.me/5516996219214?text={Uri.EscapeDataString($"Ola, quero mais detalhes do {titulo}.")}"
        };
    }

    private static string NormalizarImagem(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return "/img/carroDefault.png";
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
        {
            return imageUrl;
        }

        return imageUrl.StartsWith('/') ? imageUrl : $"/{imageUrl.TrimStart('/')}";
    }

    private static decimal? ObterPrecoPrincipal(decimal? precoVenda)
    {
        if (precoVenda.HasValue && precoVenda.Value > 0m)
        {
            return precoVenda.Value;
        }

        return null;
    }

    private static bool IsMotorcycle(VehicleCardQueryItem item)
    {
        if (item.MotoEletrica)
        {
            return true;
        }

        var text = NormalizarTextoComparacao(string.Join(" ", new[] { item.Titulo, item.Marca, item.Modelo, item.Versao }
            .Where(value => !string.IsNullOrWhiteSpace(value))));

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] keywords =
        [
            "MOTO", "MOTOCICLETA", "MOTONETA", "SCOOTER", "CG", "BIZ", "POP", "FAN", "TITAN",
            "BROS", "BROZ", "XRE", "CB ", "CB-", "CBR", "HORNET", "PCX", "NMAX", "XMAX",
            "FAZER", "LANDER", "CROSSER", "TENERE", "MT-", "FZ", "TWISTER", "HARLEY",
            "DUCATI", "TRIUMPH", "ROYAL ENFIELD", "KAWASAKI", "KTM", "HAOJUE", "DAFRA", "BAJAJ"
        ];

        return keywords.Any(keyword => text.Contains(keyword, StringComparison.Ordinal));
    }

    private static bool IsMotorcycle(VehicleFilterMetadataItem item)
    {
        if (item.MotoEletrica)
        {
            return true;
        }

        var text = NormalizarTextoComparacao(string.Join(" ", new[] { item.Titulo, item.Marca, item.Modelo, item.Versao }
            .Where(value => !string.IsNullOrWhiteSpace(value))));

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] keywords =
        [
            "MOTO", "MOTOCICLETA", "MOTONETA", "SCOOTER", "CG", "BIZ", "POP", "FAN", "TITAN",
            "BROS", "BROZ", "XRE", "CB ", "CB-", "CBR", "HORNET", "PCX", "NMAX", "XMAX",
            "FAZER", "LANDER", "CROSSER", "TENERE", "MT-", "FZ", "TWISTER", "HARLEY",
            "DUCATI", "TRIUMPH", "ROYAL ENFIELD", "KAWASAKI", "KTM", "HAOJUE", "DAFRA", "BAJAJ"
        ];

        return keywords.Any(keyword => text.Contains(keyword, StringComparison.Ordinal));
    }

    private static string NormalizarTextoComparacao(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    private static bool CombustivelCompativel(string? valorVeiculo, string? filtroSelecionado)
    {
        var veiculo = NormalizarCombustivelChave(valorVeiculo ?? string.Empty);
        var filtro = NormalizarCombustivelChave(filtroSelecionado ?? string.Empty);
        if (string.IsNullOrWhiteSpace(filtro))
        {
            return true;
        }

        return veiculo == filtro;
    }

    private static string NormalizarCombustivelChave(string value)
    {
        var token = NormalizarTextoComparacao(value);
        return token switch
        {
            "ACOOL" => "ALCOOL",
            _ => token
        };
    }

    private static string NormalizarCombustivelExibicao(string normalizedValue)
    {
        return normalizedValue switch
        {
            "ALCOOL" => "Álcool",
            "DIESEL" => "Diesel",
            "ELETRICO" => "Elétrico",
            "FLEX" => "Flex",
            "GASOLINA" => "Gasolina",
            "HIBRIDO" => "Híbrido",
            "GNV" => "GNV",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalizedValue.ToLowerInvariant())
        };
    }


    private sealed class VehicleCardQueryItem
    {
        public int Id { get; init; }
        public string? Titulo { get; init; }
        public string? Marca { get; init; }
        public string? Modelo { get; init; }
        public string? Versao { get; init; }
        public string? Cambio { get; init; }
        public string? Combustivel { get; init; }
        public string? Cor { get; init; }
        public int? Ano { get; init; }
        public bool Seminovo { get; init; }
        public bool MotoEletrica { get; init; }
        public int? Quilometragem { get; init; }
        public decimal? PrecoVenda { get; init; }
        public bool Destaque { get; init; }
        public DateTime DataCadastro { get; init; }
        public string? ImageUrl { get; init; }
    }

    private sealed class VehicleFilterMetadataItem
    {
        public string? Titulo { get; init; }
        public string? Marca { get; init; }
        public string? Modelo { get; init; }
        public string? Versao { get; init; }
        public string? Cambio { get; init; }
        public string? Combustivel { get; init; }
        public bool MotoEletrica { get; init; }
        public int? Ano { get; init; }
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
        public bool Seminovo { get; init; } = true;
        public int? Quilometragem { get; init; }
        public decimal? Preco { get; init; }
        public decimal? PrecoDe { get; init; }
        public string Tag { get; init; } = "Disponivel";
        public string Highlight { get; init; } = string.Empty;
        public string ImageUrl { get; init; } = string.Empty;
        public string WhatsappUrl { get; init; } = string.Empty;
    }
}
