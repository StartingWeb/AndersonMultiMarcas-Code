using Data;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Project.Pages.ViewModels;
using Project.Shared;

namespace Project.Pages;

public class IndexModel(ApplicationDbContext db, ILogger<IndexModel> logger, IWebHostEnvironment environment) : PageModel
{
    public IReadOnlyCollection<HomeVehicleCardViewModel> HeroDestaques { get; private set; } = [];
    public IReadOnlyCollection<HomeVehicleCardViewModel> PremiumZeroKm { get; private set; } = [];
    public IReadOnlyCollection<HomeVehicleCardViewModel> Eletrificados { get; private set; } = [];
    public IReadOnlyCollection<HomeVehicleCardViewModel> MotosEletricas { get; private set; } = [];
    public IReadOnlyCollection<HomeStoreViewModel> Lojas { get; private set; } = [];
    public IReadOnlyCollection<HomeSellerViewModel> Vendedores { get; private set; } = [];

    public async Task OnGetAsync()
    {
        _ = logger;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var query = db.Veiculos
            .AsNoTracking()
            .Where(x => x.Ativo)
            .Select(x => new VehicleCardProjection
            {
                Id = x.Id,
                Titulo = x.Versao == null || x.Versao == string.Empty
                    ? x.Titulo
                    : x.Titulo + " " + x.Versao,
                Cor = x.Cor,
                AnoFabricacao = x.AnoFabricacao,
                AnoModelo = x.AnoModelo,
                Cambio = x.Cambio.ToString(),
                Combustivel = x.Combustivel.ToString(),
                Destaque = x.Destaque,
                Disponivel = !x.Vendido,
                Preco = x.PrecoVenda.Valor,
                Midias = x.Midias
                    .Where(m => m.Ativo && m.Tipo == TipoMidia.Imagem)
                    .OrderByDescending(m => m.Capa)
                    .ThenBy(m => m.Ordem)
                    .ThenBy(m => m.Id)
                    .Select(m => m.Url)
                    .ToList()
            });

        HeroDestaques = (await query.Where(x => x.Destaque).OrderByDescending(x => x.Id).Take(12).ToListAsync())
            .Select(ToHomeVehicleCard)
            .ToList();
        PremiumZeroKm = (await query.Where(x => x.AnoModelo >= DateTime.UtcNow.Year).OrderByDescending(x => x.Id).Take(4).ToListAsync())
            .Select(ToHomeVehicleCard)
            .ToList();
        Eletrificados = (await db.Veiculos.AsNoTracking()
            .Where(x => x.Ativo && (x.Combustivel == Combustivel.Eletrico || x.Combustivel == Combustivel.Hibrido))
            .Select(x => new VehicleCardProjection
            {
                Id = x.Id,
                Titulo = x.Versao == null || x.Versao == string.Empty
                    ? x.Titulo
                    : x.Titulo + " " + x.Versao,
                Cor = x.Cor,
                AnoFabricacao = x.AnoFabricacao,
                AnoModelo = x.AnoModelo,
                Cambio = x.Cambio.ToString(),
                Combustivel = x.Combustivel.ToString(),
                Destaque = x.Destaque,
                Disponivel = !x.Vendido,
                Preco = x.PrecoVenda.Valor,
                Midias = x.Midias
                    .Where(m => m.Ativo && m.Tipo == TipoMidia.Imagem)
                    .OrderByDescending(m => m.Capa)
                    .ThenBy(m => m.Ordem)
                    .ThenBy(m => m.Id)
                    .Select(m => m.Url)
                    .ToList()
            })
            .OrderByDescending(x => x.Id)
            .Take(4)
            .ToListAsync())
            .Select(ToHomeVehicleCard)
            .ToList();
        MotosEletricas = (await db.Veiculos.AsNoTracking()
            .Where(x => x.Ativo && x.MotoEletrica)
            .Select(x => new VehicleCardProjection
            {
                Id = x.Id,
                Titulo = x.Versao == null || x.Versao == string.Empty
                    ? x.Titulo
                    : x.Titulo + " " + x.Versao,
                Cor = x.Cor,
                AnoFabricacao = x.AnoFabricacao,
                AnoModelo = x.AnoModelo,
                Cambio = x.Cambio.ToString(),
                Combustivel = x.Combustivel.ToString(),
                Destaque = x.Destaque,
                Disponivel = !x.Vendido,
                Preco = x.PrecoVenda.Valor,
                Midias = x.Midias
                    .Where(m => m.Ativo && m.Tipo == TipoMidia.Imagem)
                    .OrderByDescending(m => m.Capa)
                    .ThenBy(m => m.Ordem)
                    .ThenBy(m => m.Id)
                    .Select(m => m.Url)
                    .ToList()
            })
            .OrderByDescending(x => x.Id)
            .Take(4)
            .ToListAsync())
            .Select(ToHomeVehicleCard)
            .ToList();

        Lojas = await db.Lojas.AsNoTracking().OrderBy(x => x.Id).Select(x => new HomeStoreViewModel
        {
            Nome = x.Nome,
            EnderecoCompleto = $"{x.Endereco.Logradouro}, {x.Endereco.Numero}, {x.Endereco.Bairro}, {x.Endereco.Cidade} - {x.Endereco.Uf}, {x.Endereco.Cep}",
            MapsQuery = Uri.EscapeDataString($"{x.Endereco.Logradouro}, {x.Endereco.Numero}, {x.Endereco.Bairro}, {x.Endereco.Cidade} - {x.Endereco.Uf}, {x.Endereco.Cep}")
        }).Take(3).ToListAsync();

        Vendedores = await db.Vendedores.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome).Select(x => new HomeSellerViewModel
        {
            Nome = x.Nome,
            Telefone = x.Whatsapp.HasValue ? x.Whatsapp.Value.Valor : (x.Telefone.HasValue ? x.Telefone.Value.Valor : string.Empty),
            FotoUrl = SellerImageHelper.Normalize(x.FotoUrl)
        }).Take(12).ToListAsync();

