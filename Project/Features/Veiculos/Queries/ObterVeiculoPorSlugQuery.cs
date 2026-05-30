using MediatR;
using Project.Features.Veiculos.DTOs;
using Project.Shared;

namespace Project.Features.Veiculos.Queries;

public sealed record ObterVeiculoPorSlugQuery(string Slug, string BaseUrl) : IRequest<Result<VeiculoDetalheDto>>;
