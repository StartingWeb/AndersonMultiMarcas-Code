using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Pages.ViewModels;
using Project.Shared;

namespace Project.ViewComponents;

public sealed class SellerContactModalViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var vendedores = await db.Vendedores
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new HomeSellerViewModel
            {
                Nome = x.Nome,
                Telefone = x.Whatsapp.HasValue ? x.Whatsapp.Value.Valor : (x.Telefone.HasValue ? x.Telefone.Value.Valor : string.Empty),
                FotoUrl = SellerImageHelper.Normalize(x.FotoUrl)
            })
            .ToListAsync();

        var model = vendedores
            .Where(x => !string.IsNullOrWhiteSpace(x.Telefone))
            .ToList();

        return View(model);
    }
}
