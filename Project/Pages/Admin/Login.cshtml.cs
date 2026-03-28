using System.ComponentModel.DataAnnotations;
using Domain.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Project.Pages.Admin;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<AspNetCoreUser> _signInManager;
    private readonly UserManager<AspNetCoreUser> _userManager;

    public LoginModel(
        SignInManager<AspNetCoreUser> signInManager,
        UserManager<AspNetCoreUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public VmInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class VmInput
    {
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Preencha os campos obrigatórios para continuar.";
            return Page();
        }

        var normalizedEmail = Input.Email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            TempData["Success"] = "Login realizado com sucesso.";

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            return RedirectToPage("/Admin/Index");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Usuário bloqueado. Procure um administrador.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
        return Page();
    }
    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "Sessão encerrada com sucesso.";
        return RedirectToPage("/Admin/Login");
    }
}
