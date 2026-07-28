using Core.Storage;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Project.Features.Veiculos.Services;

public sealed class VeiculoMediaService(
    IStorageService storage,
    IOptions<StorageOptions> storageOptions) : IVeiculoMediaService
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const string OutputContentType = "image/webp";

    public async Task<IReadOnlyCollection<VeiculoMediaUploadResult>> ProcessarUploadAsync(
        int veiculoId,
        IReadOnlyCollection<IFormFile> arquivos,
        CancellationToken ct)
    {
        var resultado = new List<VeiculoMediaUploadResult>();

        foreach (var arquivo in arquivos)
        {
            var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext)) continue;

            await using var readStream = arquivo.OpenReadStream();
            using var image = await Image.LoadAsync(readStream, ct);

            image.Mutate(x => x.AutoOrient());
            if (image.Width > 1920)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1920, 1920)
                }));
            }

            var nomeArquivo = $"{Guid.NewGuid():N}.webp";
            var key = StoragePath.Combine("uploads", "veiculos", veiculoId.ToString(), nomeArquivo);
            await using var output = new MemoryStream();
            await image.SaveAsync(output, new WebpEncoder { Quality = 75 }, ct);
            output.Position = 0;

            var stored = await storage.SaveAsync(key, output, OutputContentType, ct);
            resultado.Add(new VeiculoMediaUploadResult(
                stored.Url,
                nomeArquivo,
                stored.Key,
                stored.Container,
                stored.ContentType,
                stored.SizeBytes));
        }

        return resultado;
    }

    public Task RemoverArquivoAsync(string? caminhoRelativo, CancellationToken ct)
        => RemoverArquivoAsync(new StorageImageReference(caminhoRelativo), ct);

    public async Task RemoverArquivoAsync(StorageImageReference reference, CancellationToken ct)
    {
        if (StoragePath.TryGetKey(reference, PublicBaseUrls(), out var key))
        {
            await storage.DeleteAsync(key, ct);
        }
    }

    private IEnumerable<string?> PublicBaseUrls()
    {
        yield return storageOptions.Value.PublicBaseUrl;
        yield return storageOptions.Value.R2.PublicBaseUrl;
        yield return storageOptions.Value.R2.ServiceUrl;
    }
}
