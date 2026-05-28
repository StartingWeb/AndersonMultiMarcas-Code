using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Project.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            ViewData["SeoTitle"] = "Anderson Multi Marcas | Carros Seminovos em Sao Paulo";
            ViewData["MetaDescription"] = "Loja de carros seminovos em Sao Paulo com procedencia, revisao e atendimento especializado para compra segura.";
            ViewData["MetaKeywords"] = "carros seminovos sao paulo, revenda de carros, loja de carros";
            ViewData["CanonicalUrl"] = $"{baseUrl}/";
            ViewData["BreadcrumbSchema"] = JsonSerializer.Serialize(new
            {
                @context = "https://schema.org",
                @type = "BreadcrumbList",
                itemListElement = new object[]
                {
                    new
                    {
                        @type = "ListItem",
                        position = 1,
                        name = "Inicio",
                        item = $"{baseUrl}/"
                    }
                }
            });
            ViewData["FaqSchema"] = JsonSerializer.Serialize(new
            {
                @context = "https://schema.org",
                @type = "FAQPage",
                mainEntity = new object[]
                {
                    new
                    {
                        @type = "Question",
                        name = "Quais tipos de veiculos a Anderson Multi Marcas oferece?",
                        acceptedAnswer = new
                        {
                            @type = "Answer",
                            text = "Oferecemos carros seminovos com analise de procedencia, revisao e suporte na escolha do modelo."
                        }
                    },
                    new
                    {
                        @type = "Question",
                        name = "A loja atende clientes de qual regiao?",
                        acceptedAnswer = new
                        {
                            @type = "Answer",
                            text = "Atendemos Sao Paulo e regiao, com foco em atendimento consultivo e seguranca na compra."
                        }
                    }
                }
            });
        }
    }
}
