using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Project.Pages.Admin.VeiculosVenda;

[Authorize]
public sealed class IndexModel(ApplicationDbContext db) : PageModel
{
    private static readonly CultureInfo BrCulture = new("pt-BR");

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty]
    public SellVehicleInput Venda { get; set; } = new();

    public IReadOnlyList<VehicleSaleListItem> Veiculos { get; private set; } = [];
    public IReadOnlyList<SelectListItem> Vendedores { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Venda de Veiculos";
        ViewData["Robots"] = "noindex,nofollow";

        await LoadPageDataAsync(ct);
    }

    public async Task<IActionResult> OnPostSellAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Venda de Veiculos";
        ViewData["Robots"] = "noindex,nofollow";

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync(ct);
            return Page();
        }

        var vendedorExiste = await db.Vendedores
            .AnyAsync(x => x.Id == Venda.VendedorId && x.Ativo, ct);

        if (!vendedorExiste)
        {
            ModelState.AddModelError("Venda.VendedorId", "Selecione um vendedor ativo.");
            await LoadPageDataAsync(ct);
            return Page();
        }

        var veiculo = await db.Veiculos
            .FirstOrDefaultAsync(x => x.Id == Venda.VeiculoId && x.Ativo && !x.Vendido, ct);

        if (veiculo is null)
        {
            TempData["ErrorMessage"] = "Veiculo nao encontrado ou ja vendido.";
            return RedirectToPage(new { Search });
        }

        veiculo.MarcarComoVendido(Venda.DataVenda.Date, Venda.VendedorId);
        await db.SaveChangesAsync(ct);

        TempData["SuccessMessage"] = "Venda registrada com sucesso.";
        return RedirectToPage(new { Search });
    }

    private async Task LoadPageDataAsync(CancellationToken ct)
    {
        var query = db.Veiculos
            .AsNoTracking()
            .Where(x => x.Ativo || x.Vendido)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            query = query.Where(x =>
                x.Titulo.Contains(term) ||
                x.Modelo.Contains(term) ||
                (x.Versao != null && x.Versao.Contains(term)) ||
                (x.Placa != null && x.Placa.Contains(term)) ||
                x.Marca.Nome.Contains(term) ||
                x.Loja.Nome.Contains(term));
        }

        var veiculos = await query
            .OrderBy(x => x.Vendido)
            .ThenByDescending(x => x.DataCadastro)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Titulo,
                x.Modelo,
                x.Versao,
                x.Placa,
                x.DataCadastro,
                x.DataVenda,
                x.Seminovo,
                x.Vendido,
                MarcaNome = x.Marca.Nome,
                LojaNome = x.Loja.Nome
            })
            .ToListAsync(ct);

        Veiculos = veiculos
            .Select(x => new VehicleSaleListItem(
                x.Id,
                BuildTitle(x.Titulo, x.Modelo, x.Versao),
                $"{x.MarcaNome} - {x.LojaNome}",
                string.IsNullOrWhiteSpace(x.Placa) ? "-" : x.Placa,
                x.DataCadastro.ToString("dd/MM/yyyy", BrCulture),
                x.DataVenda.HasValue ? x.DataVenda.Value.ToString("dd/MM/yyyy", BrCulture) : "-",
                x.Seminovo ? "Seminovo" : "0km",
                x.Vendido))
            .ToList();

        Vendedores = await db.Vendedores
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem(x.Nome, x.Id.ToString(CultureInfo.InvariantCulture)))
            .ToListAsync(ct);
    }

    private static string BuildTitle(string titulo, string modelo, string? versao)
    {
        var nome = string.IsNullOrWhiteSpace(modelo) ? titulo : $"{titulo} {modelo}";
        return string.IsNullOrWhiteSpace(versao) ? nome.Trim() : $"{nome} {versao}".Trim();
    }

    public sealed class SellVehicleInput
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int VeiculoId { get; set; }

        [Required(ErrorMessage = "Selecione o vendedor responsavel.")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecione o vendedor responsavel.")]
        public int VendedorId { get; set; }

        [Required(ErrorMessage = "Informe a data da venda.")]
        [DataType(DataType.Date)]
        public DateTime DataVenda { get; set; } = DateTime.Today;
    }

    public sealed record VehicleSaleListItem(
        int Id,
        string Nome,
        string Descricao,
        string Placa,
        string DataCadastro,
        string DataVenda,
        string Condicao,
        bool Vendido);
}
