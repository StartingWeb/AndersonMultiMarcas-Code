using Domain.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Data;

public static class IdentitySeed
{
    public static async Task EnsureDeveloperUserAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<AspNetCoreRole>>();
        var userManager = services.GetRequiredService<UserManager<AspNetCoreUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        const string roleName = "Desenvolvedor";
        const string adminRoleName = "Administrador";
        const string officeRoleName = "AdminConcessionaria";
        const string devEmail = "dev@stw.com";
        const string devUser = "desenvolvedor";
        const string devPassword = "123456@Senha";

        await EnsureRoleAsync(roleManager, roleName, "Perfil com acesso total ao painel.");
        await EnsureRoleAsync(roleManager, adminRoleName, "Perfil administrativo do painel.");
        await EnsureRoleAsync(roleManager, officeRoleName, "Perfil operacional para vendas de veículos.");

        var usersInRole = await userManager.GetUsersInRoleAsync(roleName);
        if (!usersInRole.Any())
        {
            var user = new AspNetCoreUser
            {
                Id = Guid.NewGuid(),
                UserName = devUser,
                NormalizedUserName = devUser.ToUpperInvariant(),
                Email = devEmail,
                NormalizedEmail = devEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                NomeCompleto = "Desenvolvedor"
            };

            var result = await userManager.CreateAsync(user, devPassword);

            if (!result.Succeeded)
            {
                throw new Exception(
                    "Erro ao criar usuário Desenvolvedor: " +
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );
            }

            await userManager.AddToRoleAsync(user, roleName);
        }

        var menus = await EnsureBaseMenusAsync(db);

        var developerRole = await roleManager.FindByNameAsync(roleName);
        if (developerRole == null)
        {
            throw new Exception("Perfil Desenvolvedor não encontrado após a inicialização.");
        }

        var existingMenuIds = await db.MenuRoles
            .Where(x => x.RoleId == developerRole.Id)
            .Select(x => x.MenuId)
            .ToListAsync();

        var missingLinks = menus
            .Where(menu => !existingMenuIds.Contains(menu.Id))
            .Select(menu => new AspNetMenuRole
            {
                MenuId = menu.Id,
                RoleId = developerRole.Id
            })
            .ToList();

        if (missingLinks.Count > 0)
        {
            db.MenuRoles.AddRange(missingLinks);
            await db.SaveChangesAsync();
        }

        await EnsureRoleMenusAsync(db, roleManager, adminRoleName, menu =>
            !string.Equals(menu.Nome, "Auth", StringComparison.OrdinalIgnoreCase));

