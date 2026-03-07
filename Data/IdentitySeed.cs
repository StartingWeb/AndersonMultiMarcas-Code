using Domain.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data;

public static class IdentitySeed
{
    public static async Task EnsureDeveloperUserAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AspNetCoreUser>>();

        const string roleName = "Desenvolvedor";
        const string devEmail = "dev@stw.com";
        const string devUser = "desenvolvedor";
        const string devPassword = "123456@Senha"; // depois você troca

        // 1️⃣ Garante a Role
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        // 2️⃣ Verifica se já existe algum usuário Desenvolvedor
        var usersInRole = await userManager.GetUsersInRoleAsync(roleName);
        if (usersInRole.Any())
            return; // já existe, não cria outro

        // 3️⃣ Cria o usuário Desenvolvedor
        var user = new AspNetCoreUser
        {
            UserName = devUser,
            Email = devEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, devPassword);

        if (!result.Succeeded)
        {
            throw new Exception(
                "Erro ao criar usuário Desenvolvedor: " +
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
        }

        // 4️⃣ Vincula à role
        await userManager.AddToRoleAsync(user, roleName);
    }
}
