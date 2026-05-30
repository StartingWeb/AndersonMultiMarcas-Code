using Domain.Enums;

namespace Project.Features.Veiculos.DTOs;

public sealed class VeiculoDetalheDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string Modelo { get; init; } = string.Empty;
    public string? Versao { get; init; }
    public int? AnoFabricacao { get; init; }
    public int AnoModelo { get; init; }
    public string? Cor { get; init; }
    public Combustivel Combustivel { get; init; }
    public Cambio Cambio { get; init; }
    public int? Quilometragem { get; init; }
    public string? Placa { get; init; }
    public decimal PrecoVenda { get; init; }
    public string? Descricao { get; init; }
    public string? UrlVideo { get; init; }
    public string MarcaNome { get; init; } = string.Empty;
    public string LojaNome { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Midias { get; init; } = [];
    public IReadOnlyCollection<string> Opcionais { get; init; } = [];
    public string SeoTitle { get; init; } = string.Empty;
    public string SeoDescription { get; init; } = string.Empty;
    public string CanonicalUrl { get; init; } = string.Empty;
    public string OpenGraphImage { get; init; } = string.Empty;
    public string BreadcrumbJsonLd { get; init; } = string.Empty;
    public string VehicleJsonLd { get; init; } = string.Empty;
}
