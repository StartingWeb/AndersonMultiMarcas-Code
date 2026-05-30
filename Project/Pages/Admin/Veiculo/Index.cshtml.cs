using System.Globalization;
using Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Project.Pages.Admin.Veiculo;

[Authorize]
public sealed class IndexModel(ApplicationDbContext db) : PageModel
{
    private static readonly CultureInfo BrCulture = new("pt-BR");

    [BindProperty(SupportsGet = true)]
    public int? Codigo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Filtro { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Ordem { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? SomenteSeminovo { get; set; }

    public IReadOnlyList<VehicleAdminListItem> Veiculos { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Veiculos";
        ViewData["Robots"] = "noindex,nofollow";

        var query = db.Veiculos
            .AsNoTracking()
            .Where(x => x.Ativo)
            .AsQueryable();

        if (Codigo.HasValue && Codigo.Value > 0)
        {
            query = query.Where(x => x.Id == Codigo.Value || x.IdLegado == Codigo.Value);
        }

        if (!string.IsNullOrWhiteSpace(Filtro))
        {
            var term = Filtro.Trim();
            query = query.Where(x =>
                x.Titulo.Contains(term) ||
                x.Modelo.Contains(term) ||
                (x.Versao != null && x.Versao.Contains(term)) ||
                (x.Placa != null && x.Placa.Contains(term)) ||
                x.Marca.Nome.Contains(term));
        }

        if (SomenteSeminovo.HasValue)
        {
            query = query.Where(x => x.Seminovo == SomenteSeminovo.Value);
        }

        query = (Ordem ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "antigos" => query.OrderBy(x => x.DataCadastro).ThenBy(x => x.Id),
            "titulo" => query.OrderBy(x => x.Titulo).ThenBy(x => x.Modelo).ThenBy(x => x.Versao),
            "titulo_desc" => query.OrderByDescending(x => x.Titulo).ThenByDescending(x => x.Modelo).ThenByDescending(x => x.Versao),
            "preco_asc" => query.OrderBy(x => x.PrecoVenda.Valor).ThenBy(x => x.Titulo),
            "preco_desc" => query.OrderByDescending(x => x.PrecoVenda.Valor).ThenBy(x => x.Titulo),
            _ => query.OrderByDescending(x => x.DataCadastro).ThenByDescending(x => x.Id)
        };

        var veiculos = await query
            .Select(x => new
            {
                x.Id,
                x.Titulo,
                x.Modelo,
                x.Versao,
                x.Placa,
                x.DataCadastro,
                x.Seminovo,
                x.PrecoVenda,
                MarcaNome = x.Marca.Nome
            })
            .ToListAsync(ct);

        Veiculos = veiculos
            .Select(x => new VehicleAdminListItem(
                x.Id,
                BuildTitle(x.Titulo, x.Modelo),
                BuildSubtitle(x.MarcaNome, x.Modelo, x.Versao),
                string.IsNullOrWhiteSpace(x.Placa) ? "-" : x.Placa,
                x.DataCadastro.ToString("MM/yyyy", BrCulture),
                x.Seminovo ? "Seminovo" : "0km",
                x.PrecoVenda.Valor > 0 ? x.PrecoVenda.Valor.ToString("C", BrCulture) : "Não informado"))
            .ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        int id,
        string? filtro,
        int? codigo,
        string? ordem,
        bool? somenteSeminovo,
        CancellationToken ct)
    {
        var veiculo = await db.Veiculos.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (veiculo is null)
        {
            TempData["ErrorMessage"] = "Veiculo nao encontrado.";
            return RedirectToPage(new { Filtro = filtro, Codigo = codigo, Ordem = ordem, SomenteSeminovo = somenteSeminovo });
        }

        veiculo.Desativar();
        await db.SaveChangesAsync(ct);

        TempData["SuccessMessage"] = "Veiculo excluido com sucesso.";
        return RedirectToPage(new { Filtro = filtro, Codigo = codigo, Ordem = ordem, SomenteSeminovo = somenteSeminovo });
    }

    private static string BuildTitle(string titulo, string modelo)
        => string.IsNullOrWhiteSpace(modelo) ? titulo.Trim() : $"{titulo} {modelo}".Trim();

    private static string BuildSubtitle(string marca, string modelo, string? versao)
        => string.IsNullOrWhiteSpace(versao)
            ? $"{marca} • {modelo}"
            : $"{marca} • {modelo} • {versao}";

    public sealed record VehicleAdminListItem(
        int Id,
        string Titulo,
        string Subtitulo,
        string Placa,
        string MesAno,
        string Condicao,
        string Preco);
}
