using Project.Shared;
using Xunit;

namespace Project.Tests.Shared;

public sealed class VehicleImageHelperTests
{
    [Theory]
    [InlineData("uploads/veiculos/1/carro.webp", "/uploads/veiculos/1/carro.webp")]
    [InlineData("wwwroot/uploads/veiculos/2/carro.jpg", "/uploads/veiculos/2/carro.jpg")]
    [InlineData("/media/img?src=%2Fuploads%2Fveiculos%2F3%2Fcarro.png&w=520&q=68", "/uploads/veiculos/3/carro.png")]
    [InlineData("https://cdn.example.com/uploads/veiculos/4/carro.webp", "https://cdn.example.com/uploads/veiculos/4/carro.webp")]
    [InlineData("/media/img?src=https%3A%2F%2Fcdn.example.com%2Fuploads%2Fveiculos%2F5%2Fcarro.webp&w=520&q=68", "https://cdn.example.com/uploads/veiculos/5/carro.webp")]
    public void Normalize_DeveAceitarCaminhosValidosDeVeiculos(string source, string expected)
    {
        Assert.Equal(expected, VehicleImageHelper.Normalize(source));
    }

    [Theory]
    [InlineData("/uploads/vendedores/ana.webp")]
    [InlineData("uploads/vendedores/joao.jpg")]
    [InlineData("/img/Vendedores1.JPG")]
    [InlineData("/media/img?src=%2Fimg%2FVendedores2.JPG&w=520&q=68")]
    [InlineData("/media/img?src=%2Fuploads%2Fvendedores%2Fana.webp&w=96&q=65")]
    public void Normalize_DeveRejeitarImagensDeVendedores(string source)
    {
        Assert.Equal(VehicleImageHelper.DefaultVehicleImage, VehicleImageHelper.Normalize(source));
        Assert.False(VehicleImageHelper.TryNormalize(source, out _));
    }
}
