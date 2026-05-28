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

        const string roleName = "Desenvolvedor";
        const string devEmail = "dev@stw.com";
        const string devUser = "desenvolvedor";
        const string devPassword = "123456@Senha";

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var role = new IdentityRole<Guid> { Name = roleName, NormalizedName = roleName.ToUpperInvariant() };
            await roleManager.CreateAsync(role);
        }

        var usersInRole = await userManager.GetUsersInRoleAsync(roleName);
        if (usersInRole.Any())
            return;

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
                "Erro ao criar usuario Desenvolvedor: " +
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
        }

        await userManager.AddToRoleAsync(user, roleName);
    }
}
