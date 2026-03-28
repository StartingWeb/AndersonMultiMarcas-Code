using Core.Dtos;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concessionaria.Pages.Loja;

public class UpsertModel : PageModel
{
    private readonly ILojaService _lojaService;

    public UpsertModel(ILojaService lojaService)
    {
        _lojaService = lojaService;
    }

    [BindProperty(SupportsGet = false)]
    public LojaDto Loja { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (!id.HasValue || id.Value <= 0)
        {
            Loja = new LojaDto
            {
                Ativo = true,
                DataCadastro = DateTime.Now
            };

            return Page();
        }

        var result = await _lojaService.ObterPorIdAsync(id.Value);

        if (result.Status != PackageStatus.Success || result.Data is null)
        {
            TempData["Warning"] = string.IsNullOrWhiteSpace(result.UserMessage)
                ? "Loja não encontrada."
                : result.UserMessage;
            return RedirectToPage("./Index");
        }

        Loja = result.Data;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ObterMensagemValidacao();
            return Page();
        }

        if (Loja.Id <= 0)
        {
            var result = await _lojaService.CriarAsync(Loja);

            if (result.Status != PackageStatus.Success)
            {
                TempData["Error"] = string.IsNullOrWhiteSpace(result.UserMessage)
                    ? "Não foi possível cadastrar a loja."
                    : result.UserMessage;

                return Page();
            }

            TempData["Success"] = string.IsNullOrWhiteSpace(result.UserMessage)
                ? "Loja cadastrada com sucesso."
                : result.UserMessage;

            return RedirectToPage("./Index");
        }

        var editarResult = await _lojaService.EditarAsync(Loja);

        if (editarResult.Status != PackageStatus.Success)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(editarResult.UserMessage)
                ? "Não foi possível atualizar a loja."
                : editarResult.UserMessage;

            return Page();
        }

        TempData["Success"] = string.IsNullOrWhiteSpace(editarResult.UserMessage)
            ? "Loja atualizada com sucesso."
            : editarResult.UserMessage;

        return RedirectToPage("./Index");
    }

    private string ObterMensagemValidacao()
    {
        var erros = ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (!erros.Any())
            return "Existem campos inválidos. Revise os dados informados.";

        return string.Join(" | ", erros);
    }
}