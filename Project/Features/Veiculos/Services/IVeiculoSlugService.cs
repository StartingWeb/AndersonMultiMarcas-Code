namespace Project.Features.Veiculos.Services;

public interface IVeiculoSlugService
{
    string CriarSlug(string titulo, string modelo, string? versao, int id);
    int? ObterIdPorSlug(string slug);
}
