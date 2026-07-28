using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Project.Infrastructure.Storage;

namespace Project.ViewComponents;

public sealed class SellerContactModalViewComponent(
    ApplicationDbContext db,
    IStorageImageResolver imageResolver,
    IMemoryCache cache) : ViewComponent
{
    private const string CacheKey = "seller-contact-modal:v1";

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await LoadModelAsync();
        });

        return View(model ?? []);
    }

    private async Task<IReadOnlyList<SellerContactModalStoreGroupViewModel>> LoadModelAsync()
    {
        var grupos = await db.Vendedores
            .AsNoTracking()
            .Where(x => x.Ativo)
            .Select(x => new
            {
                LojaNome = x.Loja.Nome,
                x.Nome,
                Telefone = x.Whatsapp.HasValue ? x.Whatsapp.Value.Valor : (x.Telefone.HasValue ? x.Telefone.Value.Valor : string.Empty),
                x.FotoUrl
            })
            .ToListAsync();

        var vendedores = new List<(string LojaNome, SellerContactModalSellerViewModel Vendedor)>();
        foreach (var grupo in grupos.Where(x => !string.IsNullOrWhiteSpace(x.Telefone)))
        {
            vendedores.Add((
                grupo.LojaNome,
                new SellerContactModalSellerViewModel(
                    grupo.Nome,
                    grupo.Telefone,
                    await imageResolver.ResolveSellerPhotoAsync(grupo.FotoUrl, HttpContext.RequestAborted))));
        }

        var model = vendedores
            .GroupBy(x => string.IsNullOrWhiteSpace(x.LojaNome) ? "Sem loja vinculada" : x.LojaNome)
            .OrderBy(x => x.Key)
            .Select(x => new SellerContactModalStoreGroupViewModel(
                x.Key,
                x.Select(v => v.Vendedor).OrderBy(v => v.Nome).ToList()))
            .Where(x => x.Vendedores.Count > 0)
            .ToList();

        return model;
    }
}

public sealed record SellerContactModalStoreGroupViewModel(
    string LojaNome,
    IReadOnlyList<SellerContactModalSellerViewModel> Vendedores);

public sealed record SellerContactModalSellerViewModel(
    string Nome,
    string Telefone,
    string? FotoUrl);
