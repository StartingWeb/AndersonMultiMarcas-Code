using Project.Shared;
using Xunit;

namespace Project.Tests.Shared;

public sealed class VehicleImageHelperTests
{
    [Theory]
    [InlineData("uploads/veiculos/1/carro.webp", "/uploads/veiculos/1/carro.webp")]
    [InlineData("wwwroot/uploads/veiculos/2/carro.jpg", "/uploads/veiculos/2/carro.jpg")]
    [InlineData("/media/img?src=%2Fuploads%2Fveiculos%2F3%2Fcarro.png&w=520&q=68", "/uploads/veiculos/3/carro.png")]
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
