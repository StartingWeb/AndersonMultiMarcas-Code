using Core.Enums;
using Core.Interfaces;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concessionaria.Pages.Marca;

public class IndexModel : PageModel
{
    private readonly IMarcaService _marcaService;

    public IndexModel(IMarcaService marcaService)
    {
        _marcaService = marcaService;
    }

    public List<Domain.Marca> Marcas { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Filtro { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Ordem { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? SomenteAtivas { get; set; }

    public async Task OnGetAsync()
    {
        var resultado = await _marcaService.ListarAsync();

        if (resultado.Status == PackageStatus.Success && resultado.Data != null)
            Marcas = resultado.Data;
        else
            Marcas = new List<Domain.Marca>();

        if (!string.IsNullOrWhiteSpace(Filtro))
        {
            var termo = Filtro.Trim().ToLower();

            Marcas = Marcas
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(x.Nome) && x.Nome.ToLower().Contains(termo)) ||
                    (!string.IsNullOrWhiteSpace(x.LogoUrl) && x.LogoUrl.ToLower().Contains(termo)) ||
                    x.Id.ToString().Contains(termo))
                .ToList();
        }

        if (SomenteAtivas.HasValue)
        {
            Marcas = Marcas
                .Where(x => x.Ativo == SomenteAtivas.Value)
                .ToList();
        }

        Marcas = Ordem switch
        {
            "nome_desc" => Marcas.OrderByDescending(x => x.Nome).ToList(),
            "recentes" => Marcas.OrderByDescending(x => x.DataCadastro).ToList(),
            "antigas" => Marcas.OrderBy(x => x.DataCadastro).ToList(),
            _ => Marcas.OrderBy(x => x.Nome).ToList()
        };
    }
}