using Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Project.Pages.Admin.Cadastros.Vendedores;

[Authorize]
public sealed class IndexModel(ApplicationDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Filtro { get; set; }

    public IReadOnlyList<VendedorListItem> Vendedores { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Vendedores";
        ViewData["Robots"] = "noindex,nofollow";

        var query = db.Vendedores.AsNoTracking().Include(x => x.Loja).Where(x => x.Ativo);

        if (!string.IsNullOrWhiteSpace(Filtro))
        {
            var term = Filtro.Trim();
            query = query.Where(x =>
                x.Nome.Contains(term) ||
                (x.Cargo != null && x.Cargo.Contains(term)) ||
                x.Loja.Nome.Contains(term));
        }

        Vendedores = await query
            .OrderBy(x => x.Nome)
            .Select(x => new VendedorListItem(
                x.Id,
                x.Nome,
                x.Cargo ?? "-",
                x.Loja.Nome,
                x.Email.HasValue ? x.Email.Value.Valor : "-",
                x.Telefone.HasValue ? x.Telefone.Value.Valor : "-",
                x.Whatsapp.HasValue ? x.Whatsapp.Value.Valor : "-"))
            .ToListAsync(ct);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, string? filtro, CancellationToken ct)
    {
        var vendedor = await db.Vendedores.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (vendedor is null)
        {
            TempData["ErrorMessage"] = "Vendedor nao encontrado.";
            return RedirectToPage(new { Filtro = filtro });
        }

        vendedor.Desativar();
        await db.SaveChangesAsync(ct);

        TempData["SuccessMessage"] = "Vendedor excluido com sucesso.";
        return RedirectToPage(new { Filtro = filtro });
    }

    public sealed record VendedorListItem(int Id, string Nome, string Cargo, string Loja, string Email, string Telefone, string Whatsapp);
}
