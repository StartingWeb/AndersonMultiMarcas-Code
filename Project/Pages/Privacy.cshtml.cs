using Microsoft.AspNetCore.Mvc.RazorPages;
using Project.Shared;

namespace Project.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly ILogger<PrivacyModel> _logger;

        public PrivacyModel(ILogger<PrivacyModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            ViewData["SeoTitle"] = "Politica de Privacidade | Anderson Multi Marcas";
            ViewData["MetaDescription"] = "Saiba como a Anderson Multi Marcas trata dados pessoais, privacidade e seguranca das informacoes no site.";
            ViewData["MetaKeywords"] = "politica de privacidade, protecao de dados, lgpd";
            ViewData["CanonicalUrl"] = $"{baseUrl}/Privacy";
            ViewData["BreadcrumbSchema"] = SeoJsonLd.Breadcrumb(baseUrl, ("Inicio", "/"), ("Privacidade", "/Privacy"));
        }
    }
}
