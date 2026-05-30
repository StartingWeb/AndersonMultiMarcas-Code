using MediatR;
using Project.Features.Veiculos.DTOs;
using Project.Shared;

namespace Project.Features.Veiculos.Commands;

public sealed record CriarVeiculoCommand(VeiculoCreateDto Dto) : IRequest<Result<int>>;
