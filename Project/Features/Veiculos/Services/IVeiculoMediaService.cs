using Core.Storage;

namespace Project.Features.Veiculos.Services;

public interface IVeiculoMediaService
{
    Task<IReadOnlyCollection<VeiculoMediaUploadResult>> ProcessarUploadAsync(int veiculoId, IReadOnlyCollection<IFormFile> arquivos, CancellationToken ct);
    Task RemoverArquivoAsync(string? caminhoRelativo, CancellationToken ct);
    Task RemoverArquivoAsync(StorageImageReference reference, CancellationToken ct);
}

public sealed record VeiculoMediaUploadResult(
    string Url,
    string NomeArquivo,
    string BlobName,
    string Container,
    string ContentType,
    long TamanhoBytes);
