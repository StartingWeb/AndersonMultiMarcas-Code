using System.ComponentModel.DataAnnotations;
using Data;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Project.Pages.Admin.Cadastros.Vendedores;

[Authorize]
public sealed class UpsertModel(ApplicationDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    [BindProperty]
    public VendedorInputModel Vendedor { get; set; } = new();

    public IReadOnlyList<SelectListItem> Lojas { get; private set; } = [];
    public string PageTitle { get; private set; } = "Novo vendedor";
    public string PageSubtitle { get; private set; } = "Preencha os dados para cadastrar um novo vendedor";
    public string SubmitLabel { get; private set; } = "Cadastrar vendedor";

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await LoadLojasAsync(ct);

        if (!Id.HasValue)
        {
            ViewData["Title"] = PageTitle;
            return Page();
        }

        var vendedor = await db.Vendedores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Id.Value, ct);
        if (vendedor is null)
        {
            TempData["ErrorMessage"] = "Vendedor nao encontrado.";
            return RedirectToPage("/Admin/Cadastros/Vendedores/Index");
        }

        Vendedor = new VendedorInputModel
        {
            Id = vendedor.Id,
            LojaId = vendedor.LojaId,
            Nome = vendedor.Nome,
            Email = vendedor.Email?.Valor,
            Telefone = vendedor.Telefone?.Valor,
            Whatsapp = vendedor.Whatsapp?.Valor,
            Cpf = vendedor.Cpf?.Valor,
            FotoUrl = vendedor.FotoUrl,
            Cargo = vendedor.Cargo
        };

        ApplyEditTexts();
        ViewData["Title"] = PageTitle;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadLojasAsync(ct);

        if (!ModelState.IsValid)
        {
            if (Vendedor.Id > 0) ApplyEditTexts();
            return Page();
        }

        if (!await db.Lojas.AnyAsync(x => x.Id == Vendedor.LojaId && x.Ativo, ct))
        {
            ModelState.AddModelError("Vendedor.LojaId", "Informe uma loja valida.");
            if (Vendedor.Id > 0) ApplyEditTexts();
            return Page();
        }

        try
        {
            if (Vendedor.Id > 0)
            {
                var vendedor = await db.Vendedores.FirstOrDefaultAsync(x => x.Id == Vendedor.Id, ct);
                if (vendedor is null)
                {
                    TempData["ErrorMessage"] = "Vendedor nao encontrado.";
                    return RedirectToPage("/Admin/Cadastros/Vendedores/Index");
                }

                vendedor.Update(
                    Vendedor.Nome,
                    BuildEmail(Vendedor.Email),
                    BuildTelefone(Vendedor.Telefone),
                    BuildTelefone(Vendedor.Whatsapp),
                    BuildDocumento(Vendedor.Cpf),
                    Vendedor.FotoUrl,
                    Vendedor.Cargo);
                db.Entry(vendedor).Property(x => x.LojaId).CurrentValue = Vendedor.LojaId;
                TempData["SuccessMessage"] = "Vendedor atualizado com sucesso.";
            }
            else
            {
                var vendedor = new Vendedor(Vendedor.LojaId, Vendedor.Nome);
                vendedor.Update(
                    Vendedor.Nome,
                    BuildEmail(Vendedor.Email),
                    BuildTelefone(Vendedor.Telefone),
                    BuildTelefone(Vendedor.Whatsapp),
                    BuildDocumento(Vendedor.Cpf),
                    Vendedor.FotoUrl,
                    Vendedor.Cargo);
                db.Vendedores.Add(vendedor);
                TempData["SuccessMessage"] = "Vendedor cadastrado com sucesso.";
            }

            await db.SaveChangesAsync(ct);
            return RedirectToPage("/Admin/Cadastros/Vendedores/Index");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            if (Vendedor.Id > 0) ApplyEditTexts();
            return Page();
        }
    }

    private async Task LoadLojasAsync(CancellationToken ct)
    {
        Lojas = await db.Lojas
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem(x.Nome, x.Id.ToString()))
            .ToListAsync(ct);
    }

    private void ApplyEditTexts()
    {
        PageTitle = "Editar vendedor";
        PageSubtitle = "Atualize os dados do vendedor cadastrado";
        SubmitLabel = "Salvar alteracoes";
    }

    private static Email? BuildEmail(string? value) => string.IsNullOrWhiteSpace(value) ? null : new Email(value);
    private static Telefone? BuildTelefone(string? value) => string.IsNullOrWhiteSpace(value) ? null : new Telefone(value);
    private static Documento? BuildDocumento(string? value) => string.IsNullOrWhiteSpace(value) ? null : new Documento(value);

    public sealed class VendedorInputModel
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Informe a loja.")]
        [Display(Name = "Loja")]
        public int LojaId { get; set; }

        [Required(ErrorMessage = "Informe o nome do vendedor.")]
        [StringLength(150)]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
        [StringLength(180)]
        [Display(Name = "E-mail")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [StringLength(20)]
        [Display(Name = "WhatsApp")]
        public string? Whatsapp { get; set; }

        [StringLength(14)]
        [Display(Name = "CPF")]
        public string? Cpf { get; set; }

        [StringLength(400)]
        [Display(Name = "Foto")]
        public string? FotoUrl { get; set; }

        [StringLength(120)]
        [Display(Name = "Cargo")]
        public string? Cargo { get; set; }
    }
}
