using Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Project.Pages.ViewModels;
using Project.Shared;

namespace Project.Pages;

public class ContatoModel(ApplicationDbContext db) : PageModel
{
    public IReadOnlyList<ContactStoreViewModel> Lojas { get; private set; } = [];
    public IReadOnlyList<HomeSellerViewModel> Vendedores { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var lojas = await db.Lojas
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new ContactStoreViewModel
            {
                Nome = x.Nome,
                EnderecoCompleto = BuildEnderecoCompleto(
                    x.Endereco.Logradouro,
                    x.Endereco.Numero,
                    x.Endereco.Complemento,
                    x.Endereco.Bairro,
                    x.Endereco.Cidade,
                    x.Endereco.Uf.ToString(),
                    x.Endereco.Cep),
                Telefone = x.Telefone.Valor,
                TelefoneExibicao = FormatPhone(x.Telefone.Valor),
                Email = x.Email.Valor,
                MapsQuery = Uri.EscapeDataString(BuildEnderecoCompleto(
                    x.Endereco.Logradouro,
                    x.Endereco.Numero,
                    x.Endereco.Complemento,
                    x.Endereco.Bairro,
                    x.Endereco.Cidade,
                    x.Endereco.Uf.ToString(),
                    x.Endereco.Cep)),
                Vendedores = x.Vendedores
                    .Where(v => v.Nome != null && v.Nome != "")
                    .OrderBy(v => v.Nome)
                    .Select(v => new ContactSellerViewModel
                    {
                        Nome = v.Nome,
                        Telefone = v.Whatsapp != null ? v.Whatsapp.Value.Valor : (v.Telefone != null ? v.Telefone.Value.Valor : string.Empty),
                        TelefoneExibicao = v.Whatsapp != null
                            ? FormatPhone(v.Whatsapp.Value.Valor)
                            : (v.Telefone != null ? FormatPhone(v.Telefone.Value.Valor) : string.Empty),
                        Cargo = v.Cargo,
                        FotoUrl = SellerImageHelper.Normalize(v.FotoUrl)
                    })
                    .ToList()
            })
            .ToListAsync();

        Lojas = lojas
            .Select(loja => new ContactStoreViewModel
            {
                Nome = loja.Nome,
                EnderecoCompleto = loja.EnderecoCompleto,
                Telefone = loja.Telefone,
                TelefoneExibicao = loja.TelefoneExibicao,
                Email = loja.Email,
                MapsQuery = loja.MapsQuery,
                Vendedores = loja.Vendedores
                    .Where(v => !string.IsNullOrWhiteSpace(v.Telefone))
                    .ToList()
            })
            .ToList();

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

        Vendedores = vendedores
            .Where(x => !string.IsNullOrWhiteSpace(x.Telefone))
            .ToList();

        ViewData["SeoTitle"] = "Contato e lojas em Taquaritinga/SP | Anderson Multimarcas";
        ViewData["MetaDescription"] = "Veja contatos, endereco, telefone, e-mail, mapa e vendedores das lojas Anderson Multimarcas em Taquaritinga/SP.";
        ViewData["CanonicalUrl"] = $"{baseUrl}/Contato";
        ViewData["BreadcrumbSchema"] = SeoJsonLd.Breadcrumb(baseUrl, ("Inicio", "/"), ("Contato", "/Contato"));
        ViewData["FaqSchema"] = SeoJsonLd.Faq(
            ("Como falar com a Anderson Multimarcas?", "Voce pode ligar para a loja, chamar um vendedor no WhatsApp ou abrir a rota para atendimento presencial."),
            ("Posso ver os carros presencialmente?", "Sim. A pagina de contato mostra as lojas, mapas e vendedores por unidade para facilitar a visita."));
    }

    private static string BuildEnderecoCompleto(
        string logradouro,
        string numero,
        string? complemento,
        string bairro,
        string cidade,
        string uf,
        string cep)
    {
        var partes = new[]
        {
            logradouro,
            numero,
            complemento,
            bairro,
            $"{cidade} - {uf}",
            cep
        };

        return string.Join(", ", partes.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string FormatPhone(string phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        return digits.Length switch
        {
            10 => $"({digits[..2]}) {digits[2..6]}-{digits[6..]}",
            11 => $"({digits[..2]}) {digits[2..7]}-{digits[7..]}",
            12 when digits.StartsWith("55") => $"({digits[2..4]}) {digits[4..8]}-{digits[8..]}",
            13 when digits.StartsWith("55") => $"({digits[2..4]}) {digits[4..9]}-{digits[9..]}",
            _ => digits
        };
    }
}
