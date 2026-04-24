using Data;
using Domain.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Project.Navigation;

public sealed class AdminMenuService : IAdminMenuService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AspNetCoreUser> _userManager;
    private readonly SignInManager<AspNetCoreUser> _signInManager;

    public AdminMenuService(
        ApplicationDbContext db,
        UserManager<AspNetCoreUser> userManager,
        SignInManager<AspNetCoreUser> signInManager)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<AdminMenuViewModel> BuildAsync(ClaimsPrincipal user, string? currentPath, CancellationToken cancellationToken = default)
    {
        if (!_signInManager.IsSignedIn(user))
        {
            return new AdminMenuViewModel();
        }

        var currentUser = await _userManager.GetUserAsync(user);
        if (currentUser == null)
        {
            return new AdminMenuViewModel();
        }

        var roleNames = await _userManager.GetRolesAsync(currentUser);
        var isDeveloper = roleNames.Any(roleName =>
            string.Equals(roleName, "Desenvolvedor", StringComparison.OrdinalIgnoreCase));

        var userDisplayName = string.IsNullOrWhiteSpace(currentUser.NomeCompleto)
            ? (currentUser.UserName ?? "Usuario")
            : currentUser.NomeCompleto;

        var userRoleLabel = roleNames.Count > 0
            ? string.Join(", ", roleNames)
            : "Sem perfil";

        var roleIds = await _db.UserRoles
            .AsNoTracking()
            .Where(link => link.UserId == currentUser.Id)
            .Select(link => link.RoleId)
            .ToListAsync(cancellationToken);

        var allMenus = await _db.Menus
            .AsNoTracking()
            .Where(menu => menu.Ativo)
            .OrderBy(menu => menu.Ordem)
            .ThenBy(menu => menu.Nome)
            .ToListAsync(cancellationToken);

        var allowedMenuIds = await _db.MenuRoles
            .AsNoTracking()
            .Where(link => roleIds.Contains(link.RoleId))
            .Select(link => link.MenuId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (allowedMenuIds.Count == 0)
        {
            allowedMenuIds = ResolveFallbackMenuIds(allMenus, roleNames);
        }

        var menuLookup = allMenus.ToDictionary(menu => menu.Id);
        var visibleIds = new HashSet<Guid>(allowedMenuIds);

        foreach (var allowedMenuId in allowedMenuIds)
        {
            var cursor = menuLookup.GetValueOrDefault(allowedMenuId);
            while (cursor?.MenuPaiId is Guid parentId && menuLookup.TryGetValue(parentId, out var parent))
            {
                visibleIds.Add(parent.Id);
                cursor = parent;
            }
        }

        var visibleMenus = allMenus
            .Where(menu => visibleIds.Contains(menu.Id))
            .Where(menu => isDeveloper || !string.Equals(menu.Nome, "Auth", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var childrenByParent = visibleMenus
            .Where(menu => menu.MenuPaiId.HasValue)
            .GroupBy(menu => menu.MenuPaiId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(menu => menu.Ordem).ThenBy(menu => menu.Nome).ToList());

        var normalizedPath = NormalizePath(currentPath);
        var rootItems = visibleMenus
            .Where(menu => !menu.MenuPaiId.HasValue)
            .OrderBy(menu => menu.Ordem)
            .ThenBy(menu => menu.Nome)
            .Select(menu => BuildNode(menu, childrenByParent, normalizedPath))
            .ToList();

        return new AdminMenuViewModel
        {
            UserDisplayName = userDisplayName,
            UserRoleLabel = userRoleLabel,
            RootItems = rootItems
        };
    }

    private static AdminMenuViewModel.MenuNodeViewModel BuildNode(
        AspNetMenu menu,
        IReadOnlyDictionary<Guid, List<AspNetMenu>> childrenByParent,
        string currentPath)
    {
        var children = childrenByParent.TryGetValue(menu.Id, out var childMenus)
            ? childMenus.Select(child => BuildNode(child, childrenByParent, currentPath)).ToList()
            : [];

        var ownActive = IsActive(menu.Url, currentPath);
        var anyChildActive = children.Any(child => child.IsActive);

        return new AdminMenuViewModel.MenuNodeViewModel
        {
            Id = menu.Id,
            Nome = menu.Nome,
            Url = menu.Url,
            Icone = string.IsNullOrWhiteSpace(menu.Icone) ? "bi bi-grid" : menu.Icone,
            IsActive = ownActive || anyChildActive,
            Children = children
        };
    }

    private static bool IsActive(string? url, string currentPath)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var menuPath = NormalizePath(url);
        if (string.IsNullOrWhiteSpace(menuPath))
        {
            return false;
        }

        return string.Equals(currentPath, menuPath, StringComparison.OrdinalIgnoreCase)
            || currentPath.StartsWith($"{menuPath}/", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized == "/" ? "/" : normalized.TrimEnd('/');
    }

    private static List<Guid> ResolveFallbackMenuIds(
        IReadOnlyCollection<AspNetMenu> allMenus,
        IEnumerable<string> roleNames)
    {
        var normalizedRoles = roleNames
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalizedRoles.Contains("Desenvolvedor"))
        {
            return allMenus.Select(menu => menu.Id).ToList();
        }

        if (normalizedRoles.Contains("Administrador"))
        {
            return allMenus
                .Where(menu => !string.Equals(menu.Nome, "Auth", StringComparison.OrdinalIgnoreCase))
                .Select(menu => menu.Id)
                .ToList();
        }

        if (normalizedRoles.Contains("AdminConcessionaria"))
        {
            return allMenus
                .Where(menu =>
                    string.Equals(menu.Url, "/Admin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(menu.Url, "/Admin/VeiculosVenda", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(menu.Url, "/Admin/ConferenciaEstoque", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(menu.Url, "/Veiculo/ImportaJSON", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(menu.Url, "/Veiculo/ImportaMidia", StringComparison.OrdinalIgnoreCase))
                .Select(menu => menu.Id)
                .ToList();
        }

        return [];
    }
}
