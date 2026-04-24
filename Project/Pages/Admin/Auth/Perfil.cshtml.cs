using System.ComponentModel.DataAnnotations;
using Data;
using Domain.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Project.Pages.Admin.Auth;

[Authorize(Roles = "Desenvolvedor")]
public class PerfilModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly RoleManager<AspNetCoreRole> _roleManager;

    public PerfilModel(ApplicationDbContext db, RoleManager<AspNetCoreRole> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? EditRoleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool NewProfile { get; set; }

    [BindProperty]
    public ProfileInputModel Input { get; set; } = new();

    public IReadOnlyList<ProfileListItem> Profiles { get; private set; } = [];
    public IReadOnlyList<UserOptionItem> Users { get; private set; } = [];
    public bool IsEditing => Input.Id.HasValue;
    public bool IsModalOpen => NewProfile || IsEditing || !ModelState.IsValid;

    public async Task OnGetAsync()
    {
        await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ValidateInput())
        {
            await LoadPageAsync();
            return Page();
        }

        var role = Input.Id.HasValue
            ? await _roleManager.FindByIdAsync(Input.Id.Value.ToString())
            : null;

        if (Input.Id.HasValue && role == null)
        {
            ModelState.AddModelError(string.Empty, "Perfil não encontrado.");
            await LoadPageAsync();
            return Page();
        }

        if (role == null)
        {
            role = new AspNetCoreRole
            {
                Id = Guid.NewGuid()
            };
        }

        role.Name = Input.Name.Trim();
        role.NormalizedName = Input.Name.Trim().ToUpperInvariant();
        role.Descricao = NormalizeNullable(Input.Descricao);

        IdentityResult result;
        if (Input.Id.HasValue)
        {
            result = await _roleManager.UpdateAsync(role);
        }
        else
        {
            result = await _roleManager.CreateAsync(role);
        }

        AddIdentityErrors(result);

        if (!result.Succeeded)
        {
            await LoadPageAsync();
            return Page();
        }

        await SyncRoleUsersAsync(role.Id, Input.SelectedUserIds);

        TempData["Success"] = Input.Id.HasValue
            ? "Perfil atualizado com sucesso."
            : "Perfil criado com sucesso.";

        return RedirectToPage("/Admin/Auth/Perfil", new { search = Search });
    }

    private async Task LoadPageAsync()
    {
        var roleEntities = await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync();

        var userCountByRole = await _db.UserRoles
            .AsNoTracking()
            .GroupBy(link => link.RoleId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count);

        var profiles = roleEntities
            .Select(role => new ProfileListItem
            {
                Id = role.Id,
                Name = role.Name ?? "-",
                Descricao = string.IsNullOrWhiteSpace(role.Descricao) ? "Sem descrição." : role.Descricao,
                UserCount = userCountByRole.GetValueOrDefault(role.Id)
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var searchValue = Search.Trim();
            profiles = profiles
                .Where(profile =>
                    profile.Name.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    profile.Descricao.Contains(searchValue, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        Profiles = profiles;

        Users = await _db.Users
            .AsNoTracking()
            .OrderBy(user => user.NomeCompleto ?? user.UserName)
            .ThenBy(user => user.UserName)
            .Select(user => new UserOptionItem
            {
                Id = user.Id,
                DisplayName = string.IsNullOrWhiteSpace(user.NomeCompleto)
                    ? (user.UserName ?? "Usuario")
                    : user.NomeCompleto!,
                UserName = user.UserName ?? "-",
                Email = string.IsNullOrWhiteSpace(user.Email) ? "Sem e-mail" : user.Email
            })
            .ToListAsync();

        if (EditRoleId.HasValue)
        {
            var editRole = await _roleManager.FindByIdAsync(EditRoleId.Value.ToString());
            if (editRole != null)
            {
                var selectedUserIds = await _db.UserRoles
                    .AsNoTracking()
                    .Where(link => link.RoleId == editRole.Id)
                    .Select(link => link.UserId)
                    .ToListAsync();

                Input = ProfileInputModel.From(editRole, selectedUserIds);
            }
        }
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Name)}", "Informe o nome do perfil.");
        }

        return ModelState.IsValid;
    }

    private async Task SyncRoleUsersAsync(Guid roleId, IReadOnlyCollection<Guid> selectedUserIds)
    {
        var existingLinks = await _db.UserRoles
            .Where(link => link.RoleId == roleId)
            .ToListAsync();

        var selectedIds = selectedUserIds.ToHashSet();

        var linksToRemove = existingLinks
            .Where(link => !selectedIds.Contains(link.UserId))
            .ToList();

        if (linksToRemove.Count > 0)
        {
            _db.UserRoles.RemoveRange(linksToRemove);
        }

        var existingIds = existingLinks.Select(link => link.UserId).ToHashSet();
        var linksToAdd = selectedIds
            .Where(userId => !existingIds.Contains(userId))
            .Select(userId => new AspNetCoreUserRoles
            {
                UserId = userId,
                RoleId = roleId
            })
            .ToList();

        if (linksToAdd.Count > 0)
        {
            _db.UserRoles.AddRange(linksToAdd);
        }

        await _db.SaveChangesAsync();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class ProfileInputModel
    {
        public Guid? Id { get; set; }

        [Display(Name = "Nome")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        public List<Guid> SelectedUserIds { get; set; } = [];

        public static ProfileInputModel From(AspNetCoreRole role, IReadOnlyCollection<Guid> userIds)
        {
            return new ProfileInputModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Descricao = role.Descricao,
                SelectedUserIds = userIds.ToList()
            };
        }
    }

    public sealed class ProfileListItem
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public int UserCount { get; init; }
    }

    public sealed class UserOptionItem
    {
        public Guid Id { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}
