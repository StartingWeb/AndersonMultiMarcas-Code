namespace Project.Features.Veiculos.Services;

public interface IVeiculoMediaService
{
    Task<IReadOnlyCollection<(string Url, string NomeArquivo, long TamanhoBytes)>> ProcessarUploadAsync(int veiculoId, IReadOnlyCollection<IFormFile> arquivos, CancellationToken ct);
    Task RemoverArquivoAsync(string? caminhoRelativo, CancellationToken ct);
}
