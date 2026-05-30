using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Project.Features.Veiculos.Services;

public sealed class VeiculoMediaService(IWebHostEnvironment environment) : IVeiculoMediaService
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<IReadOnlyCollection<(string Url, string NomeArquivo, long TamanhoBytes)>> ProcessarUploadAsync(
        int veiculoId,
        IReadOnlyCollection<IFormFile> arquivos,
        CancellationToken ct)
    {
        var pastaRelativa = Path.Combine("uploads", "veiculos", veiculoId.ToString());
        var pastaFisica = Path.Combine(environment.WebRootPath, pastaRelativa);
        Directory.CreateDirectory(pastaFisica);

        var resultado = new List<(string Url, string NomeArquivo, long TamanhoBytes)>();

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
            var caminhoFisico = Path.Combine(pastaFisica, nomeArquivo);
            await using var output = File.Create(caminhoFisico);
            await image.SaveAsync(output, new WebpEncoder { Quality = 75 }, ct);

            var info = new FileInfo(caminhoFisico);
            var url = $"/{pastaRelativa.Replace('\\', '/')}/{nomeArquivo}";
            resultado.Add((url, nomeArquivo, info.Length));
        }

        return resultado;
    }

    public Task RemoverArquivoAsync(string? caminhoRelativo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(caminhoRelativo)) return Task.CompletedTask;

        var normalized = caminhoRelativo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var completo = Path.Combine(environment.WebRootPath, normalized);

        if (File.Exists(completo))
        {
            File.Delete(completo);
        }

        return Task.CompletedTask;
    }
}
