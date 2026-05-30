using FluentValidation;
using Project.Features.Veiculos.Commands;

namespace Project.Features.Veiculos.Validators;

public sealed class UploadMidiaVeiculoCommandValidator : AbstractValidator<UploadMidiaVeiculoCommand>
{
    private static readonly string[] Allowed = ["image/jpeg", "image/png", "image/webp"];

    public UploadMidiaVeiculoCommandValidator()
    {
        RuleFor(x => x.Dto.VeiculoId).GreaterThan(0);
        RuleFor(x => x.Arquivos).NotEmpty();
        RuleForEach(x => x.Arquivos).Must(f => f.Length > 0 && f.Length <= 10 * 1024 * 1024).WithMessage("Arquivo invalido ou muito grande (max 10MB).");
        RuleForEach(x => x.Arquivos).Must(f => Allowed.Contains(f.ContentType)).WithMessage("Formato de imagem nao suportado.");
    }
}
