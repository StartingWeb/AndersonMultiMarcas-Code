using Core.Dtos;
using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Project.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ApplicationDbContext _context;
    private static readonly IReadOnlyList<HomePriceRangeItem> DefaultPriceRanges =
    [
        new("At\u00e9 R$ 60 mil", null, 60000m),
        new("De R$ 60 mil a R$ 90 mil", 60000m, 90000m),
        new("De R$ 90 mil a R$ 130 mil", 90000m, 130000m),
        new("Acima de R$ 130 mil", 130000m, null)
    ];

    public IndexModel(
        ILogger<IndexModel> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IReadOnlyList<CatalogoModel.CatalogVehicleItem> FeaturedVehicles { get; private set; } = [];
    public IReadOnlyList<CatalogoModel.CatalogVehicleItem> PremiumVehicles { get; private set; } = [];
    public IReadOnlyList<CatalogoModel.CatalogVehicleItem> HybridElectricCars { get; private set; } = [];
    public IReadOnlyList<CatalogoModel.CatalogVehicleItem> ElectricMotorcycles { get; private set; } = [];
    public IReadOnlyList<string> SellerPhotoPrefetchUrls { get; private set; } = [];
    public IReadOnlyList<HomeStoreItem> Stores { get; private set; } = [];
    public IReadOnlyList<string> AvailableBrands { get; private set; } = [];
    public IReadOnlyList<string> AvailableModels { get; private set; } = [];
    public IReadOnlyList<int> AvailableYears { get; private set; } = [];
    public IReadOnlyList<HomePriceRangeItem> PriceRanges => DefaultPriceRanges;
    public int TotalActiveVehicles { get; private set; }

    public async Task OnGetAsync()
    {
        var activeVehiclesQuery = _context.Veiculos
            .AsNoTracking()
            .Where(veiculo => veiculo.Ativo && !veiculo.Vendido);

        var activeVehicleMetadata = await activeVehiclesQuery
            .Select(veiculo => new ActiveVehicleMetadataItem
            {
                Marca = veiculo.Marca != null ? veiculo.Marca.Nome : null,
                Modelo = veiculo.Modelo,
                Ano = veiculo.AnoModelo ?? veiculo.AnoFabricacao
            })
            .ToListAsync();

        var featuredVehiclesBaseQuery = _context.Veiculos
            .AsNoTracking()
            .Where(veiculo => veiculo.Ativo && !veiculo.Vendido)
            .Select(veiculo => new VehicleCardQueryItem
            {
                Id = veiculo.Id,
                Titulo = veiculo.Titulo,
                Marca = veiculo.Marca != null ? veiculo.Marca.Nome : null,
                Modelo = veiculo.Modelo,
                Versao = veiculo.Versao,
                Cambio = veiculo.Cambio,
                Combustivel = veiculo.Combustivel,
                Cor = veiculo.Cor,
                AnoFabricacao = veiculo.AnoFabricacao,
                AnoModelo = veiculo.AnoModelo,
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

        var featuredVehicles = await featuredVehiclesBaseQuery
            .Where(veiculo => veiculo.Destaque)
            .OrderByDescending(veiculo => veiculo.DataCadastro)
            .Take(12)
            .ToListAsync();

        var premiumVehicles = await _context.Veiculos
            .AsNoTracking()
            .Where(veiculo => veiculo.Ativo && !veiculo.Vendido)
            .Where(veiculo => !veiculo.Seminovo)
            .Where(veiculo => veiculo.PrecoVenda.HasValue && veiculo.PrecoVenda.Value >= 130000m)
            .OrderByDescending(veiculo => veiculo.Destaque)
            .ThenByDescending(veiculo => veiculo.DataCadastro)
            .Take(4)
            .Select(veiculo => new VehicleCardQueryItem
            {
                Id = veiculo.Id,
                Titulo = veiculo.Titulo,
                Marca = veiculo.Marca != null ? veiculo.Marca.Nome : null,
                Modelo = veiculo.Modelo,
                Versao = veiculo.Versao,
                Cambio = veiculo.Cambio,
                Combustivel = veiculo.Combustivel,
                Cor = veiculo.Cor,
                AnoFabricacao = veiculo.AnoFabricacao,
                AnoModelo = veiculo.AnoModelo,
                Seminovo = veiculo.Seminovo,
                MotoEletrica = veiculo.MotoEletrica,
                Quilometragem = veiculo.Quilometragem,
                PrecoVenda = veiculo.PrecoVenda,
                Destaque = veiculo.Destaque,
                ImageUrl = veiculo.Midias
                    .Where(item => item.Ativo && item.Url != null && item.Url != string.Empty)
                    .OrderByDescending(item => item.Capa)
                    .ThenBy(item => item.Ordem)
                    .Select(item => item.Url)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var electricCandidates = await _context.Veiculos
            .AsNoTracking()
            .Where(veiculo => veiculo.Ativo && !veiculo.Vendido)
            .OrderByDescending(veiculo => veiculo.Destaque)
            .ThenByDescending(veiculo => veiculo.DataCadastro)
            .Select(veiculo => new VehicleCardQueryItem
            {
                Id = veiculo.Id,
                Titulo = veiculo.Titulo,
                Marca = veiculo.Marca != null ? veiculo.Marca.Nome : null,
                Modelo = veiculo.Modelo,
                Versao = veiculo.Versao,
                Cambio = veiculo.Cambio,
                Combustivel = veiculo.Combustivel,
                Cor = veiculo.Cor,
                AnoFabricacao = veiculo.AnoFabricacao,
                AnoModelo = veiculo.AnoModelo,
                Seminovo = veiculo.Seminovo,
                MotoEletrica = veiculo.MotoEletrica,
                Quilometragem = veiculo.Quilometragem,
                PrecoVenda = veiculo.PrecoVenda,
                Destaque = veiculo.Destaque,
                ImageUrl = veiculo.Midias
                    .Where(item => item.Ativo && item.Url != null && item.Url != string.Empty)
                    .OrderByDescending(item => item.Capa)
                    .ThenBy(item => item.Ordem)
                    .Select(item => item.Url)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var electricVehicles = electricCandidates
            .Where(item => item.MotoEletrica)
            .OrderByDescending(item => item.Destaque)
            .Take(4)
            .ToList();

        var hybridElectricCars = electricCandidates
            .Where(item => !IsMotorcycle(item))
            .Where(IsHybridOrElectricVehicle)
            .OrderByDescending(item => item.Destaque)
            .ThenByDescending(item => item.DataCadastro)
            .Take(4)
            .ToList();

        var stores = await _context.Lojas
            .AsNoTracking()
            .Where(loja => loja.Ativo)
            .OrderBy(loja => loja.Nome)
            .Select(loja => new LojaDto
            {
                Id = loja.Id,
                Nome = loja.Nome,
                Endereco = loja.Endereco,
                Numero = loja.Numero,
                Bairro = loja.Bairro,
                Cidade = loja.Cidade,
                Uf = loja.Uf,
                Cep = loja.Cep,
                Ativo = loja.Ativo,
                DataCadastro = loja.DataCadastro
            })
            .ToListAsync();

        var sellerPhotoUrls = await _context.Vendedores
            .AsNoTracking()
            .Where(vendedor => vendedor.Ativo && vendedor.FotoUrl != null && vendedor.FotoUrl != string.Empty)
            .OrderBy(vendedor => vendedor.Nome)
            .Select(vendedor => vendedor.FotoUrl!)
            .Take(24)
            .ToListAsync();

        FeaturedVehicles = featuredVehicles
            .Select(MapToCatalogVehicleItem)
            .ToList();

        PremiumVehicles = premiumVehicles
            .Select(MapToCatalogVehicleItem)
            .ToList();

        HybridElectricCars = hybridElectricCars
            .Select(MapToCatalogVehicleItem)
            .ToList();

        ElectricMotorcycles = electricVehicles
            .Select(MapToCatalogVehicleItem)
            .ToList();

        AvailableBrands = activeVehicleMetadata
            .Select(item => item.Marca)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        AvailableModels = activeVehicleMetadata
            .Select(item => item.Modelo)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .Take(80)
            .ToList();

        AvailableYears = activeVehicleMetadata
            .Select(item => item.Ano)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .OrderByDescending(value => value)
            .Take(12)
            .ToList();

        TotalActiveVehicles = activeVehicleMetadata.Count;

        Stores = stores
            .Select(HomeStoreItem.From)
            .ToList();

        SellerPhotoPrefetchUrls = sellerPhotoUrls
            .Select(NormalizarImagem)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IActionResult> OnGetSearchSuggestionsAsync(string? term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
        {
            return new JsonResult(Array.Empty<SearchSuggestionItem>());
        }

        var termo = term.Trim();
        var like = $"%{termo}%";

        var candidatos = await _context.Veiculos
            .AsNoTracking()
            .Where(veiculo => veiculo.Ativo && !veiculo.Vendido)
            .Where(veiculo =>
                EF.Functions.Like(veiculo.Titulo, like) ||
                (veiculo.Modelo != null && EF.Functions.Like(veiculo.Modelo, like)) ||
                (veiculo.Versao != null && EF.Functions.Like(veiculo.Versao, like)) ||
                (veiculo.Combustivel != null && EF.Functions.Like(veiculo.Combustivel, like)) ||
                (veiculo.Cambio != null && EF.Functions.Like(veiculo.Cambio, like)) ||
                (veiculo.Marca != null && EF.Functions.Like(veiculo.Marca.Nome, like)))
            .OrderByDescending(veiculo => veiculo.Destaque)
            .ThenByDescending(veiculo => veiculo.DataCadastro)
            .Take(24)
            .Select(veiculo => new SearchSuggestionQueryItem
            {
                Titulo = veiculo.Titulo,
                Marca = veiculo.Marca != null ? veiculo.Marca.Nome : null,
                Modelo = veiculo.Modelo,
                Versao = veiculo.Versao,
                Cambio = veiculo.Cambio,
                Combustivel = veiculo.Combustivel,
                AnoFabricacao = veiculo.AnoFabricacao,
                AnoModelo = veiculo.AnoModelo
            })
            .ToListAsync();

        var sugestoes = new List<SearchSuggestionItem>();

        var nomes = candidatos
            .Select(item => new
            {
                Nome = MontarNomePesquisa(item.Titulo, item.Marca, item.Modelo, item.Versao),
                item.Marca,
                Ano = item.AnoModelo ?? item.AnoFabricacao
            })
            .Where(item => item.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase))
            .GroupBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(6)
            .Select(item => new SearchSuggestionItem
            {
                Group = "Nome",
                Label = item.Nome,
                Meta = string.Join(" - ", new[]
                {
                    item.Marca,
                    item.Ano?.ToString()
                }.Where(value => !string.IsNullOrWhiteSpace(value))),
                Query = item.Nome,
                Url = $"/Catalogo?busca={Uri.EscapeDataString(item.Nome)}"
            });

        sugestoes.AddRange(nomes);

        var marcas = candidatos
            .Select(item => item.Marca)
            .Where(marca => !string.IsNullOrWhiteSpace(marca) &&
                            marca.Contains(termo, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .Select(marca => new SearchSuggestionItem
            {
                Group = "Marca",
                Label = marca!,
                Meta = "Filtrar ve\u00edculos por marca",
                Query = marca!,
                Url = $"/Catalogo?marca={Uri.EscapeDataString(marca!)}"
            });

        sugestoes.AddRange(marcas);

        var categorias = candidatos
            .SelectMany(item => new[]
            {
                CriarSugestaoCategoria("Combust\u00edvel", item.Combustivel, termo, "combustivel"),
                CriarSugestaoCategoria("C\u00e2mbio", item.Cambio, termo, "cambio")
            })
            .Where(item => item != null)
            .Cast<SearchSuggestionItem>()
            .GroupBy(item => $"{item.Meta}|{item.Label}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(6);

        sugestoes.AddRange(categorias);

        var ordenadas = sugestoes
            .OrderBy(item => item.Group == "Categoria" ? 0 : item.Group == "Marca" ? 1 : 2)
            .ThenBy(item => item.Label)
            .Take(12)
            .ToList();

        return new JsonResult(ordenadas);
    }

    private static CatalogoModel.CatalogVehicleItem MapToCatalogVehicleItem(VehicleCardQueryItem item)
    {
        var titulo = MontarNomePesquisa(item.Titulo, item.Marca, item.Modelo, item.Versao);
        var precoPrincipal = ObterPrecoPrincipal(item.PrecoVenda);
        var precoDe = null as decimal?;

        return new CatalogoModel.CatalogVehicleItem
        {
            Id = item.Id,
            Titulo = titulo,
            Marca = item.Marca ?? "Sem marca",
            Modelo = item.Modelo ?? string.Empty,
            Versao = item.Versao ?? string.Empty,
            Cambio = string.IsNullOrWhiteSpace(item.Cambio) ? "-" : item.Cambio,
            Combustivel = string.IsNullOrWhiteSpace(item.Combustivel) ? "-" : item.Combustivel,
            Cor = item.Cor ?? string.Empty,
            Ano = item.AnoModelo ?? item.AnoFabricacao,
            Seminovo = item.Seminovo,
            Quilometragem = item.Quilometragem,
            Preco = precoPrincipal,
            PrecoDe = precoDe,
            Tag = item.Destaque ? "Destaque" : precoDe.HasValue ? "Promo\u00e7\u00e3o" : "Dispon\u00edvel",
            Highlight = string.Join(" - ", new[]
            {
                item.Cor,
                (item.AnoModelo ?? item.AnoFabricacao)?.ToString()
            }.Where(value => !string.IsNullOrWhiteSpace(value))),
            ImageUrl = NormalizarImagem(item.ImageUrl),
            WhatsappUrl = $"https://wa.me/5516996219214?text={Uri.EscapeDataString($"Ol\u00e1, quero mais detalhes do {titulo}.")}"
        };
    }

    private static SearchSuggestionItem? CriarSugestaoCategoria(
        string meta,
        string? valor,
        string termo,
        string parametro)
    {
        if (string.IsNullOrWhiteSpace(valor) ||
            !valor.Contains(termo, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new SearchSuggestionItem
        {
            Group = "Categoria",
            Label = valor,
            Meta = meta,
            Query = valor,
            Url = $"/Catalogo?{parametro}={Uri.EscapeDataString(valor)}"
        };
    }

    private static string MontarNomePesquisa(string? titulo, string? marca, string? modelo, string? versao)
    {
        var marcaModelo = string.Join(" ", new[] { marca, modelo }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (!string.IsNullOrWhiteSpace(marcaModelo))
        {
            return marcaModelo;
        }

        if (!string.IsNullOrWhiteSpace(modelo))
        {
            return modelo.Trim();
        }

        if (!string.IsNullOrWhiteSpace(titulo))
        {
            return titulo.Trim();
        }

        var nome = string.Join(" ", new[] { marca, versao }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(nome) ? "Ve\u00edculo" : nome;
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
    private static bool IsElectricVehicle(VehicleCardQueryItem item)
    {
        if (item.MotoEletrica)
        {
            return true;
        }
        var text = NormalizarTextoComparacao(string.Join(" ", new[] { item.Titulo, item.Marca, item.Modelo, item.Versao }
            .Where(value => !string.IsNullOrWhiteSpace(value))));
        var fuel = NormalizarTextoComparacao(item.Combustivel?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(fuel))
        {
            return false;
        }
        if (fuel.Equals("ELETRICO", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        string[] keywords =
        [
            "ELETRICO", "EV", "E-TRON", "ETRON", "LEAF", "BYD", "DOLPHIN",
            "ORA", "KONA ELECTRIC", "I3", "I4", "IX", "EQS", "EQE", "TAYCAN"
        ];
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.Ordinal));
    }
    private static bool IsHybridVehicle(VehicleCardQueryItem item)
    {
        var text = NormalizarTextoComparacao(string.Join(" ", new[] { item.Titulo, item.Marca, item.Modelo, item.Versao }
            .Where(value => !string.IsNullOrWhiteSpace(value))));
        var fuel = NormalizarTextoComparacao(item.Combustivel?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(fuel))
        {
            return false;
        }
        if (fuel.Contains("HIBRIDO", StringComparison.OrdinalIgnoreCase) ||
            fuel.Equals("HEV", StringComparison.OrdinalIgnoreCase) ||
            fuel.Equals("PHEV", StringComparison.OrdinalIgnoreCase) ||
            fuel.Equals("MHEV", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        string[] keywords =
        [
            "HIBRIDO", "HEV", "PHEV", "MHEV", "HYBRID", "E-POWER"
        ];
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.Ordinal));
    }
    private static bool IsHybridOrElectricVehicle(VehicleCardQueryItem item)
    {
        var fuel = NormalizarTextoComparacao(item.Combustivel?.Trim() ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(fuel))
        {
            string[] combustionFuels =
            [
                "GASOLINA", "ETANOL", "DIESEL", "FLEX", "GNV"
            ];

            if (combustionFuels.Any(keyword => fuel.Contains(keyword, StringComparison.Ordinal)))
            {
                return false;
            }
        }

        return IsElectricVehicle(item) || IsHybridVehicle(item);
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
    private static decimal? ObterPrecoPrincipal(decimal? precoVenda)
    {
        if (precoVenda.HasValue && precoVenda.Value > 0m)
        {
            return precoVenda.Value;
        }

        return null;
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
        public int? AnoFabricacao { get; init; }
        public int? AnoModelo { get; init; }
        public bool Seminovo { get; init; }
        public bool MotoEletrica { get; init; }
        public int? Quilometragem { get; init; }
        public decimal? PrecoVenda { get; init; }
        public bool Destaque { get; init; }
        public DateTime DataCadastro { get; init; }
        public string? ImageUrl { get; init; }
    }

    private sealed class SearchSuggestionQueryItem
    {
        public string? Titulo { get; init; }
        public string? Marca { get; init; }
        public string? Modelo { get; init; }
        public string? Versao { get; init; }
        public string? Cambio { get; init; }
        public string? Combustivel { get; init; }
        public int? AnoFabricacao { get; init; }
        public int? AnoModelo { get; init; }
    }

    private sealed class ActiveVehicleMetadataItem
    {
        public string? Marca { get; init; }
        public string? Modelo { get; init; }
        public int? Ano { get; init; }
    }

    public sealed class HomeStoreItem
    {
        public string Nome { get; init; } = string.Empty;
        public string EnderecoCompleto { get; init; } = string.Empty;
        public string MapsEmbedUrl { get; init; } = string.Empty;
        public string MapsLinkUrl { get; init; } = string.Empty;

        public static HomeStoreItem From(LojaDto loja)
        {
            var endereco = string.Join(", ", new[]
            {
                MontarLogradouro(loja),
                loja.Bairro,
                MontarCidadeUf(loja),
                loja.Cep
            }.Where(item => !string.IsNullOrWhiteSpace(item)));

            var query = Uri.EscapeDataString(string.IsNullOrWhiteSpace(endereco) ? loja.Nome : endereco);

            return new HomeStoreItem
            {
                Nome = loja.Nome,
                EnderecoCompleto = string.IsNullOrWhiteSpace(endereco) ? "Endere\u00e7o n\u00e3o informado." : endereco,
                MapsEmbedUrl = $"https://www.google.com/maps?q={query}&output=embed",
                MapsLinkUrl = $"https://www.google.com/maps/search/?api=1&query={query}"
            };
        }

        private static string? MontarLogradouro(LojaDto loja)
        {
            return string.Join(", ", new[] { loja.Endereco, loja.Numero }
                .Where(item => !string.IsNullOrWhiteSpace(item)));
        }

        private static string? MontarCidadeUf(LojaDto loja)
        {
            return string.Join(" - ", new[] { loja.Cidade, loja.Uf }
                .Where(item => !string.IsNullOrWhiteSpace(item)));
        }
    }

    public sealed class SearchSuggestionItem
    {
        public string Group { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string Meta { get; init; } = string.Empty;
        public string Query { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
    }

    public sealed class HomePriceRangeItem
    {
        public HomePriceRangeItem(string label, decimal? minValue, decimal? maxValue)
        {
            Label = label;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public string Label { get; }
        public decimal? MinValue { get; }
        public decimal? MaxValue { get; }
        public string Value =>
            $"{MinValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty}|{MaxValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty}";
    }
}

