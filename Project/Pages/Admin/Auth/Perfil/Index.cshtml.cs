using Domain.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Project.Shared;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Project.Pages.Admin.Auth.Perfil;

[Authorize(Roles = "Desenvolvedor")]
public class IndexModel(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<AspNetCoreUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Busca { get; set; }

    [BindProperty]
    public PerfilInput Input { get; set; } = new();

    public IReadOnlyList<PerfilRow> Perfis { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Perfil";
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        ViewData["Title"] = "Perfil";

        if (!ModelState.IsValid)
        {
            TempData["WarningMessage"] = "Confira os campos informados.";
            await LoadAsync();
            return Page();
        }

        var normalizedName = Input.Nome.Trim();
        var isNew = Input.Id is null;
        var role = isNew
            ? new IdentityRole<Guid> { Id = Guid.NewGuid() }
            : await roleManager.FindByIdAsync(Input.Id!.Value.ToString());

        if (role is null)
        {
            TempData["ErrorMessage"] = "Perfil nao encontrado.";
            return RedirectToPage();
        }

        var roleWithName = await roleManager.FindByNameAsync(normalizedName);
        if (roleWithName is not null && roleWithName.Id != role.Id)
        {
            TempData["WarningMessage"] = "Ja existe um perfil com esse nome.";
            await LoadAsync();
            return Page();
        }

        role.Name = normalizedName;

        var result = isNew
            ? await roleManager.CreateAsync(role)
            : await roleManager.UpdateAsync(role);

        if (result.Succeeded)
        {
            result = await SyncMenuClaimsAsync(role, Input.Menus);
        }

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? (isNew ? "Perfil criado com sucesso." : "Perfil atualizado com sucesso.")
            : string.Join(" ", result.Errors.Select(x => x.Description));

        return result.Succeeded ? RedirectToPage(new { Busca }) : Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
        {
            TempData["ErrorMessage"] = "Perfil nao encontrado.";
            return RedirectToPage(new { Busca });
        }

        if (role.Name == "Desenvolvedor")
        {
            TempData["WarningMessage"] = "O perfil Desenvolvedor nao pode ser excluido.";
            return RedirectToPage(new { Busca });
        }

        var users = await userManager.GetUsersInRoleAsync(role.Name!);
        if (users.Count > 0)
        {
            TempData["WarningMessage"] = "Nao e possivel excluir um perfil vinculado a usuarios.";
            return RedirectToPage(new { Busca });
        }

        var result = await roleManager.DeleteAsync(role);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Perfil excluido com sucesso."
            : string.Join(" ", result.Errors.Select(x => x.Description));

        return RedirectToPage(new { Busca });
    }

    private async Task LoadAsync()
    {
        var query = roleManager.Roles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Busca))
        {
            var busca = Busca.Trim();
            query = query.Where(x => x.Name != null && x.Name.Contains(busca));
        }

        var roles = await query.OrderBy(x => x.Name).ToListAsync();
        var rows = new List<PerfilRow>();

        foreach (var role in roles)
        {
            var users = role.Name is null
                ? []
                : await userManager.GetUsersInRoleAsync(role.Name);

            var menuIds = (await roleManager.GetClaimsAsync(role))
                .Where(x => x.Type == AdminMenuCatalog.ClaimType)
                .Select(x => x.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var menusResumo = AdminMenuCatalog.AllItems
                .Where(x => menuIds.Contains(x.Id))
                .Select(x => x.Label)
                .ToList();

            rows.Add(new PerfilRow(
                role.Id,
                role.Name ?? string.Empty,
                users.Count,
                menuIds.ToList(),
                menusResumo.Count == 0 ? "Nenhum menu selecionado" : string.Join(", ", menusResumo)));
        }

        Perfis = rows;
    }

    private async Task<IdentityResult> SyncMenuClaimsAsync(IdentityRole<Guid> role, IEnumerable<string>? selectedMenus)
    {
        var selected = AdminMenuCatalog.Normalize(selectedMenus);
        var currentClaims = (await roleManager.GetClaimsAsync(role))
            .Where(x => x.Type == AdminMenuCatalog.ClaimType)
            .ToList();

        foreach (var claim in currentClaims.Where(x => !selected.Contains(x.Value)))
        {
            var result = await roleManager.RemoveClaimAsync(role, claim);
            if (!result.Succeeded)
            {
                return result;
            }
        }

        var currentValues = currentClaims.Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var menuId in selected.Where(x => !currentValues.Contains(x)))
        {
            var result = await roleManager.AddClaimAsync(role, new Claim(AdminMenuCatalog.ClaimType, menuId));
            if (!result.Succeeded)
            {
                return result;
            }
        }

        return IdentityResult.Success;
    }

    public sealed class PerfilInput
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Informe o nome do perfil.")]
        public string Nome { get; set; } = string.Empty;

        public List<string> Menus { get; set; } = [];
    }

    public sealed record PerfilRow(
        Guid Id,
        string Nome,
        int UsuariosVinculados,
        IReadOnlyList<string> MenuIds,
        string MenusResumo);
}
