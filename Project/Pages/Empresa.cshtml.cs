using Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Project.Pages.ViewModels;
using Project.Shared;

namespace Project.Pages;

public class EmpresaModel(ApplicationDbContext db) : PageModel
{
    public int TotalVeiculos { get; private set; }
    public IReadOnlyList<HomeStoreViewModel> Lojas { get; private set; } = [];
    public IReadOnlyList<HomeSellerViewModel> Vendedores { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        TotalVeiculos = await db.Veiculos
            .AsNoTracking()
            .CountAsync(x => x.Ativo && !x.Vendido);

        Lojas = await db.Lojas
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new HomeStoreViewModel
            {
                Nome = x.Nome,
                EnderecoCompleto = $"{x.Endereco.Logradouro}, {x.Endereco.Numero}, {x.Endereco.Bairro}, {x.Endereco.Cidade} - {x.Endereco.Uf}, {x.Endereco.Cep}",
                MapsQuery = Uri.EscapeDataString($"{x.Endereco.Logradouro}, {x.Endereco.Numero}, {x.Endereco.Bairro}, {x.Endereco.Cidade} - {x.Endereco.Uf}, {x.Endereco.Cep}")
            })
            .Take(3)
            .ToListAsync();

        Vendedores = await db.Vendedores
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new HomeSellerViewModel
            {
                Nome = x.Nome,
                Telefone = x.Whatsapp.HasValue ? x.Whatsapp.Value.Valor : (x.Telefone.HasValue ? x.Telefone.Value.Valor : string.Empty),
                FotoUrl = SellerImageHelper.Normalize(x.FotoUrl)
            })
            .Take(12)
            .ToListAsync();

        ViewData["SeoTitle"] = "Quem somos | Anderson Multimarcas em Taquaritinga/SP";
        ViewData["MetaDescription"] = "Conheça a história da Anderson Multimarcas, nossas lojas em Taquaritinga/SP e o atendimento pensado para gerar confiança em cada negócio.";
        ViewData["CanonicalUrl"] = $"{baseUrl}/Empresa";
        ViewData["BreadcrumbSchema"] = SeoJsonLd.Breadcrumb(baseUrl, ("Início", "/"), ("Quem somos", "/Empresa"));
        ViewData["FaqSchema"] = SeoJsonLd.Faq(
            ("Onde fica a Anderson Multimarcas?", "A Anderson Multimarcas atende em Taquaritinga/SP e região, com unidades presenciais e vendedores especializados."),
            ("A loja trabalha com quais tipos de veículo?", "O estoque inclui carros seminovos, opções 0 km, modelos eletrificados e motos elétricas conforme disponibilidade."));
    }
}
