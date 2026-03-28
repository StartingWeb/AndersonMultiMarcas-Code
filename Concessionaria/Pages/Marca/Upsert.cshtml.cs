using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concessionaria.Pages.Marca
{
    public class UpsertModel : PageModel
    {
        private readonly IMarcaService _marcaService;
        private readonly IWebHostEnvironment _environment;

        public UpsertModel(IMarcaService marcaService, IWebHostEnvironment environment)
        {
            _marcaService = marcaService;
            _environment = environment;
        }

        [BindProperty]
        public Domain.Marca Marca { get; set; } = new();

        [BindProperty]
        public IFormFile? LogoArquivo { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                Marca = new Domain.Marca
                {
                    Ativo = true
                };

                return Page();
            }

            var result = await _marcaService.ObterPorIdAsync(id.Value);

            if (result.Status != PackageStatus.Success || result.Data == null)
            {
                TempData["Error"] = result.UserMessage ?? "Marca não encontrada.";
                return RedirectToPage("./Index");
            }

            Marca = result.Data;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Marca.LogoUrl");
            if (!ModelState.IsValid)
                return Page();

            if (LogoArquivo != null)
            {
                var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extensao = Path.GetExtension(LogoArquivo.FileName).ToLowerInvariant();

                if (!extensoesPermitidas.Contains(extensao))
                {
                    TempData["Warning"] = "Formato de imagem inválido. Use JPG, JPEG, PNG ou WEBP.";
                    return Page();
                }

                if (LogoArquivo.Length > 5 * 1024 * 1024)
                {
                    TempData["Warning"] = "A imagem deve ter no máximo 5 MB.";
                    return Page();
                }

                var pastaUploads = Path.Combine(_environment.WebRootPath, "uploads", "marcas");

                if (!Directory.Exists(pastaUploads))
                    Directory.CreateDirectory(pastaUploads);

                var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
                var caminhoArquivo = Path.Combine(pastaUploads, nomeArquivo);

                using (var stream = new FileStream(caminhoArquivo, FileMode.Create))
                {
                    await LogoArquivo.CopyToAsync(stream);
                }

                Marca.LogoUrl = $"/uploads/marcas/{nomeArquivo}";
            }

            if (Marca.Id > 0)
            {
                var marcaAtual = await _marcaService.ObterPorIdAsync(Marca.Id);
                if (marcaAtual.Status != PackageStatus.Success || marcaAtual.Data == null)
                {
                    TempData["Warning"] = "Marca não encontrada.";
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(Marca.LogoUrl))
                    Marca.LogoUrl = marcaAtual.Data.LogoUrl;

                var result = await _marcaService.EditarAsync(Marca);

                if (result.Status != PackageStatus.Success)
                {
                    TempData["Error"] = result.UserMessage ?? "Não foi possível atualizar a marca.";
                    return Page();
                }

                TempData["Success"] = result.UserMessage ?? "Marca atualizada com sucesso.";
                return RedirectToPage("./Index");
            }
            else
            {
                var result = await _marcaService.CriarAsync(Marca);

                if (result.Status != PackageStatus.Success)
                {
                    TempData["Error"] = result.UserMessage ?? "Não foi possível cadastrar a marca.";
                    return Page();
                }

                TempData["Success"] = result.UserMessage ?? "Marca cadastrada com sucesso.";
                return RedirectToPage("./Index");
            }
        }
    }
}