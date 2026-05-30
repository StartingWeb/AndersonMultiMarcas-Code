using MediatR;
using Project.Shared;

namespace Project.Features.Veiculos.Queries.Facets;

public sealed record ObterCatalogoFacetasQuery : IRequest<Result<CatalogoFacetasDto>>;

public sealed class CatalogoFacetasDto
{
    public IReadOnlyCollection<string> Marcas { get; init; } = [];
    public IReadOnlyCollection<string> Modelos { get; init; } = [];
    public IReadOnlyCollection<int> Anos { get; init; } = [];
}
