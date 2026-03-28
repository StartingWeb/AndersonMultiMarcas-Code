using System.ComponentModel.DataAnnotations;
using Data;
using Domain.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Project.Pages.Admin.Auth;

public class UsuariosModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AspNetCoreUser> _userManager;
    private readonly RoleManager<AspNetCoreRole> _roleManager;

    public UsuariosModel(
        ApplicationDbContext db,
        UserManager<AspNetCoreUser> userManager,
        RoleManager<AspNetCoreRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool NewUser { get; set; }

    [BindProperty]
    public UserInputModel Input { get; set; } = new();

    public IReadOnlyList<UserListItem> Users { get; private set; } = [];
    public IReadOnlyList<RoleOptionItem> Roles { get; private set; } = [];
    public int TotalUsers { get; private set; }
    public int ActiveUsers { get; private set; }
    public int FilteredUsers => Users.Count;
    public bool IsEditing => Input.Id.HasValue;
    public bool IsModalOpen => NewUser || IsEditing || !ModelState.IsValid;

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

        AspNetCoreUser? user;

        if (Input.Id.HasValue)
        {
            user = await UpdateUserAsync();
        }
        else
        {
            user = await CreateUserAsync();
        }

        if (!ModelState.IsValid || user == null)
        {
            await LoadPageAsync();
            return Page();
        }

        await SyncUserRolesAsync(user, Input.SelectedRoleIds);

        if (!ModelState.IsValid)
        {
            await LoadPageAsync();
            return Page();
        }

        TempData["Success"] = Input.Id.HasValue
            ? "Usuário atualizado com sucesso."
            : "Usuário cadastrado com sucesso.";

        return RedirectToPage("/Admin/Auth/Usuarios", new { search = Search });
    }

    private async Task LoadPageAsync()
    {
        Roles = await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleOptionItem
            {
                Id = role.Id,
                Name = role.Name ?? "-",
                Descricao = string.IsNullOrWhiteSpace(role.Descricao) ? "Sem descrição." : role.Descricao
            })
            .ToListAsync();

        var query = _userManager.Users.AsNoTracking();

        TotalUsers = await query.CountAsync();
        ActiveUsers = await query.CountAsync(user => !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var searchValue = Search.Trim();

            query = query.Where(user =>
                (user.NomeCompleto != null && user.NomeCompleto.Contains(searchValue)) ||
                (user.UserName != null && user.UserName.Contains(searchValue)) ||
                (user.Email != null && user.Email.Contains(searchValue)));
        }

        var users = await query
            .OrderBy(user => user.NomeCompleto ?? user.UserName)
            .ThenBy(user => user.UserName)
            .ToListAsync();

        var roleNamesById = Roles.ToDictionary(role => role.Id, role => role.Name);

        var userRoleLookup = await _db.UserRoles
            .AsNoTracking()
            .Where(link => users.Select(user => user.Id).Contains(link.UserId))
            .GroupBy(link => link.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                RoleIds = group.Select(item => item.RoleId).ToList()
            })
            .ToListAsync();

        var roleIdsByUser = userRoleLookup.ToDictionary(
            item => item.UserId,
            item => item.RoleIds);

        Users = users
            .Select(user => UserListItem.From(
                user,
                roleIdsByUser.GetValueOrDefault(user.Id, []).Select(roleId => roleNamesById.GetValueOrDefault(roleId, "-"))))
            .ToList();

        if (EditId.HasValue)
        {
            var user = await _userManager.FindByIdAsync(EditId.Value.ToString());
            if (user != null)
            {
                var selectedRoleIds = await _db.UserRoles
                    .AsNoTracking()
                    .Where(link => link.UserId == user.Id)
                    .Select(link => link.RoleId)
                    .ToListAsync();

                Input = UserInputModel.From(user, selectedRoleIds);
            }
        }
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Input.NomeCompleto))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.NomeCompleto)}", "Informe o nome completo.");
        }

        if (string.IsNullOrWhiteSpace(Input.UserName))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.UserName)}", "Informe o usuário.");
        }

        if (string.IsNullOrWhiteSpace(Input.Email))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Email)}", "Informe o e-mail.");
        }

        if (!Input.Id.HasValue && string.IsNullOrWhiteSpace(Input.Password))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Password)}", "Informe a senha para o novo usuário.");
        }

        if (!string.IsNullOrWhiteSpace(Input.Password) && Input.Password != Input.ConfirmPassword)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.ConfirmPassword)}", "A confirmação da senha não confere.");
        }

        return ModelState.IsValid;
    }

    private async Task<AspNetCoreUser?> CreateUserAsync()
    {
        var user = new AspNetCoreUser
        {
            Id = Guid.NewGuid(),
            NomeCompleto = Input.NomeCompleto.Trim(),
            UserName = Input.UserName.Trim(),
            Email = Input.Email.Trim(),
            PhoneNumber = NormalizePhone(Input.PhoneNumber),
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        ApplyActiveStatus(user, Input.Active);

        var result = await _userManager.CreateAsync(user, Input.Password!);
        AddIdentityErrors(result);

        return result.Succeeded ? user : null;
    }

    private async Task<AspNetCoreUser?> UpdateUserAsync()
    {
        var user = await _userManager.FindByIdAsync(Input.Id!.Value.ToString());
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Usuário não encontrado.");
            return null;
        }

        user.NomeCompleto = Input.NomeCompleto.Trim();
        user.UserName = Input.UserName.Trim();
        user.Email = Input.Email.Trim();
        user.PhoneNumber = NormalizePhone(Input.PhoneNumber);
        user.EmailConfirmed = true;
        user.LockoutEnabled = true;

        ApplyActiveStatus(user, Input.Active);

        var updateResult = await _userManager.UpdateAsync(user);
        AddIdentityErrors(updateResult);

        if (!updateResult.Succeeded)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(Input.Password))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, Input.Password);
            AddIdentityErrors(resetResult);

            if (!resetResult.Succeeded)
            {
                return null;
            }
        }

        return user;
    }

    private async Task SyncUserRolesAsync(AspNetCoreUser user, IReadOnlyCollection<Guid> selectedRoleIds)
    {
        var selectedIds = selectedRoleIds.ToHashSet();

        var existingRoleIds = await _db.UserRoles
            .AsNoTracking()
            .Where(link => link.UserId == user.Id)
            .Select(link => link.RoleId)
            .ToListAsync();

        var rolesToRemove = Roles
            .Where(role => existingRoleIds.Contains(role.Id) && !selectedIds.Contains(role.Id))
            .Select(role => role.Name)
            .ToList();

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            AddIdentityErrors(removeResult);
            if (!removeResult.Succeeded)
            {
                return;
            }
        }

        var rolesToAdd = Roles
            .Where(role => selectedIds.Contains(role.Id) && !existingRoleIds.Contains(role.Id))
            .Select(role => role.Name)
            .ToList();

        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            AddIdentityErrors(addResult);
        }
    }

    private void ApplyActiveStatus(AspNetCoreUser user, bool active)
    {
        user.LockoutEnd = active
            ? null
            : DateTimeOffset.UtcNow.AddYears(50);
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

    private static string? NormalizePhone(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    public sealed class UserInputModel
    {
        public Guid? Id { get; set; }

        [Display(Name = "Nome completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Display(Name = "Usuário")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "E-mail")]
        [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Telefone")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Senha")]
        public string? Password { get; set; }

        [Display(Name = "Confirmar senha")]
        public string? ConfirmPassword { get; set; }

        public bool Active { get; set; } = true;
        public List<Guid> SelectedRoleIds { get; set; } = [];

        public static UserInputModel From(AspNetCoreUser user, IReadOnlyCollection<Guid> selectedRoleIds)
        {
            return new UserInputModel
            {
                Id = user.Id,
                NomeCompleto = user.NomeCompleto ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Active = !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow,
                SelectedRoleIds = selectedRoleIds.ToList()
            };
        }
    }

    public sealed class RoleOptionItem
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
    }

    public sealed class UserListItem
    {
        public Guid Id { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = "Não informado";
        public string PhoneDisplay { get; init; } = "Não informado";
        public bool Active { get; init; }
        public string Initials { get; init; } = "US";
        public string RolesDisplay { get; init; } = "Sem perfil";

        public static UserListItem From(AspNetCoreUser user, IEnumerable<string> roleNames)
        {
            var displayName = string.IsNullOrWhiteSpace(user.NomeCompleto)
                    ? (user.UserName ?? "Usuário")
                : user.NomeCompleto;

            var orderedRoleNames = roleNames
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(roleName => roleName)
                .ToList();

            return new UserListItem
            {
                Id = user.Id,
                DisplayName = displayName,
                UserName = user.UserName ?? "-",
                Email = string.IsNullOrWhiteSpace(user.Email) ? "Não informado" : user.Email,
                PhoneDisplay = FormatPhone(user.PhoneNumber),
                Active = !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow,
                Initials = BuildInitials(displayName),
                RolesDisplay = orderedRoleNames.Count == 0 ? "Sem perfil" : string.Join(", ", orderedRoleNames)
            };
        }

        private static string BuildInitials(string value)
        {
            var parts = value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0]));

            var initials = string.Concat(parts);
            return string.IsNullOrWhiteSpace(initials) ? "US" : initials;
        }

        private static string FormatPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return "Não informado";
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 11)
            {
                return $"({digits[..2]}) {digits[2..7]}-{digits[7..]}";
            }

            if (digits.Length == 10)
            {
                return $"({digits[..2]}) {digits[2..6]}-{digits[6..]}";
            }

            return phone;
        }
    }
}
