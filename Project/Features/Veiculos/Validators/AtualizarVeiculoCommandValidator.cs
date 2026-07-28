using FluentValidation;
using Project.Features.Veiculos.Commands;

namespace Project.Features.Veiculos.Validators;

public sealed class AtualizarVeiculoCommandValidator : AbstractValidator<AtualizarVeiculoCommand>
{
    public AtualizarVeiculoCommandValidator()
    {
        RuleFor(x => x.Dto.Id).GreaterThan(0);
        RuleFor(x => x.Dto.Titulo).MaximumLength(180);
        RuleFor(x => x.Dto.Modelo).MaximumLength(150);
        RuleFor(x => x.Dto.PrecoVenda).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.AnoModelo).Must(x => x == 0 || (x >= 1950 && x <= DateTime.UtcNow.Year + 1));
        RuleFor(x => x.Dto.AnoFabricacao).Must(x => !x.HasValue || (x >= 1950 && x <= DateTime.UtcNow.Year + 1));
        RuleFor(x => x.Dto.Placa)
            .Must(x => string.IsNullOrWhiteSpace(x) || System.Text.RegularExpressions.Regex.IsMatch(x.Trim().ToUpperInvariant(), "^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$"))
            .WithMessage("Placa invalida.");
    }
}
