using Core.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Project.Pages;

public class EmpresaModel : PageModel
{
    private readonly ILojaService _lojaService;
    private readonly IVeiculoService _veiculoService;

    public EmpresaModel(ILojaService lojaService, IVeiculoService veiculoService)
    {
        _lojaService = lojaService;
        _veiculoService = veiculoService;
    }

    public int TotalVeiculosAtivos { get; private set; }
    public int TotalLojasAtivas { get; private set; }
    public IReadOnlyList<StoreItem> Stores { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["ShowHero"] = false;

        var lojasTask = _lojaService.ListarAsync();
        var veiculosTask = _veiculoService.ListarAtivosAsync();

        await Task.WhenAll(lojasTask, veiculosTask);

        var lojas = lojasTask.Result.Data ?? [];
        var veiculos = veiculosTask.Result.Data ?? [];

        var lojasAtivas = lojas.Where(loja => loja.Ativo).OrderBy(loja => loja.Nome).ToList();

        TotalLojasAtivas = lojasAtivas.Count;
        TotalVeiculosAtivos = veiculos.Count(veiculo => veiculo.Ativo && !veiculo.Vendido);

        Stores = lojasAtivas
            .Select(loja => new StoreItem
            {
                Nome = loja.Nome,
                Endereco = string.Join(", ", new[]
                {
                    string.Join(", ", new[] { loja.Endereco, loja.Numero }.Where(value => !string.IsNullOrWhiteSpace(value))),
                    loja.Bairro,
                    string.Join(" - ", new[] { loja.Cidade, loja.Uf }.Where(value => !string.IsNullOrWhiteSpace(value))),
                    loja.Cep
                }.Where(value => !string.IsNullOrWhiteSpace(value)))
            })
            .ToList();
    }

    public sealed class StoreItem
    {
        public string Nome { get; init; } = string.Empty;
        public string Endereco { get; init; } = string.Empty;
    }
}
