using Domain.Enums;
using Project.Shared;

namespace Project.Features.Veiculos.DTOs;

public sealed class BuscarVeiculosFiltroDto : PaginationParams
{
    public string? Busca { get; init; }
    public int? MarcaId { get; init; }
    public string? Marca { get; init; }
    public string? Modelo { get; init; }
    public int? AnoModelo { get; init; }
    public int? AnoMinimo { get; init; }
    public int? AnoMaximo { get; init; }
    public decimal? PrecoMinimo { get; init; }
    public decimal? PrecoMaximo { get; init; }
    public Combustivel? Combustivel { get; init; }
    public Cambio? Cambio { get; init; }
    public bool? Destaque { get; init; }
    public bool? Disponivel { get; init; }
    public bool? Seminovo { get; init; }
    public bool? Financiavel { get; init; }
    public bool? AceitaTroca { get; init; }
    public string OrdenarPor { get; init; } = "recentes";
}
