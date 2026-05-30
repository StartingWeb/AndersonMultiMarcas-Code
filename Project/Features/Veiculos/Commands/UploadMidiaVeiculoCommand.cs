using MediatR;
using Project.Features.Veiculos.DTOs;
using Project.Shared;

namespace Project.Features.Veiculos.Commands;

public sealed record UploadMidiaVeiculoCommand(VeiculoMidiaUploadDto Dto, IReadOnlyCollection<IFormFile> Arquivos) : IRequest<Result>;
