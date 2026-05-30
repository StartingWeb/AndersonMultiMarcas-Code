using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
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
    IWebHostEnvironment environment,
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
    [ResponseCache(Duration = 60 * 60 * 24 * 30, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> GetImage([FromQuery] string src, [FromQuery] int? w, [FromQuery] int? q, CancellationToken ct)
    {
        if (!TryResolveLocalPath(src, out var filePath))
        {
            if (!TryResolveLocalPath(DefaultImageVirtualPath, out filePath))
            {
                return NotFound();
            }
        }

        var extension = Path.GetExtension(filePath);
        if (!OptimizableExtensions.Contains(extension))
        {
            return PhysicalFile(filePath, GetContentType(filePath));
        }

        var width = Math.Clamp(w ?? 0, 0, 2200);
        var quality = Math.Clamp(q ?? 68, 35, 90);
        var format = ShouldUseWebp(Request.Headers.Accept.ToString()) ? "webp" : NormalizeFormat(extension);
        var lastWrite = System.IO.File.GetLastWriteTimeUtc(filePath).Ticks;
        var etag = $"\"img-{lastWrite}-{width}-{quality}-{format}\"";

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public,max-age=2592000,immutable";

        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var cacheKey = $"imgopt::{filePath}::{lastWrite}::{width}::{quality}::{format}";
        var payload = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30);

            using var image = await Image.LoadAsync(filePath, ct);
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

    private bool TryResolveLocalPath(string src, out string filePath)
    {
        filePath = string.Empty;

        if (string.IsNullOrWhiteSpace(src) || !src.StartsWith('/'))
        {
            return false;
        }

        var relativePath = src.Split('?', '#')[0].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(environment.WebRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));

        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
        {
            return false;
        }

        filePath = fullPath;
        return true;
    }

    private static string GetContentType(string filePath)
        => ContentTypeProvider.TryGetContentType(filePath, out var contentType) ? contentType : "application/octet-stream";

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
