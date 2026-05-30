using MediatR;
using Project.Features.Veiculos.DTOs;
using Project.Shared;

namespace Project.Features.Veiculos.Commands;

public sealed record AtualizarVeiculoCommand(VeiculoUpdateDto Dto) : IRequest<Result>;
