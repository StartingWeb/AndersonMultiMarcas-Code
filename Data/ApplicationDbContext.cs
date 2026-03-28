using Domain;
using Domain.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data;

public class ApplicationDbContext
    : IdentityDbContext<AspNetCoreUser, AspNetCoreRole, Guid,
        IdentityUserClaim<Guid>,
        AspNetCoreUserRoles,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Loja> Lojas { get; set; }
    public DbSet<Marca> Marcas { get; set; }
    public DbSet<Vendedor> Vendedores { get; set; }
    public DbSet<Veiculo> Veiculos { get; set; }
    public DbSet<VeiculoCaracteristica> VeiculoCaracteristicas { get; set; }
    public DbSet<VeiculoMidia> VeiculoMidias { get; set; }
    public DbSet<AspNetMenu> Menus { get; set; }
    public DbSet<AspNetMenuRole> MenuRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        BuildLoja(builder);
        BuildMarca(builder);
        BuildVendedor(builder);
        BuildVeiculo(builder);
        BuildVeiculoCaracteristica(builder);
        BuildVeiculoMidia(builder);
        BuildRole(builder);
        BuildMenu(builder);
        BuildMenuRole(builder);
    }

    private void BuildRole(ModelBuilder builder)
    {
        builder.Entity<AspNetCoreRole>(entity =>
        {
            entity.Property(e => e.Descricao)
                .HasMaxLength(250);
        });
    }

    private void BuildLoja(ModelBuilder builder)
    {
        builder.Entity<Loja>(entity =>
        {
            entity.ToTable("Loja");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.RazaoSocial)
                .HasMaxLength(200);

            entity.Property(e => e.Cnpj)
                .HasMaxLength(20);

            entity.Property(e => e.Email)
                .HasMaxLength(150);

            entity.Property(e => e.Telefone)
                .HasMaxLength(20);

            entity.Property(e => e.Endereco)
                .HasMaxLength(200);

            entity.Property(e => e.Numero)
                .HasMaxLength(20);

            entity.Property(e => e.Complemento)
                .HasMaxLength(100);

            entity.Property(e => e.Bairro)
                .HasMaxLength(100);

            entity.Property(e => e.Cidade)
                .HasMaxLength(100);

            entity.Property(e => e.Uf)
                .HasMaxLength(2);

            entity.Property(e => e.Cep)
                .HasMaxLength(10);

            entity.Property(e => e.Ativo)
                .HasDefaultValue(true);

            entity.Property(e => e.DataCadastro)
                .HasDefaultValueSql("GETDATE()");
        });
    }

    private void BuildMarca(ModelBuilder builder)
    {
        builder.Entity<Marca>(entity =>
        {
            entity.ToTable("Marca");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LogoUrl)
                .HasMaxLength(255);

            entity.Property(e => e.Ativo)
                .HasDefaultValue(true);

            entity.Property(e => e.DataCadastro)
                .HasDefaultValueSql("GETDATE()");
        });
    }

    private void BuildVendedor(ModelBuilder builder)
    {
        builder.Entity<Vendedor>(entity =>
        {
            entity.ToTable("Vendedor");

            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Loja)
                .WithMany()
                .HasForeignKey(e => e.LojaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Email)
                .HasMaxLength(150);

            entity.Property(e => e.Telefone)
                .HasMaxLength(20);

            entity.Property(e => e.Whatsapp)
                .HasMaxLength(20);

            entity.Property(e => e.Cpf)
                .HasMaxLength(20);

            entity.Property(e => e.FotoUrl)
                .HasMaxLength(255);

            entity.Property(e => e.Cargo)
                .HasMaxLength(100);

            entity.Property(e => e.Ativo)
                .HasDefaultValue(true);

            entity.Property(e => e.DataCadastro)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(e => e.LojaId);
        });
    }

    private void BuildVeiculo(ModelBuilder builder)
    {
        builder.Entity<Veiculo>(entity =>
        {
            entity.ToTable("Veiculo");

            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Loja)
                .WithMany()
                .HasForeignKey(e => e.LojaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Marca)
                .WithMany()
                .HasForeignKey(e => e.MarcaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Vendedor)
                .WithMany()
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.VendidoPorUsuario)
                .WithMany()
                .HasForeignKey(e => e.VendidoPorUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Titulo)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Modelo)
                .HasMaxLength(100);

            entity.Property(e => e.Versao)
                .HasMaxLength(100);

            entity.Property(e => e.Cor)
                .HasMaxLength(30);

            entity.Property(e => e.Combustivel)
                .HasMaxLength(30);

            entity.Property(e => e.Cambio)
                .HasMaxLength(30);

            entity.Property(e => e.Placa)
                .HasMaxLength(20);

            entity.Property(e => e.Chassi)
                .HasMaxLength(50);

            entity.Property(e => e.Renavam)
                .HasMaxLength(50);

            entity.Property(e => e.PrecoVenda)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.PrecoPromocional)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.PrecoFipe)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Descricao)
                .HasMaxLength(1000);

            entity.Property(e => e.UrlVideo)
                .HasMaxLength(255);

            entity.Property(e => e.ObservacoesInternas)
                .HasMaxLength(1000);

            entity.Property(e => e.AceitaTroca)
                .HasDefaultValue(false);

            entity.Property(e => e.Financiavel)
                .HasDefaultValue(false);

            entity.Property(e => e.Destaque)
                .HasDefaultValue(false);

            entity.Property(e => e.Seminovo)
                .HasDefaultValue(false);

            entity.Property(e => e.Ativo)
                .HasDefaultValue(true);

            entity.Property(e => e.Vendido)
                .HasDefaultValue(false);

            entity.Property(e => e.DataCadastro)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(e => e.LojaId);
            entity.HasIndex(e => e.MarcaId);
            entity.HasIndex(e => e.VendedorId);
            entity.HasIndex(e => e.VendidoPorUsuarioId);
            entity.HasIndex(e => e.Ativo);
            entity.HasIndex(e => e.Vendido);
            entity.HasIndex(e => e.Destaque);
        });
    }

    private void BuildVeiculoCaracteristica(ModelBuilder builder)
    {
        builder.Entity<VeiculoCaracteristica>(entity =>
        {
            entity.ToTable("VeiculoCaracteristica");

            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Veiculo)
                .WithOne(v => v.Caracteristica)
                .HasForeignKey<VeiculoCaracteristica>(e => e.VeiculoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.VeiculoId)
                .IsUnique();

            entity.Property(e => e.ArCondicionado).HasDefaultValue(false);
            entity.Property(e => e.ArQuente).HasDefaultValue(false);
            entity.Property(e => e.DirecaoHidraulica).HasDefaultValue(false);
            entity.Property(e => e.DirecaoEletrica).HasDefaultValue(false);
            entity.Property(e => e.VidroEletrico).HasDefaultValue(false);
            entity.Property(e => e.TravaEletrica).HasDefaultValue(false);
            entity.Property(e => e.RetrovisorEletrico).HasDefaultValue(false);
            entity.Property(e => e.BancoDeCouro).HasDefaultValue(false);
            entity.Property(e => e.AjusteEletricoBancos).HasDefaultValue(false);
            entity.Property(e => e.AquecimentoBancos).HasDefaultValue(false);
        });
    }

    private void BuildVeiculoMidia(ModelBuilder builder)
    {
        builder.Entity<VeiculoMidia>(entity =>
        {
            entity.ToTable("VeiculoMidia");

            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Veiculo)
                .WithMany(v => v.Midias)
                .HasForeignKey(e => e.VeiculoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.NomeArquivo)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Url)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.BlobName)
                .HasMaxLength(200);

            entity.Property(e => e.Container)
                .HasMaxLength(100);

            entity.Property(e => e.Tipo)
                .HasMaxLength(20);

            entity.Property(e => e.ContentType)
                .HasMaxLength(100);

            entity.Property(e => e.Capa)
                .HasDefaultValue(false);

            entity.Property(e => e.Ordem)
                .HasDefaultValue(0);

            entity.Property(e => e.Ativo)
                .HasDefaultValue(true);

            entity.Property(e => e.DataCadastro)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(e => e.VeiculoId);

            entity.HasIndex(e => new { e.VeiculoId, e.Ordem });

            entity.HasIndex(e => new { e.VeiculoId, e.Capa })
                .HasFilter("[Capa] = 1")
                .IsUnique();
        });
    }

    private void BuildMenu(ModelBuilder builder)
    {
        builder.Entity<AspNetMenu>(entity =>
        {
            entity.ToTable("AspNetMenus");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(e => e.Descricao)
                .HasMaxLength(250);

            entity.Property(e => e.Icone)
                .HasMaxLength(80);

            entity.Property(e => e.Url)
                .HasMaxLength(200);

            entity.Property(e => e.Ativo)
                .HasDefaultValue(true);

            entity.HasOne(e => e.MenuPai)
                .WithMany(e => e.SubMenus)
                .HasForeignKey(e => e.MenuPaiId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.MenuPaiId, e.Ordem });
        });
    }

    private void BuildMenuRole(ModelBuilder builder)
    {
        builder.Entity<AspNetMenuRole>(entity =>
        {
            entity.ToTable("AspNetMenuRoles");

            entity.HasKey(e => new { e.MenuId, e.RoleId });

            entity.HasOne(e => e.Menu)
                .WithMany(e => e.MenuRoles)
                .HasForeignKey(e => e.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