        await EnsureRoleMenusAsync(db, roleManager, officeRoleName, menu =>
            string.Equals(menu.Url, "/Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(menu.Url, "/Admin/VeiculosVenda", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(menu.Url, "/Admin/ConferenciaEstoque", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(menu.Url, "/Veiculo/ImportaJSON", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(menu.Url, "/Veiculo/ImportaMidia", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<List<AspNetMenu>> EnsureBaseMenusAsync(ApplicationDbContext db)
    {
        var menus = await db.Menus
            .OrderBy(x => x.Ordem)
            .ToListAsync();

        AspNetMenu EnsureMenu(
            string nome,
            int ordem,
            string? descricao = null,
            string? icone = null,
            string? url = null,
            Guid? menuPaiId = null,
            bool ativo = true)
        {
            var menu = menus.FirstOrDefault(item =>
                string.Equals(item.Nome, nome, StringComparison.OrdinalIgnoreCase) &&
                item.MenuPaiId == menuPaiId);

            if (menu == null)
            {
                menu = new AspNetMenu
                {
                    Id = Guid.NewGuid(),
                    Nome = nome,
                    Ordem = ordem,
                    MenuPaiId = menuPaiId
                };

                menus.Add(menu);
                db.Menus.Add(menu);
            }

            menu.Descricao = descricao;
            menu.Icone = icone;
            menu.Url = url;
            menu.Ordem = ordem;
            menu.Ativo = ativo;

            return menu;
        }

        var principal = EnsureMenu("Principal", 10, "Grupo principal do painel.");
        EnsureMenu("Dashboard", 11, icone: "bi bi-speedometer2", url: "/Admin", menuPaiId: principal.Id);
        EnsureMenu("Veiculos", 12, icone: "bi bi-car-front-fill", url: "/Veiculo", menuPaiId: principal.Id);
        EnsureMenu("Importar JSON", 13, descricao: "Importacao de veiculos via arquivo JSON legado.", icone: "bi bi-filetype-json", url: "/Veiculo/ImportaJSON", menuPaiId: principal.Id);
        EnsureMenu("Importar Midia", 14, descricao: "Importacao de fotos legadas para os veiculos.", icone: "bi bi-images", url: "/Veiculo/ImportaMidia", menuPaiId: principal.Id);
        EnsureMenu("Venda de Veículos", 15, descricao: "Operação de venda dos veículos.", icone: "bi bi-clipboard-check-fill", url: "/Admin/VeiculosVenda", menuPaiId: principal.Id);
        EnsureMenu("Conferencia Estoque", 16, descricao: "Conferencia de estoque por planilha Excel.", icone: "bi bi-file-earmark-spreadsheet", url: "/Admin/ConferenciaEstoque", menuPaiId: principal.Id);

        var cadastros = EnsureMenu("Cadastros", 20, "Cadastros base do sistema.");
        EnsureMenu("Lojas", 21, icone: "bi bi-shop", url: "/Loja", menuPaiId: cadastros.Id);
        EnsureMenu("Marcas", 22, icone: "bi bi-tags-fill", url: "/Marca", menuPaiId: cadastros.Id);
        EnsureMenu("Vendedores", 23, icone: "bi bi-person-badge-fill", url: "/Vendedor", menuPaiId: cadastros.Id);

        var auth = EnsureMenu("Auth", 30, "Controle de acessos.");
        EnsureMenu("Usuarios", 31, icone: "bi bi-people-fill", url: "/Admin/Auth/Usuarios", menuPaiId: auth.Id);
        EnsureMenu("Perfil", 32, icone: "bi bi-person-lines-fill", url: "/Admin/Auth/Perfil", menuPaiId: auth.Id);
        EnsureMenu("Menu", 33, icone: "bi bi-menu-button-wide-fill", url: "/Admin/Auth/Menu", menuPaiId: auth.Id);

        await db.SaveChangesAsync();

        return menus
            .OrderBy(x => x.Ordem)
            .ToList();
    }

    private static async Task EnsureRoleAsync(RoleManager<AspNetCoreRole> roleManager, string roleName, string descricao)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            await roleManager.CreateAsync(new AspNetCoreRole
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                Descricao = descricao
            });

            return;
        }

        if (!string.Equals(role.Descricao, descricao, StringComparison.Ordinal))
        {
            role.Descricao = descricao;
            await roleManager.UpdateAsync(role);
        }
    }

    private static async Task EnsureRoleMenusAsync(
        ApplicationDbContext db,
        RoleManager<AspNetCoreRole> roleManager,
        string roleName,
        Func<AspNetMenu, bool> predicate)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return;
        }

        var allowedMenuIds = (await db.Menus
                .AsNoTracking()
                .ToListAsync())
            .Where(predicate)
            .Select(menu => menu.Id)
            .ToList();

        var existingLinks = await db.MenuRoles
            .Where(link => link.RoleId == role.Id)
            .ToListAsync();

        var existingMenuIds = existingLinks
            .Select(link => link.MenuId)
            .ToHashSet();

        var linksToAdd = allowedMenuIds
            .Where(menuId => !existingMenuIds.Contains(menuId))
            .Select(menuId => new AspNetMenuRole
            {
                MenuId = menuId,
                RoleId = role.Id
            })
            .ToList();

        if (linksToAdd.Count == 0)
        {
            return;
        }

        db.MenuRoles.AddRange(linksToAdd);
        await db.SaveChangesAsync();
    }
}
