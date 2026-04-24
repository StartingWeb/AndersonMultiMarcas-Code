using Core.Interfaces;
using Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Project.Pages;

public class EmpresaModel : PageModel
{
    private readonly ILojaService _lojaService;
    private readonly ApplicationDbContext _context;

    public EmpresaModel(ILojaService lojaService, ApplicationDbContext context)
    {
        _lojaService = lojaService;
        _context = context;
    }

    public int TotalVeiculosAtivos { get; private set; }
    public int TotalLojasAtivas { get; private set; }
    public IReadOnlyList<StoreItem> Stores { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["ShowHero"] = false;

        var lojasResult = await _lojaService.ListarAsync();
        TotalVeiculosAtivos = await _context.Veiculos
            .AsNoTracking()
            .CountAsync(veiculo => veiculo.Ativo && !veiculo.Vendido);

        var lojas = lojasResult.Data ?? [];

        var lojasAtivas = lojas.Where(loja => loja.Ativo).OrderBy(loja => loja.Nome).ToList();

        TotalLojasAtivas = lojasAtivas.Count;

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
