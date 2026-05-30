using MediatR;
using Project.Features.Veiculos.DTOs;
using Project.Shared;

namespace Project.Features.Veiculos.Queries;

public sealed record BuscarVeiculosQuery(BuscarVeiculosFiltroDto Filtro) : IRequest<Result<PagedResult<VeiculoListItemDto>>>;