        ViewData["SeoTitle"] = "Carros seminovos e 0 km em Taquaritinga/SP | Anderson Multimarcas";
        ViewData["MetaDescription"] = "Compre carros seminovos, 0 km, hibridos, eletricos e motos eletricas em Taquaritinga/SP com atendimento consultivo, troca e financiamento.";
        ViewData["CanonicalUrl"] = $"{baseUrl}/";
        ViewData["BreadcrumbSchema"] = SeoJsonLd.Breadcrumb(baseUrl, ("Inicio", "/"));
        ViewData["FaqSchema"] = SeoJsonLd.Faq(
            ("Quais veiculos encontro no estoque?", "Voce encontra veiculos seminovos, 0 km, hibridos, eletricos e motos eletricas."),
            ("Posso trocar meu carro?", "Sim. Fale com um vendedor para avaliacao e proposta de troca."),
            ("A loja atende em Taquaritinga/SP?", "Sim. A Anderson Multimarcas atende Taquaritinga e regiao com lojas e vendedores locais."));
    }

    public async Task<IActionResult> OnGetSearchSuggestionsAsync([FromQuery] string? term, CancellationToken ct)
    {
        var query = (term ?? string.Empty).Trim();
        if (query.Length < 2)
        {
            return new JsonResult(new { groups = Array.Empty<object>() });
        }

        var marcas = await db.Veiculos
            .AsNoTracking()
            .Where(x => x.Ativo && EF.Functions.Like(x.Marca.Nome, $"%{query}%"))
            .Select(x => x.Marca.Nome)
            .Distinct()
            .OrderBy(x => x)
            .Take(5)
            .ToListAsync(ct);

        var modelos = await db.Veiculos
            .AsNoTracking()
            .Where(x => x.Ativo && (EF.Functions.Like(x.Titulo, $"%{query}%") || EF.Functions.Like(x.Modelo, $"%{query}%")))
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                Label = (x.Titulo ?? string.Empty) + " " + (x.Modelo ?? string.Empty),
                MarcaNome = x.Marca.Nome,
                x.AnoModelo
            })
            .Take(7)
            .ToListAsync(ct);

        var groups = new List<object>();

        if (marcas.Count > 0)
        {
            groups.Add(new
            {
                title = "Marca",
                items = marcas.Select(m => new
                {
                    label = m,
                    meta = "Filtrar veículos por marca",
                    url = $"/veiculos?marca={Uri.EscapeDataString(m)}"
                })
            });
        }

        if (modelos.Count > 0)
        {
            groups.Add(new
            {
                title = "Nome",
                items = modelos.Select(m => new
                {
                    label = m.Label.Trim(),
                    meta = string.IsNullOrWhiteSpace(m.MarcaNome) ? $"Ano {m.AnoModelo}" : $"{m.MarcaNome} - {m.AnoModelo}",
                    url = $"/veiculos?busca={Uri.EscapeDataString(m.Label.Trim())}"
                })
            });
        }

        return new JsonResult(new { groups });
    }

    private HomeVehicleCardViewModel ToHomeVehicleCard(VehicleCardProjection vehicle)
        => new()
        {
            Id = vehicle.Id,
            Titulo = vehicle.Titulo,
            Cor = vehicle.Cor,
            AnoFabricacao = vehicle.AnoFabricacao,
            AnoModelo = vehicle.AnoModelo,
            Cambio = vehicle.Cambio,
            Combustivel = vehicle.Combustivel,
            Destaque = vehicle.Destaque,
            Disponivel = vehicle.Disponivel,
            Preco = vehicle.Preco,
            MidiaUrl = VehicleImageHelper.SelectCover(vehicle.Midias, environment.WebRootPath)
        };

    private sealed class VehicleCardProjection
    {
        public int Id { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string? Cor { get; init; }
        public int? AnoFabricacao { get; init; }
        public int AnoModelo { get; init; }
        public string Cambio { get; init; } = string.Empty;
        public string Combustivel { get; init; } = string.Empty;
        public bool Destaque { get; init; }
        public bool Disponivel { get; init; }
        public decimal Preco { get; init; }
        public IReadOnlyList<string?> Midias { get; init; } = [];
    }
}
