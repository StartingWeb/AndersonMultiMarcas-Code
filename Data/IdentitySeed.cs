using Domain.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Data;

public static class IdentitySeed
{
    public static async Task EnsureDeveloperUserAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<AspNetCoreUser>>();

        await EnsureRoleAsync(roleManager, "Desenvolvedor");
        await EnsureRoleAsync(roleManager, "Administrador");
        await EnsureRoleAsync(roleManager, "AdminConcessionaria");

        await EnsureUserAsync(
            userManager,
            roleName: "Desenvolvedor",
            userName: "desenvolvedor",
            email: "dev@stw.com",
            password: "123456@Senha",
            nomeCompleto: "Desenvolvedor");

        await EnsureUserAsync(
            userManager,
            roleName: "Administrador",
            userName: "admin.master",
            email: "admin@andersonmultimarcas.com.br",
            password: "123456@Admin",
            nomeCompleto: "Admin Master");
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
            return;

        var role = new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant()
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            throw new Exception(
                $"Erro ao criar perfil {roleName}: " +
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<AspNetCoreUser> userManager,
        string roleName,
        string userName,
        string email,
        string password,
        string nomeCompleto)
    {
        var user = await userManager.FindByEmailAsync(email) ?? await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new AspNetCoreUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                NomeCompleto = nomeCompleto
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new Exception(
                    $"Erro ao criar usuario {nomeCompleto}: " +
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );
            }
        }
        else
        {
            var changed = false;

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(user.NomeCompleto))
            {
                user.NomeCompleto = nomeCompleto;
                changed = true;
            }

            if (changed)
            {
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new Exception(
                        $"Erro ao atualizar usuario {nomeCompleto}: " +
                        string.Join(", ", updateResult.Errors.Select(e => e.Description))
                    );
                }
            }
        }

        if (await userManager.IsInRoleAsync(user, roleName))
            return;

        var roleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            throw new Exception(
                $"Erro ao vincular usuario {nomeCompleto} ao perfil {roleName}: " +
                string.Join(", ", roleResult.Errors.Select(e => e.Description))
            );
        }
    }
}
