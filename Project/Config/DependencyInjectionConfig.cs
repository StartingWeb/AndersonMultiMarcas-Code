using Core;
using Core.Interfaces;
using Core.Services;
using Data;
using Domain.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Navigation;
using Project.Services;

namespace Project.Config;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddProjectDependencies(this IServiceCollection services)
    {
        services.AddScoped<ILojaService, LojaService>();
        services.AddScoped<IMarcaService, MarcaService>();
        services.AddScoped<IVendedorService, VendedorService>();
        services.AddScoped<IVeiculoService, VeiculoService>();
        services.AddScoped<IVeiculoCaracteristicaService, VeiculoCaracteristicaService>();
        services.AddScoped<IVeiculoMidiaService, VeiculoMidiaService>();
        services.AddScoped<IEstoqueConferenciaExcelService, EstoqueConferenciaExcelService>();
        services.AddScoped<IAdminMenuService, AdminMenuService>();
        return services;
    }

}

public static class IdentityDependencyInjection
{
    public static IServiceCollection AddIdentityDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentity<AspNetCoreUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
