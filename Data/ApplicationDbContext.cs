using Data.Persistence.Configurations;
using Domain.Application;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data;

public class ApplicationDbContext : IdentityDbContext<AspNetCoreUser, IdentityRole<Guid>, Guid>
{
    public DbSet<Loja> Lojas => Set<Loja>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<VeiculoCaracteristica> VeiculoCaracteristicas => Set<VeiculoCaracteristica>();
    public DbSet<VeiculoMidia> VeiculoMidias => Set<VeiculoMidia>();
    public DbSet<Vendedor> Vendedores => Set<Vendedor>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
