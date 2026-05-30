using Domain.Enums;

namespace Project.Features.Veiculos.DTOs;

public class VeiculoCreateDto
{
    public int LojaId { get; init; }
    public int MarcaId { get; init; }
    public int? VendedorId { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string Modelo { get; init; } = string.Empty;
    public string? Versao { get; init; }
    public int? AnoFabricacao { get; init; }
    public int AnoModelo { get; init; }
    public string? Cor { get; init; }
    public Combustivel Combustivel { get; init; }
    public Cambio Cambio { get; init; }
    public decimal PrecoVenda { get; init; }
    public int? Quilometragem { get; init; }
    public string? Placa { get; init; }
    public string? Descricao { get; init; }
    public bool Destaque { get; init; }
    public bool Seminovo { get; init; }
    public bool Financiavel { get; init; }
    public bool AceitaTroca { get; init; }
    public string? UrlVideo { get; init; }
    public IReadOnlyCollection<TipoVeiculoOpcional> Opcionais { get; init; } = [];
}
