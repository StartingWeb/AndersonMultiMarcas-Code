using Project.Shared;
using Xunit;

namespace Project.Tests.Shared;

public sealed class ImageUrlBuilderTests
{
    [Fact]
    public void Build_NaoDeveOtimizarImagemPadraoDeVeiculo()
    {
        var url = ImageUrlBuilder.Build(VehicleImageHelper.DefaultVehicleImage, 640, 66);

        Assert.Equal(VehicleImageHelper.DefaultVehicleImage, url);
    }

    [Fact]
    public void BuildSrcSet_NaoDeveGerarSrcSetParaImagemPadraoDeVeiculo()
    {
        var srcSet = ImageUrlBuilder.BuildSrcSet(VehicleImageHelper.DefaultVehicleImage, 320, 640);

        Assert.Equal(string.Empty, srcSet);
    }

    [Fact]
    public void Build_DeveOtimizarImagemRealDeVeiculo()
    {
        var url = ImageUrlBuilder.Build("/uploads/veiculos/10/carro.jpg", 640, 66);

        Assert.StartsWith("/media/img?", url);
        Assert.Contains("src=%2Fuploads%2Fveiculos%2F10%2Fcarro.jpg", url);
        Assert.Contains("w=640", url);
        Assert.Contains("q=66", url);
    }
}
