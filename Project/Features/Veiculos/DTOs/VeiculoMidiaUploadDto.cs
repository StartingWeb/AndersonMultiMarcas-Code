namespace Project.Features.Veiculos.DTOs;

public sealed class VeiculoMidiaUploadDto
{
    public int VeiculoId { get; init; }
    public bool DefinirPrimeiraComoCapa { get; init; } = true;
}
