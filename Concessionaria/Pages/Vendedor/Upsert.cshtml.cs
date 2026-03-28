using Core.Enums;
using Core.Interfaces;
using Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Concessionaria.Pages.Vendedor
{
    public class UpsertModel : PageModel
    {
        private readonly IVendedorService _vendedorService;
        private readonly ILojaService _lojaService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UpsertModel(
            IVendedorService vendedorService,
            ILojaService lojaService,
            IWebHostEnvironment webHostEnvironment)
        {
            _vendedorService = vendedorService;
            _lojaService = lojaService;
            _webHostEnvironment = webHostEnvironment;
        }

        [BindProperty]
        public Domain.Vendedor Vendedor { get; set; } = new();

        [BindProperty]
        public IFormFile? FotoArquivo { get; set; }

        public List<SelectListItem> Lojas { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            await CarregarLojasAsync();

            if (id == null || id <= 0)
            {
                Vendedor = new Domain.Vendedor
                {
                    Ativo = true
                };

                return Page();
            }

            var result = await _vendedorService.ObterPorIdAsync(id.Value);

            if (result.Status != PackageStatus.Success || result.Data == null)
            {
                TempData["Error"] = result.UserMessage ?? "Vendedor não encontrado.";
                return RedirectToPage("./Index");
            }

            Vendedor = result.Data;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            //if (!ModelState.IsValid)
            //{
            //    await CarregarLojasAsync();
            //    TempData["Warning"] = "Revise as informações obrigatórias!";
            //    return Page();
            //}

            var lojaIdPostado = Request.Form["Vendedor.LojaId"].ToString();

            if (!ModelState.IsValid)
            {
                var erros = string.Join(" || ",
                    ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}")
                );

                TempData["Error"] = $"POST LojaId={lojaIdPostado} | Model LojaId={Vendedor?.LojaId} | Erros={erros}";
                return Page();
            }


            var fotoAnterior = Vendedor.FotoUrl;

            if (FotoArquivo != null && FotoArquivo.Length > 0)
            {
                var validacaoArquivo = ValidarArquivoImagem(FotoArquivo);
                if (!string.IsNullOrWhiteSpace(validacaoArquivo))
                {
                    await CarregarLojasAsync();
                    TempData["Error"] = validacaoArquivo;
                    return Page();
                }

                var uploadResult = await SalvarArquivoAsync(FotoArquivo);
                if (!uploadResult.Sucesso)
                {
                    await CarregarLojasAsync();
                    TempData["Error"] = uploadResult.MensagemErro ?? "Não foi possível salvar a foto.";
                    return Page();
                }

                Vendedor.FotoUrl = uploadResult.CaminhoRelativo;
            }

            if (Vendedor.Id > 0)
            {
                var result = await _vendedorService.EditarAsync(Vendedor);

                if (result.Status != PackageStatus.Success)
                {
                    await CarregarLojasAsync();
                    TempData["Error"] = result.UserMessage ?? "Não foi possível atualizar o vendedor.";
                    return Page();
                }

                if (FotoArquivo != null && !string.IsNullOrWhiteSpace(fotoAnterior) && fotoAnterior != Vendedor.FotoUrl)
                {
                    RemoverArquivoAntigo(fotoAnterior);
                }

                TempData["Success"] = result.UserMessage;
                return RedirectToPage("./Index");
            }
            else
            {
                var result = await _vendedorService.CriarAsync(Vendedor);

                if (result.Status != PackageStatus.Success)
                {
                    if (FotoArquivo != null && !string.IsNullOrWhiteSpace(Vendedor.FotoUrl))
                    {
                        RemoverArquivoAntigo(Vendedor.FotoUrl);
                    }

                    await CarregarLojasAsync();
                    TempData["Error"] = result.UserMessage ?? "Não foi possível cadastrar o vendedor.";
                    return Page();
                }

                TempData["Success"] = result.UserMessage;
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostDeletePhotoAsync()
        {
            await CarregarLojasAsync();

            if (Vendedor.Id <= 0)
            {
                TempData["Error"] = "Vendedor não encontrado para remover a foto.";
                return RedirectToPage("./Index");
            }

            var result = await _vendedorService.ObterPorIdAsync(Vendedor.Id);
            if (result.Status != PackageStatus.Success || result.Data == null)
            {
                TempData["Error"] = result.UserMessage ?? "Vendedor não encontrado.";
                return RedirectToPage("./Index");
            }

            var vendedor = result.Data;
            var fotoAnterior = vendedor.FotoUrl;

            if (string.IsNullOrWhiteSpace(fotoAnterior))
            {
                TempData["Warning"] = "Esse vendedor não possui foto cadastrada.";
                return RedirectToPage("./Upsert", new { id = vendedor.Id });
            }

            vendedor.FotoUrl = null;

            var editarResult = await _vendedorService.EditarAsync(vendedor);
            if (editarResult.Status != PackageStatus.Success)
            {
                TempData["Error"] = editarResult.UserMessage ?? "Não foi possível excluir a foto do vendedor.";
                return RedirectToPage("./Upsert", new { id = vendedor.Id });
            }

            RemoverArquivoAntigo(fotoAnterior);

            TempData["Success"] = "Foto do vendedor excluída com sucesso.";
            return RedirectToPage("./Upsert", new { id = vendedor.Id });
        }

        private async Task CarregarLojasAsync()
        {
            var result = await _lojaService.ListarAsync();

            Lojas = result.Data?
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Nome
                })
                .ToList() ?? new List<SelectListItem>();
        }

        private string? ValidarArquivoImagem(IFormFile arquivo)
        {
            const long tamanhoMaximo = 5 * 1024 * 1024; // 5 MB

            var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extensao) || !extensoesPermitidas.Contains(extensao))
                return "Formato de imagem inválido. Envie um arquivo JPG, JPEG, PNG ou WEBP.";

            if (arquivo.Length > tamanhoMaximo)
                return "A imagem deve ter no máximo 5 MB.";

            return null;
        }

        private async Task<(bool Sucesso, string? CaminhoRelativo, string? MensagemErro)> SalvarArquivoAsync(IFormFile arquivo)
        {
            try
            {
                var pastaUploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "vendedores");

                if (!Directory.Exists(pastaUploads))
                    Directory.CreateDirectory(pastaUploads);

                var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
                var nomeArquivo = $"{Guid.NewGuid():N}{extensao}";
                var caminhoCompleto = Path.Combine(pastaUploads, nomeArquivo);

                using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                {
                    await arquivo.CopyToAsync(stream);
                }

                var caminhoRelativo = $"/uploads/vendedores/{nomeArquivo}";
                return (true, caminhoRelativo, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        private void RemoverArquivoAntigo(string? caminhoRelativo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(caminhoRelativo))
                    return;

                var caminhoNormalizado = caminhoRelativo.Replace("/", Path.DirectorySeparatorChar.ToString())
                                                       .TrimStart(Path.DirectorySeparatorChar);

                var caminhoCompleto = Path.Combine(_webHostEnvironment.WebRootPath, caminhoNormalizado);

                if (System.IO.File.Exists(caminhoCompleto))
                {
                    System.IO.File.Delete(caminhoCompleto);
                }
            }
            catch
            {
                // opcionalmente registrar log aqui
            }
        }
    }
}
