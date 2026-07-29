using System.Security.Cryptography;
using System.Text;
using Core.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Project.Infrastructure.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Project.Controllers;

[ApiController]
[Route("media")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class MediaController(
    LocalWebRootStorageService local,
    R2StorageService r2,
    IOptions<StorageOptions> storageOptions,
    IMemoryCache cache) : ControllerBase
{
    private const string DefaultImageVirtualPath = "/img/carroDefault.png";
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
    private static readonly HashSet<string> OptimizableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    [HttpGet("img")]
    [ResponseCache(Duration = 60 * 60 * 24 * 30, Location = ResponseCacheLocation.Any, NoStore = false, VaryByHeader = "Accept", VaryByQueryKeys = ["src", "w", "q"])]
    public async Task<IActionResult> GetImage([FromQuery] string src, [FromQuery] int? w, [FromQuery] int? q, CancellationToken ct)
    {
        var storageKey = ResolveKey(src);
        if (storageKey is null)
        {
            storageKey = ResolveKey(DefaultImageVirtualPath);
            if (storageKey is null)
            {
                return NotFound();
            }
        }

        var extension = Path.GetExtension(storageKey);
        if (!OptimizableExtensions.Contains(extension))
        {
            var raw = await OpenRequestedOrDefaultAsync(storageKey, ct);
            return raw is null ? NotFound() : File(raw, GetContentType(storageKey));
        }

        var width = Math.Clamp(w ?? 0, 0, 2200);
        var quality = Math.Clamp(q ?? 68, 35, 90);
        var format = ShouldUseWebp(Request.Headers.Accept.ToString()) ? "webp" : NormalizeFormat(extension);
        var etag = $"\"img-{StableHash(storageKey)}-{width}-{quality}-{format}\"";

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public,max-age=2592000,immutable";

        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var cacheKey = $"imgopt::{storageKey}::{width}::{quality}::{format}";
        var payload = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30);

            await using var input = await OpenRequestedOrDefaultAsync(storageKey, ct)
                ?? throw new FileNotFoundException("Imagem nao encontrada no storage.", storageKey);
            using var image = await Image.LoadAsync(input, ct);
            image.Mutate(ctx =>
            {
                ctx.AutoOrient();

                if (width > 0 && image.Width > width)
                {
                    var height = (int)Math.Round(image.Height * (width / (double)image.Width));
                    ctx.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.Lanczos3,
                        Size = new Size(width, height)
                    });
                }
            });

            await using var output = new MemoryStream();
            var contentType = await SaveImageAsync(image, output, format, quality, ct);
            return new OptimizedImagePayload(output.ToArray(), contentType);
        });

        return File(payload!.Bytes, payload.ContentType);
    }

    private string? ResolveKey(string? src)
    {
        if (!StoragePath.TryGetKeyFromSource(src, PublicBaseUrls(), out var key))
        {
            return null;
        }

        return key;
    }

    private async Task<Stream?> OpenReadForMediaAsync(string key, CancellationToken ct)
    {
        var normalizedKey = StoragePath.NormalizeKey(key);

        if (!StoragePath.IsVehicleKey(normalizedKey) && !StoragePath.IsSellerKey(normalizedKey))
        {
            return await local.OpenReadAsync(normalizedKey, ct);
        }

        var localStream = await local.OpenReadAsync(normalizedKey, ct);
        if (localStream is not null)
        {
            return localStream;
        }

        return r2.IsConfigured ? await r2.OpenReadAsync(normalizedKey, ct) : null;
    }

    private async Task<Stream?> OpenRequestedOrDefaultAsync(string key, CancellationToken ct)
    {
        var stream = await OpenReadForMediaAsync(key, ct);
        if (stream is not null)
        {
            return stream;
        }

        var fallbackKey = ResolveKey(DefaultImageVirtualPath);
        return fallbackKey is null || string.Equals(fallbackKey, key, StringComparison.OrdinalIgnoreCase)
            ? null
            : await OpenReadForMediaAsync(fallbackKey, ct);
    }

    private IEnumerable<string?> PublicBaseUrls()
    {
        yield return storageOptions.Value.PublicBaseUrl;
        yield return storageOptions.Value.R2.PublicBaseUrl;
        yield return storageOptions.Value.R2.ServiceUrl;
    }

    private static string GetContentType(string key)
        => ContentTypeProvider.TryGetContentType(key, out var contentType) ? contentType : "application/octet-stream";

    private static string StableHash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..16];

    private static bool ShouldUseWebp(string? acceptHeader)
        => !string.IsNullOrWhiteSpace(acceptHeader) && acceptHeader.Contains("image/webp", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeFormat(string extension)
        => extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpeg";

    private static async Task<string> SaveImageAsync(Image image, Stream output, string format, int quality, CancellationToken ct)
    {
        if (string.Equals(format, "webp", StringComparison.OrdinalIgnoreCase))
        {
            await image.SaveAsWebpAsync(output, new WebpEncoder
            {
                Quality = quality,
                FileFormat = WebpFileFormatType.Lossy,
                Method = WebpEncodingMethod.BestQuality
            }, ct);
            return "image/webp";
        }

        if (string.Equals(format, "png", StringComparison.OrdinalIgnoreCase))
        {
            await image.SaveAsPngAsync(output, new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.Level6
            }, ct);
            return "image/png";
        }

        await image.SaveAsJpegAsync(output, new JpegEncoder
        {
            Quality = quality
        }, ct);
        return "image/jpeg";
    }

    private sealed record OptimizedImagePayload(byte[] Bytes, string ContentType);
}
