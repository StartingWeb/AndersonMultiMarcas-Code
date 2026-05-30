using MediatR;
using Project.Shared;

namespace Project.Features.Veiculos.Commands;

public sealed record MarcarVeiculoComoVendidoCommand(int VeiculoId, DateTime DataVenda) : IRequest<Result>;
