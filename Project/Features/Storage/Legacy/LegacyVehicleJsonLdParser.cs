using System.Text.Json;
using HtmlAgilityPack;

namespace Project.Features.Storage.Legacy;

public sealed class LegacyVehicleJsonLdParser
{
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public IReadOnlyList<string> ExtractVehicleImageUrls(string html, Uri pageUri)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var imageUrls = new List<string>();
        var scripts = document.DocumentNode
            .Descendants("script")
            .Where(node => IsJsonLdScript(node.GetAttributeValue("type", string.Empty)));

        foreach (var script in scripts)
        {
            var json = HtmlEntity.DeEntitize(script.InnerText)?.Trim();
            if (string.IsNullOrWhiteSpace(json))
            {
                continue;
            }

            using var jsonDocument = JsonDocument.Parse(json, JsonOptions);
            ExtractFromElement(jsonDocument.RootElement, pageUri, imageUrls);
        }

        return imageUrls
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsJsonLdScript(string type)
        => HtmlEntity.DeEntitize(type)
            .Split(';', 2)[0]
            .Trim()
            .Equals("application/ld+json", StringComparison.OrdinalIgnoreCase);

    private static void ExtractFromElement(JsonElement element, Uri pageUri, List<string> imageUrls)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                ExtractFromElement(item, pageUri, imageUrls);
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (IsVehicle(element))
        {
            ExtractImages(element, pageUri, imageUrls);
            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                ExtractFromElement(property.Value, pageUri, imageUrls);
            }
        }
    }

    private static bool IsVehicle(JsonElement element)
    {
        if (!TryGetPropertyIgnoreCase(element, "@type", out var typeElement)
            && !TryGetPropertyIgnoreCase(element, "type", out typeElement))
        {
            return false;
        }

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return IsVehicleType(typeElement.GetString());
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            return typeElement.EnumerateArray()
                .Any(item => item.ValueKind == JsonValueKind.String && IsVehicleType(item.GetString()));
        }

        return false;
    }

    private static bool IsVehicleType(string? value)
        => string.Equals(value, "Vehicle", StringComparison.OrdinalIgnoreCase);

    private static void ExtractImages(JsonElement vehicle, Uri pageUri, List<string> imageUrls)
    {
        if (!TryGetPropertyIgnoreCase(vehicle, "image", out var imageElement))
        {
            return;
        }

        ExtractImageValue(imageElement, pageUri, imageUrls);
    }

    private static void ExtractImageValue(JsonElement element, Uri pageUri, List<string> imageUrls)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            AddUrl(element.GetString(), pageUri, imageUrls);
            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                ExtractImageValue(item, pageUri, imageUrls);
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (TryGetPropertyIgnoreCase(element, "url", out var urlElement)
            && urlElement.ValueKind == JsonValueKind.String)
        {
            AddUrl(urlElement.GetString(), pageUri, imageUrls);
        }

        if (TryGetPropertyIgnoreCase(element, "contentUrl", out var contentUrlElement)
            && contentUrlElement.ValueKind == JsonValueKind.String)
        {
            AddUrl(contentUrlElement.GetString(), pageUri, imageUrls);
        }
    }

    private static void AddUrl(string? value, Uri pageUri, List<string> imageUrls)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(pageUri, value.Trim(), out var imageUri)
            || imageUri.Scheme != Uri.UriSchemeHttps)
        {
            return;
        }

        imageUrls.Add(imageUri.ToString());
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
