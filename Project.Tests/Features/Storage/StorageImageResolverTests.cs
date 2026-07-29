using Core.Storage;
using Microsoft.Extensions.Options;
using Project.Features.Veiculos.Handlers;
using Project.Infrastructure.Storage;
using Project.Pages;
using Project.Shared;
using Xunit;

namespace Project.Tests.Features.Storage;

public sealed class StorageImageResolverTests
{
    [Fact]
    public void SelectVehicleCover_DeveMontarUrlPublicaPeloBlobName()
    {
        var resolver = CreateResolver("https://cdn.example.com");

        var cover = resolver.SelectVehicleCover([
            new StorageImageReference(
                Url: "/uploads/veiculos/10/antiga.webp",
                BlobName: "uploads/veiculos/10/capa.webp",
                Container: "uploads/veiculos/10",
                NomeArquivo: "capa.webp",
                ContentType: "image/webp",
                SizeBytes: 123)
        ]);

        Assert.Equal("https://cdn.example.com/uploads/veiculos/10/capa.webp", cover);
    }

    [Fact]
    public void ResolveVehicleGallery_DevePreservarUrlAbsolutaQuandoBlobNameEstaVazio()
    {
        var resolver = CreateResolver("https://cdn.example.com");
        var source = "https://cdn.example.com/uploads/veiculos/20/foto.webp";

        var gallery = resolver.ResolveVehicleGallery([new StorageImageReference(source)], includeDefault: true);

        Assert.Equal([source], gallery);
    }

    [Fact]
    public void SelectVehicleCover_DeveUsarFallbackQuandoBlobNameEUrlEstaoVazios()
    {
        var resolver = CreateResolver("https://cdn.example.com");

        var cover = resolver.SelectVehicleCover([new StorageImageReference(null)]);

        Assert.Equal(VehicleImageHelper.DefaultVehicleImage, cover);
    }

    [Fact]
    public void ResolveSellerPhoto_DevePreservarUrlAbsoluta()
    {
        var resolver = CreateResolver("https://cdn.example.com");
        var source = "https://cdn.example.com/uploads/vendedores/ana.webp";

        var photo = resolver.ResolveSellerPhoto(source);

        Assert.Equal(source, photo);
    }

    [Fact]
    public void ResolveSellerPhoto_DeveMontarUrlPublicaParaCaminhoDeVendedor()
    {
        var resolver = CreateResolver("https://cdn.example.com");

        var photo = resolver.ResolveSellerPhoto("/uploads/vendedores/ana.webp");

        Assert.Equal("https://cdn.example.com/uploads/vendedores/ana.webp", photo);
    }

    [Fact]
    public void StorageImageResolver_NaoDeveDependerDeServicosDeStorageOuCache()
    {
        var constructor = typeof(StorageImageResolver).GetConstructors().Single();
        var parameterTypes = constructor.GetParameters().Select(x => x.ParameterType).ToList();

        Assert.DoesNotContain(typeof(IStorageService), parameterTypes);
        Assert.DoesNotContain(typeof(R2StorageService), parameterTypes);
        Assert.DoesNotContain(typeof(LocalWebRootStorageService), parameterTypes);
        Assert.DoesNotContain(parameterTypes, x => x.FullName == "Microsoft.Extensions.Caching.Memory.IMemoryCache");
    }

    [Theory]
    [InlineData(typeof(BuscarVeiculosQueryHandler))]
    [InlineData(typeof(VeiculoModel))]
    public void FluxosPublicos_NaoDevemReceberIStorageServicePorInjecao(Type type)
    {
        var constructors = type.GetConstructors();

        Assert.All(constructors, constructor =>
        {
            var parameterTypes = constructor.GetParameters().Select(x => x.ParameterType);
            Assert.DoesNotContain(typeof(IStorageService), parameterTypes);
        });
    }

    private static StorageImageResolver CreateResolver(string publicBaseUrl)
        => new(Options.Create(new StorageOptions
        {
            Provider = StorageProviders.R2,
            R2 = new R2StorageOptions
            {
                PublicBaseUrl = publicBaseUrl
            }
        }));
}
