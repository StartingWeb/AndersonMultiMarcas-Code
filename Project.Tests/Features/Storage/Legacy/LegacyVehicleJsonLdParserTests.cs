using Project.Features.Storage.Legacy;
using Xunit;

namespace Project.Tests.Features.Storage.Legacy;

public sealed class LegacyVehicleJsonLdParserTests
{
    [Fact]
    public void ExtractVehicleImageUrls_DeveLerImagensDoJsonLdVehicle()
    {
        var html = """
            <html>
              <head>
                <script type="application/ld+json">
                {
                  "@context": "https://schema.org",
                  "@graph": [
                    { "@type": "WebPage", "name": "Teste" },
                    {
                      "@type": ["Vehicle", "Product"],
                      "image": [
                        "https://andersonmultimarcas.com.br/uploads/veiculos/769/1.jpg",
                        "https://andersonmultimarcas.com.br/uploads/veiculos/769/2.jpg"
                      ]
                    }
                  ]
                }
                </script>
              </head>
            </html>
            """;

        var parser = new LegacyVehicleJsonLdParser();
        var urls = parser.ExtractVehicleImageUrls(html, new Uri("https://andersonmultimarcas.com.br/veiculo/769/"));

        Assert.Equal(2, urls.Count);
        Assert.Equal("https://andersonmultimarcas.com.br/uploads/veiculos/769/1.jpg", urls[0]);
        Assert.Equal("https://andersonmultimarcas.com.br/uploads/veiculos/769/2.jpg", urls[1]);
    }

    [Fact]
    public void ExtractVehicleImageUrls_DeveIgnorarImagemHttp()
    {
        var html = """
            <script type="application/ld&#x2B;json">
            {
              "@type": "Vehicle",
              "image": [
                "http://andersonmultimarcas.com.br/uploads/veiculos/1/1.jpg",
                "https://andersonmultimarcas.com.br/uploads/veiculos/1/2.jpg"
              ]
            }
            </script>
            """;

        var parser = new LegacyVehicleJsonLdParser();
        var urls = parser.ExtractVehicleImageUrls(html, new Uri("https://andersonmultimarcas.com.br/veiculo/1/"));

        Assert.Single(urls);
        Assert.Equal("https://andersonmultimarcas.com.br/uploads/veiculos/1/2.jpg", urls[0]);
    }

    [Fact]
    public void ExtractVehicleImageUrls_DeveLerFormatoLegadoSemArrobaNoType()
    {
        var html = """
            <script type="application/ld&#x2B;json">
            {
              "context": "https://schema.org",
              "type": "Vehicle",
              "image": [
                "https://andersonmultimarcas.com.br/uploads/veiculos/964/1.webp",
                "https://andersonmultimarcas.com.br/uploads/veiculos/964/2.webp"
              ]
            }
            </script>
            """;

        var parser = new LegacyVehicleJsonLdParser();
        var urls = parser.ExtractVehicleImageUrls(html, new Uri("https://andersonmultimarcas.com.br/veiculo/964/"));

        Assert.Equal(2, urls.Count);
        Assert.Equal("https://andersonmultimarcas.com.br/uploads/veiculos/964/1.webp", urls[0]);
        Assert.Equal("https://andersonmultimarcas.com.br/uploads/veiculos/964/2.webp", urls[1]);
    }
}
