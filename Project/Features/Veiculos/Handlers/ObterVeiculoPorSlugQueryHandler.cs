using System.Text;
using Core.Storage;
using Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Features.Veiculos.DTOs;
using Project.Features.Veiculos.Queries;
using Project.Features.Veiculos.Services;
using Project.Infrastructure.Storage;
using Project.Shared;

namespace Project.Features.Veiculos.Handlers;

public sealed class ObterVeiculoPorSlugQueryHandler(
    ApplicationDbContext db,
    IVeiculoSlugService slugService,
    IStorageImageResolver imageResolver) : IRequestHandler<ObterVeiculoPorSlugQuery, Result<VeiculoDetalheDto>>
{
    public async Task<Result<VeiculoDetalheDto>> Handle(ObterVeiculoPorSlugQuery request, CancellationToken cancellationToken)
    {
        var id = slugService.ObterIdPorSlug(request.Slug);
        if (!id.HasValue) return Result<VeiculoDetalheDto>.Failure("Slug invalido.");

        var veiculo = await db.Veiculos.AsNoTracking()
            .Where(x => x.Id == id.Value && x.Ativo && !x.Vendido)
            .Select(x => new
            {
                x.Id,
                x.Titulo,
                x.Modelo,
                x.Versao,
                x.AnoFabricacao,
                x.AnoModelo,
                x.Cor,
                x.Combustivel,
                x.Cambio,
                x.Quilometragem,
                x.Placa,
                PrecoVenda = x.PrecoVenda.Valor,
                x.Descricao,
                x.UrlVideo,
                MarcaNome = x.Marca.Nome,
                LojaNome = x.Loja.Nome,
                Midias = x.Midias
                    .Where(m => m.Ativo && m.Tipo == Domain.Enums.TipoMidia.Imagem)
                    .OrderByDescending(m => m.Capa)
                    .ThenBy(m => m.Ordem)
                    .Select(m => new MediaProjection
                    {
                        Url = m.Url,
                        BlobName = m.BlobName,
                        Container = m.Container,
                        NomeArquivo = m.NomeArquivo,
                        ContentType = m.ContentType,
                        TamanhoBytes = m.TamanhoBytes
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (veiculo is null) return Result<VeiculoDetalheDto>.Failure("Veiculo nao encontrado.");

        var slug = slugService.CriarSlug(veiculo.Titulo, veiculo.Modelo, veiculo.Versao, veiculo.Id);
        var canonical = $"{request.BaseUrl.TrimEnd('/')}/api/veiculos/{slug}";
        var vehicleMidias = imageResolver.ResolveVehicleGallery(
            veiculo.Midias.Select(ToStorageReference),
            includeDefault: false);

        var image = vehicleMidias.FirstOrDefault() ?? string.Empty;
        var tituloSeo = $"{veiculo.Titulo} {veiculo.Modelo} {veiculo.Versao}".Trim();
        var descricaoSeo = $"{veiculo.MarcaNome} {veiculo.Modelo} {veiculo.AnoModelo} por {veiculo.PrecoVenda:C}.";

        var breadcrumb = $"{{\"@context\":\"https://schema.org\",\"@type\":\"BreadcrumbList\",\"itemListElement\":[{{\"@type\":\"ListItem\",\"position\":1,\"name\":\"Home\",\"item\":\"{request.BaseUrl}\"}},{{\"@type\":\"ListItem\",\"position\":2,\"name\":\"Veiculos\",\"item\":\"{request.BaseUrl}/api/veiculos\"}},{{\"@type\":\"ListItem\",\"position\":3,\"name\":\"{tituloSeo}\",\"item\":\"{canonical}\"}}]}}";
        var vehicleJson = new StringBuilder();
        vehicleJson.Append("{\"@context\":\"https://schema.org\",\"@type\":\"Vehicle\",");
        vehicleJson.Append($"\"name\":\"{tituloSeo}\",");
        vehicleJson.Append($"\"brand\":\"{veiculo.MarcaNome}\",");
        vehicleJson.Append($"\"vehicleModelDate\":\"{veiculo.AnoModelo}\",");
        vehicleJson.Append($"\"color\":\"{veiculo.Cor}\",");
        vehicleJson.Append($"\"fuelType\":\"{veiculo.Combustivel}\",");
        vehicleJson.Append($"\"url\":\"{canonical}\"");
        vehicleJson.Append("}");

        var caracteristicas = await db.VeiculoCaracteristicas.AsNoTracking()
            .FirstOrDefaultAsync(x => x.VeiculoId == veiculo.Id, cancellationToken);

        var dto = new VeiculoDetalheDto
        {
            Id = veiculo.Id,
            Slug = slug,
            Titulo = veiculo.Titulo,
            Modelo = veiculo.Modelo,
            Versao = veiculo.Versao,
            AnoFabricacao = veiculo.AnoFabricacao,
            AnoModelo = veiculo.AnoModelo,
            Cor = veiculo.Cor,
            Combustivel = veiculo.Combustivel,
            Cambio = veiculo.Cambio,
            Quilometragem = veiculo.Quilometragem,
            Placa = veiculo.Placa,
            PrecoVenda = veiculo.PrecoVenda,
            Descricao = veiculo.Descricao,
            UrlVideo = veiculo.UrlVideo,
            MarcaNome = veiculo.MarcaNome,
            LojaNome = veiculo.LojaNome,
            Midias = vehicleMidias,
            Opcionais = caracteristicas?.OpcionaisAtivos().Select(x => x.ToString()).ToList() ?? [],
            SeoTitle = tituloSeo,
            SeoDescription = descricaoSeo,
            CanonicalUrl = canonical,
            OpenGraphImage = image,
            BreadcrumbJsonLd = breadcrumb,
            VehicleJsonLd = vehicleJson.ToString()
        };

        return Result<VeiculoDetalheDto>.Success(dto);
    }

    private sealed class MediaProjection
    {
        public string? Url { get; init; }
        public string? BlobName { get; init; }
        public string? Container { get; init; }
        public string? NomeArquivo { get; init; }
        public string? ContentType { get; init; }
        public long? TamanhoBytes { get; init; }
    }

    private static StorageImageReference ToStorageReference(MediaProjection media)
        => new(media.Url, media.BlobName, media.Container, media.NomeArquivo, media.ContentType, media.TamanhoBytes);
}
