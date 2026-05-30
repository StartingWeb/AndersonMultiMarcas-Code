using Domain.Enums;

namespace Project.Features.Veiculos.DTOs;

public sealed class VeiculoUpdateDto : VeiculoCreateDto
{
    public int Id { get; init; }
}
