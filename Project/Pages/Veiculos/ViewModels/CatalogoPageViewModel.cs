using Project.Features.Veiculos.DTOs;
using Project.Pages.ViewModels;

namespace Project.Pages.Veiculos.ViewModels;

public sealed class CatalogoPageViewModel
{
    public required BuscarVeiculosFiltroDto Filtro { get; init; }
    public string? CondicaoSelecionada { get; init; }
    public required IReadOnlyCollection<HomeSellerViewModel> Vendedores { get; init; }
    public required IReadOnlyCollection<VeiculoListItemDto> DestaquesRecentes { get; init; }
    public required IReadOnlyCollection<VeiculoListItemDto> OutrosVeiculos { get; init; }
    public required IReadOnlyCollection<string> Marcas { get; init; }
    public required IReadOnlyCollection<string> Modelos { get; init; }
    public required IReadOnlyCollection<int> Anos { get; init; }
    public int TotalItems { get; init; }
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public string HeaderKicker { get; init; } = "Estoque multimarcas";
    public string HeaderTitle { get; init; } = "Encontre seu proximo veiculo";
    public string HeaderSubtitle { get; init; } = "Pesquise por modelo, marca ou oportunidade e refine o restante nos filtros ao lado.";
    public SeoLandingContentViewModel? SeoLanding { get; init; }
}

public sealed class SeoLandingContentViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string IntroTitle { get; init; }
    public required IReadOnlyList<string> Paragraphs { get; init; }
    public required IReadOnlyList<SeoLandingLinkViewModel> Links { get; init; }
    public required IReadOnlyList<SeoLandingFaqViewModel> Faqs { get; init; }
}

public sealed record SeoLandingLinkViewModel(string Label, string Url);

public sealed record SeoLandingFaqViewModel(string Question, string Answer);
