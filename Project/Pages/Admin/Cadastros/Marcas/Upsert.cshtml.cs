using System.ComponentModel.DataAnnotations;
using Data;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Project.Pages.Admin.Cadastros.Marcas;

[Authorize]
public sealed class UpsertModel(ApplicationDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    [BindProperty]
    public MarcaInputModel Marca { get; set; } = new();

    public string PageTitle { get; private set; } = "Nova marca";
    public string PageSubtitle { get; private set; } = "Preencha os dados para cadastrar uma nova marca";
    public string SubmitLabel { get; private set; } = "Cadastrar marca";

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!Id.HasValue)
        {
            ViewData["Title"] = PageTitle;
            return Page();
        }

        var marca = await db.Marcas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Id.Value, ct);
        if (marca is null)
        {
            TempData["ErrorMessage"] = "Marca nao encontrada.";
            return RedirectToPage("/Admin/Cadastros/Marcas/Index");
        }

        Marca = new MarcaInputModel { Id = marca.Id, Nome = marca.Nome, LogoUrl = marca.LogoUrl };
        ApplyEditTexts();
        ViewData["Title"] = PageTitle;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            if (Marca.Id > 0) ApplyEditTexts();
            return Page();
        }

        try
        {
            if (Marca.Id > 0)
            {
                var marca = await db.Marcas.FirstOrDefaultAsync(x => x.Id == Marca.Id, ct);
                if (marca is null)
                {
                    TempData["ErrorMessage"] = "Marca nao encontrada.";
                    return RedirectToPage("/Admin/Cadastros/Marcas/Index");
                }

                marca.Update(Marca.Nome, Marca.LogoUrl);
                TempData["SuccessMessage"] = "Marca atualizada com sucesso.";
            }
            else
            {
                db.Marcas.Add(new Marca(Marca.Nome, Marca.LogoUrl));
                TempData["SuccessMessage"] = "Marca cadastrada com sucesso.";
            }

            await db.SaveChangesAsync(ct);
            return RedirectToPage("/Admin/Cadastros/Marcas/Index");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Nao foi possivel salvar. Verifique se ja existe uma marca com este nome.");
            if (Marca.Id > 0) ApplyEditTexts();
            return Page();
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            if (Marca.Id > 0) ApplyEditTexts();
            return Page();
        }
    }

    private void ApplyEditTexts()
    {
        PageTitle = "Editar marca";
        PageSubtitle = "Atualize os dados da marca cadastrada";
        SubmitLabel = "Salvar alteracoes";
    }

    public sealed class MarcaInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome da marca.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no maximo 100 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(400, ErrorMessage = "A URL deve ter no maximo 400 caracteres.")]
        [Display(Name = "Logo")]
        public string? LogoUrl { get; set; }
    }
}
