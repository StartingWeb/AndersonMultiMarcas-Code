using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concessionaria.Pages.Veiculo;

public class ImportaMidiaModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("./ImportaJSON");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("./ImportaJSON");
    }
}
