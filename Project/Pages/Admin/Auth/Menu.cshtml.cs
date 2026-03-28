using System.ComponentModel.DataAnnotations;
using Data;
using Domain.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Project.Pages.Admin.Auth;

public class MenuModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public MenuModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool NewMenu { get; set; }

    [BindProperty]
    public MenuInputModel Input { get; set; } = new();

    public IReadOnlyList<MenuListItem> Menus { get; private set; } = [];
    public IReadOnlyList<RoleOptionItem> Roles { get; private set; } = [];
    public IReadOnlyList<ParentMenuOptionItem> ParentMenuOptions { get; private set; } = [];
    public int TotalMenus { get; private set; }
    public int ActiveMenus { get; private set; }
    public int FilteredMenus => Menus.Count;
    public bool IsEditing => Input.Id.HasValue;
    public bool IsModalOpen => NewMenu || IsEditing || !ModelState.IsValid;

    public async Task OnGetAsync()
    {
        await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!await ValidateInputAsync())
        {
            await LoadPageAsync();
            return Page();
        }

        AspNetMenu? menu;
        if (Input.Id.HasValue)
        {
            menu = await UpdateMenuAsync();
        }
        else
        {
            menu = await CreateMenuAsync();
        }

        if (!ModelState.IsValid || menu == null)
        {
            await LoadPageAsync();
            return Page();
        }

        await SyncMenuRolesAsync(menu.Id, Input.SelectedRoleIds);

        TempData["Success"] = Input.Id.HasValue
            ? "Menu atualizado com sucesso."
            : "Menu cadastrado com sucesso.";

        return RedirectToPage("/Admin/Auth/Menu", new { search = Search });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var menu = await _db.Menus
            .FirstOrDefaultAsync(item => item.Id == id);

        if (menu == null)
        {
            TempData["Error"] = "Menu não encontrado.";
            return RedirectToPage("/Admin/Auth/Menu", new { search = Search });
        }

        var hasChildren = await _db.Menus
            .AsNoTracking()
            .AnyAsync(item => item.MenuPaiId == id);

        if (hasChildren)
        {
            TempData["Error"] = "Exclua ou reatribua os submenus antes de remover este menu.";
            return RedirectToPage("/Admin/Auth/Menu", new { search = Search });
        }

        var menuRoles = await _db.MenuRoles
            .Where(link => link.MenuId == id)
            .ToListAsync();

        if (menuRoles.Count > 0)
        {
            _db.MenuRoles.RemoveRange(menuRoles);
        }

        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Menu excluído com sucesso.";
        return RedirectToPage("/Admin/Auth/Menu", new { search = Search });
    }

    private async Task LoadPageAsync()
    {
        Roles = await _db.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleOptionItem
            {
                Id = role.Id,
                Name = role.Name ?? "-",
                Descricao = string.IsNullOrWhiteSpace(role.Descricao) ? "Sem descrição." : role.Descricao
            })
            .ToListAsync();

        var allMenus = await _db.Menus
            .AsNoTracking()
            .OrderBy(menu => menu.Ordem)
            .ThenBy(menu => menu.Nome)
            .ToListAsync();

        TotalMenus = allMenus.Count;
        ActiveMenus = allMenus.Count(menu => menu.Ativo);

        var queryMenus = allMenus.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var searchValue = Search.Trim();
            queryMenus = queryMenus.Where(menu =>
                menu.Nome.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(menu.Descricao) && menu.Descricao.Contains(searchValue, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(menu.Url) && menu.Url.Contains(searchValue, StringComparison.OrdinalIgnoreCase)));
        }

        var filteredMenus = queryMenus.ToList();

        var roleLookup = Roles.ToDictionary(role => role.Id, role => role.Name);
        var menuRoles = await _db.MenuRoles
            .AsNoTracking()
            .Where(link => filteredMenus.Select(menu => menu.Id).Contains(link.MenuId))
            .GroupBy(link => link.MenuId)
            .Select(group => new
            {
                MenuId = group.Key,
                RoleIds = group.Select(item => item.RoleId).ToList()
            })
            .ToListAsync();

        var roleIdsByMenu = menuRoles.ToDictionary(item => item.MenuId, item => item.RoleIds);

        Menus = filteredMenus
            .Select(menu => MenuListItem.From(
                menu,
                allMenus,
                roleIdsByMenu.GetValueOrDefault(menu.Id, []).Select(roleId => roleLookup.GetValueOrDefault(roleId, "-"))))
            .ToList();

        if (EditId.HasValue)
        {
            var editMenu = await _db.Menus
                .AsNoTracking()
                .FirstOrDefaultAsync(menu => menu.Id == EditId.Value);

            if (editMenu != null)
            {
                var selectedRoleIds = await _db.MenuRoles
                    .AsNoTracking()
                    .Where(link => link.MenuId == editMenu.Id)
                    .Select(link => link.RoleId)
                    .ToListAsync();

                Input = MenuInputModel.From(editMenu, selectedRoleIds);
            }
        }

        var restrictedParentIds = Input.Id.HasValue
            ? GetDescendantIds(Input.Id.Value, allMenus).Append(Input.Id.Value).ToHashSet()
            : [];

        ParentMenuOptions = allMenus
            .Where(menu => !restrictedParentIds.Contains(menu.Id))
            .Select(menu => ParentMenuOptionItem.From(menu, allMenus))
            .ToList();
    }

    private async Task<bool> ValidateInputAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Nome))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Nome)}", "Informe o nome do menu.");
        }

        if (Input.MenuPaiId.HasValue && Input.MenuPaiId == Input.Id)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.MenuPaiId)}", "O menu não pode ser pai dele mesmo.");
        }

        if (Input.Id.HasValue)
        {
            var menus = await _db.Menus
                .AsNoTracking()
                .ToListAsync();

            var descendantIds = GetDescendantIds(Input.Id.Value, menus).ToHashSet();
            if (Input.MenuPaiId.HasValue && descendantIds.Contains(Input.MenuPaiId.Value))
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.MenuPaiId)}", "O menu pai não pode ser um submenu do próprio menu.");
            }
        }

        return ModelState.IsValid;
    }

    private async Task<AspNetMenu?> CreateMenuAsync()
    {
        var menu = new AspNetMenu
        {
            Id = Guid.NewGuid(),
            Nome = Input.Nome.Trim(),
            Descricao = NormalizeNullable(Input.Descricao),
            Icone = NormalizeNullable(Input.Icone),
            Url = NormalizeNullable(Input.Url),
            Ordem = Input.Ordem,
            Ativo = Input.Ativo,
            MenuPaiId = Input.MenuPaiId
        };

        _db.Menus.Add(menu);
        await _db.SaveChangesAsync();
        return menu;
    }

    private async Task<AspNetMenu?> UpdateMenuAsync()
    {
        var menu = await _db.Menus.FirstOrDefaultAsync(item => item.Id == Input.Id!.Value);
        if (menu == null)
        {
            ModelState.AddModelError(string.Empty, "Menu não encontrado.");
            return null;
        }

        menu.Nome = Input.Nome.Trim();
        menu.Descricao = NormalizeNullable(Input.Descricao);
        menu.Icone = NormalizeNullable(Input.Icone);
        menu.Url = NormalizeNullable(Input.Url);
        menu.Ordem = Input.Ordem;
        menu.Ativo = Input.Ativo;
        menu.MenuPaiId = Input.MenuPaiId;

        await _db.SaveChangesAsync();
        return menu;
    }

    private async Task SyncMenuRolesAsync(Guid menuId, IReadOnlyCollection<Guid> selectedRoleIds)
    {
        var existingLinks = await _db.MenuRoles
            .Where(link => link.MenuId == menuId)
            .ToListAsync();

        var selectedIds = selectedRoleIds.ToHashSet();

        var linksToRemove = existingLinks
            .Where(link => !selectedIds.Contains(link.RoleId))
            .ToList();

        if (linksToRemove.Count > 0)
        {
            _db.MenuRoles.RemoveRange(linksToRemove);
        }

        var existingIds = existingLinks.Select(link => link.RoleId).ToHashSet();
        var linksToAdd = selectedIds
            .Where(roleId => !existingIds.Contains(roleId))
            .Select(roleId => new AspNetMenuRole
            {
                MenuId = menuId,
                RoleId = roleId
            })
            .ToList();

        if (linksToAdd.Count > 0)
        {
            _db.MenuRoles.AddRange(linksToAdd);
        }

        await _db.SaveChangesAsync();
    }

    private static IEnumerable<Guid> GetDescendantIds(Guid rootId, IReadOnlyCollection<AspNetMenu> menus)
    {
        var childrenByParent = menus
            .Where(menu => menu.MenuPaiId.HasValue)
            .GroupBy(menu => menu.MenuPaiId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(menu => menu.Id).ToList());

        var stack = new Stack<Guid>();
        stack.Push(rootId);

        var descendants = new List<Guid>();

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!childrenByParent.TryGetValue(current, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                descendants.Add(child);
                stack.Push(child);
            }
        }

        return descendants;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class MenuInputModel
    {
        public Guid? Id { get; set; }

        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Display(Name = "Icone")]
        public string? Icone { get; set; }

        [Display(Name = "URL")]
        public string? Url { get; set; }

        [Display(Name = "Ordem")]
        public int Ordem { get; set; }

        [Display(Name = "Menu pai")]
        public Guid? MenuPaiId { get; set; }

        public bool Ativo { get; set; } = true;
        public List<Guid> SelectedRoleIds { get; set; } = [];

        public static MenuInputModel From(AspNetMenu menu, IReadOnlyCollection<Guid> selectedRoleIds)
        {
            return new MenuInputModel
            {
                Id = menu.Id,
                Nome = menu.Nome,
                Descricao = menu.Descricao,
                Icone = menu.Icone,
                Url = menu.Url,
                Ordem = menu.Ordem,
                MenuPaiId = menu.MenuPaiId,
                Ativo = menu.Ativo,
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

    public sealed class MenuListItem
    {
        public Guid Id { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string Descricao { get; init; } = "Sem descrição.";
        public string Url { get; init; } = "Grupo";
        public int Ordem { get; init; }
        public bool Ativo { get; init; }
        public int Level { get; init; }
        public string ParentName { get; init; } = "Raiz";
        public string ProfilesDisplay { get; init; } = "Sem perfil";

        public static MenuListItem From(AspNetMenu menu, IReadOnlyCollection<AspNetMenu> allMenus, IEnumerable<string> profileNames)
        {
            var parentLookup = allMenus.ToDictionary(item => item.Id);
            var level = 0;
            var cursor = menu;

            while (cursor.MenuPaiId.HasValue && parentLookup.TryGetValue(cursor.MenuPaiId.Value, out var parent))
            {
                level++;
                cursor = parent;
            }

            var parentName = menu.MenuPaiId.HasValue && parentLookup.TryGetValue(menu.MenuPaiId.Value, out var directParent)
                ? directParent.Nome
                : "Raiz";

            var orderedProfiles = profileNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();

            return new MenuListItem
            {
                Id = menu.Id,
                Nome = menu.Nome,
                Descricao = string.IsNullOrWhiteSpace(menu.Descricao) ? "Sem descrição." : menu.Descricao,
                Url = string.IsNullOrWhiteSpace(menu.Url) ? "Grupo" : menu.Url,
                Ordem = menu.Ordem,
                Ativo = menu.Ativo,
                Level = level,
                ParentName = parentName,
                ProfilesDisplay = orderedProfiles.Count == 0 ? "Sem perfil" : string.Join(", ", orderedProfiles)
            };
        }
    }

    public sealed class ParentMenuOptionItem
    {
        public Guid Id { get; init; }
        public string Label { get; init; } = string.Empty;

        public static ParentMenuOptionItem From(AspNetMenu menu, IReadOnlyCollection<AspNetMenu> allMenus)
        {
            var parentLookup = allMenus.ToDictionary(item => item.Id);
            var level = 0;
            var cursor = menu;

            while (cursor.MenuPaiId.HasValue && parentLookup.TryGetValue(cursor.MenuPaiId.Value, out var parent))
            {
                level++;
                cursor = parent;
            }

            var prefix = new string('>', level);

            return new ParentMenuOptionItem
            {
                Id = menu.Id,
                Label = string.IsNullOrWhiteSpace(prefix) ? menu.Nome : $"{prefix} {menu.Nome}"
            };
        }
    }
}
