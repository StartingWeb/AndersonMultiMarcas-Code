using Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Features.Veiculos.Commands;
using Project.Shared;

namespace Project.Features.Veiculos.Handlers;

public sealed class MarcarVeiculoComoVendidoCommandHandler(ApplicationDbContext db) : IRequestHandler<MarcarVeiculoComoVendidoCommand, Result>
{
    public async Task<Result> Handle(MarcarVeiculoComoVendidoCommand request, CancellationToken cancellationToken)
    {
        var veiculo = await db.Veiculos.FirstOrDefaultAsync(x => x.Id == request.VeiculoId, cancellationToken);
        if (veiculo is null) return Result.Failure("Veiculo nao encontrado.");

        veiculo.MarcarComoVendido(request.DataVenda);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
