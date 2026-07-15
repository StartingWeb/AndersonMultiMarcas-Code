using Project.Shared;
using Xunit;

namespace Project.Tests.Shared;

public sealed class SellerImageHelperTests
{
    [Theory]
    [InlineData("uploads/vendedores/ana.webp", "/uploads/vendedores/ana.webp")]
    [InlineData("wwwroot/uploads/vendedores/joao.jpg", "/uploads/vendedores/joao.jpg")]
    public void Normalize_DeveAceitarCaminhosValidosDeVendedores(string source, string expected)
    {
        Assert.Equal(expected, SellerImageHelper.Normalize(source));
    }

    [Theory]
    [InlineData("/uploads/veiculos/carro.webp")]
    [InlineData("/img/carroDefault.png")]
    [InlineData("uploads/veiculos/carro.webp")]
    [InlineData("img/Vendedores1.JPG")]
    [InlineData("/img/Vendedores2.JPG")]
    public void Normalize_DeveRejeitarImagensDeVeiculos(string source)
    {
        Assert.Null(SellerImageHelper.Normalize(source));
    }
}
