using Domain.Enums;
using Project.Features.Veiculos.Commands;
using Project.Features.Veiculos.DTOs;
using Project.Features.Veiculos.Validators;
using Xunit;

namespace Project.Tests.Features.Veiculos;

public sealed class VeiculoCommandValidatorTests
{
    [Fact]
    public void CriarVeiculo_DeveAceitarPrecoZero()
    {
        var validator = new CriarVeiculoCommandValidator();

        var result = validator.Validate(new CriarVeiculoCommand(CreateDto(0m)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AtualizarVeiculo_DeveAceitarPrecoZero()
    {
        var validator = new AtualizarVeiculoCommandValidator();

        var result = validator.Validate(new AtualizarVeiculoCommand(new VeiculoUpdateDto
        {
            Id = 1,
            LojaId = 1,
            MarcaId = 1,
            Titulo = "Chevrolet",
            Modelo = "Onix",
            AnoFabricacao = 2023,
            AnoModelo = 2024,
            Combustivel = Combustivel.Flex,
            Cambio = Cambio.Automatico,
            PrecoVenda = 0m
        }));

        Assert.True(result.IsValid);
    }

    private static VeiculoCreateDto CreateDto(decimal preco)
        => new()
        {
            LojaId = 1,
            MarcaId = 1,
            Titulo = "Chevrolet",
            Modelo = "Onix",
            AnoFabricacao = 2023,
            AnoModelo = 2024,
            Combustivel = Combustivel.Flex,
            Cambio = Cambio.Automatico,
            PrecoVenda = preco
        };
}
