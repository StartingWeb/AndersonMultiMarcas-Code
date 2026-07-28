using Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Features.Veiculos.Queries.Facets;
using Project.Shared;

namespace Project.Features.Veiculos.Handlers.Facets;

public sealed class ObterCatalogoFacetasQueryHandler(ApplicationDbContext db)
    : IRequestHandler<ObterCatalogoFacetasQuery, Result<CatalogoFacetasDto>>
{
    public async Task<Result<CatalogoFacetasDto>> Handle(ObterCatalogoFacetasQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = db.Veiculos.AsNoTracking().Include(x => x.Marca).Where(x => x.Ativo && !x.Vendido);

        var marcas = await baseQuery.Select(x => x.Marca.Nome).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var modelos = await baseQuery.Select(x => x.Modelo).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var anos = await baseQuery.Select(x => x.AnoModelo).Distinct().OrderByDescending(x => x).ToListAsync(cancellationToken);

        return Result<CatalogoFacetasDto>.Success(new CatalogoFacetasDto
        {
            Marcas = marcas,
            Modelos = modelos,
            Anos = anos
        });
    }
}
