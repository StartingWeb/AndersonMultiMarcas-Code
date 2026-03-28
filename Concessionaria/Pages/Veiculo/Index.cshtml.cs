using Core.Enums;
using Core.Interfaces;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concessionaria.Pages.Veiculo;

public class IndexModel : PageModel
{
    private readonly IVeiculoService _veiculoService;

    public IndexModel(IVeiculoService veiculoService)
    {
        _veiculoService = veiculoService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filtro { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Ordem { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? SomenteAtivos { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? SomenteVendidos { get; set; }

    public List<Domain.Veiculo> Veiculos { get; set; } = new();

    public async Task OnGetAsync()
    {
        SomenteVendidos ??= false;

        var response = await _veiculoService.ListarAsync();

        if (response.Status != PackageStatus.Success || response.Data == null)
        {
            Veiculos = new List<Domain.Veiculo>();
            return;
        }

        var query = response.Data.AsQueryable();

        // =========================
        // FILTRO TEXTO
        // =========================
        if (!string.IsNullOrWhiteSpace(Filtro))
        {
            var filtro = Filtro.Trim().ToLower();

            query = query.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Titulo) && x.Titulo.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Modelo) && x.Modelo.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Versao) && x.Versao.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Placa) && x.Placa.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Cor) && x.Cor.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Combustivel) && x.Combustivel.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Cambio) && x.Cambio.ToLower().Contains(filtro)) ||
                (x.Marca != null && !string.IsNullOrWhiteSpace(x.Marca.Nome) && x.Marca.Nome.ToLower().Contains(filtro)) ||
                (x.Loja != null && !string.IsNullOrWhiteSpace(x.Loja.Nome) && x.Loja.Nome.ToLower().Contains(filtro)) ||
                (x.Vendedor != null && !string.IsNullOrWhiteSpace(x.Vendedor.Nome) && x.Vendedor.Nome.ToLower().Contains(filtro))
            );
        }

        // =========================
        // FILTRO STATUS
        // =========================
        if (SomenteAtivos.HasValue)
        {
            query = query.Where(x => x.Ativo == SomenteAtivos.Value);
        }

        // =========================
        // FILTRO VENDA
        // =========================
        query = query.Where(x => x.Vendido == SomenteVendidos.Value);

        // =========================
        // ORDENAÇÃO
        // =========================
        query = Ordem switch
        {
            "antigos" => query.OrderBy(x => x.DataCadastro),
            "titulo" => query.OrderBy(x => x.Titulo),
            "titulo_desc" => query.OrderByDescending(x => x.Titulo),
            "preco_asc" => query.OrderBy(x => x.PrecoPromocional ?? x.PrecoVenda ?? decimal.MaxValue),
            "preco_desc" => query.OrderByDescending(x => x.PrecoPromocional ?? x.PrecoVenda ?? 0),
            _ => query.OrderByDescending(x => x.DataCadastro)
        };

        Veiculos = query.ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var response = await _veiculoService.ExcluirAsync(id);

        TempData[response.Status == PackageStatus.Success ? "Success" : "Error"] =
            response.UserMessage ?? (response.Status == PackageStatus.Success
                ? "Veículo excluído com sucesso."
                : "Não foi possível excluir o veículo.");

        return RedirectToPage("./Index", new
        {
            Filtro,
            Ordem,
            SomenteAtivos,
            SomenteVendidos
        });
    }
}
