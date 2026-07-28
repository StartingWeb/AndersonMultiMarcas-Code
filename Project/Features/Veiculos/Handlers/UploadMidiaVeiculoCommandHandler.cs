using Data;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Features.Veiculos.Commands;
using Project.Features.Veiculos.Services;
using Project.Shared;

namespace Project.Features.Veiculos.Handlers;

public sealed class UploadMidiaVeiculoCommandHandler(ApplicationDbContext db, IVeiculoMediaService mediaService) : IRequestHandler<UploadMidiaVeiculoCommand, Result>
{
    public async Task<Result> Handle(UploadMidiaVeiculoCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var veiculo = await db.Veiculos.Include(x => x.Midias).FirstOrDefaultAsync(x => x.Id == dto.VeiculoId, cancellationToken);
        if (veiculo is null) return Result.Failure("Veiculo nao encontrado.");

        var processados = await mediaService.ProcessarUploadAsync(dto.VeiculoId, request.Arquivos, cancellationToken);
        var ordemBase = veiculo.Midias.Count == 0 ? 0 : veiculo.Midias.Max(x => x.Ordem) + 1;

        var index = 0;
        foreach (var item in processados)
        {
            var midia = new VeiculoMidia(dto.VeiculoId, item.NomeArquivo, item.Url, TipoMidia.Imagem, ordemBase + index);
            midia.UpdateStorage(item.BlobName, item.Container, item.ContentType, item.TamanhoBytes);
            db.VeiculoMidias.Add(midia);
            index++;
        }

        await db.SaveChangesAsync(cancellationToken);

        if (dto.DefinirPrimeiraComoCapa)
        {
            var midias = await db.VeiculoMidias.Where(x => x.VeiculoId == dto.VeiculoId).OrderBy(x => x.Ordem).ToListAsync(cancellationToken);
            foreach (var item in midias)
            {
                if (item == midias.First()) item.DefinirComoCapa();
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
