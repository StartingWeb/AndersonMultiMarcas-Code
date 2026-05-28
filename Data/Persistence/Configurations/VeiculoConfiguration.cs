using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Persistence.Configurations;

public sealed class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.ToTable("Veiculo");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LojaId).IsRequired();
        builder.Property(x => x.MarcaId).IsRequired();
        builder.Property(x => x.VendedorId);
        builder.Property(x => x.Titulo).IsRequired().HasMaxLength(180);
        builder.Property(x => x.Modelo).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Versao).HasMaxLength(150);
        builder.Property(x => x.AnoFabricacao);
        builder.Property(x => x.AnoModelo).IsRequired();
        builder.Property(x => x.Cor).HasMaxLength(60);
        builder.Property(x => x.Combustivel).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Cambio).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Quilometragem);
        builder.Property(x => x.Placa).HasMaxLength(10);
        builder.Property(x => x.PrecoVenda)
            .HasConversion(x => x.Valor, x => new Domain.ValueObjects.Dinheiro(x))
            .HasColumnType("decimal(18,2)");
        builder.Property(x => x.Descricao).HasMaxLength(4000);
        builder.Property(x => x.UrlVideo).HasMaxLength(400);
        builder.Property(x => x.ObservacoesInternas).HasMaxLength(2000);
        builder.Property(x => x.DataCadastro).IsRequired();
        builder.Property(x => x.DataAtualizacao);

        builder.HasOne(x => x.Loja)
            .WithMany(x => x.Veiculos)
            .HasForeignKey(x => x.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Marca)
            .WithMany(x => x.Veiculos)
            .HasForeignKey(x => x.MarcaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vendedor)
            .WithMany(x => x.Veiculos)
            .HasForeignKey(x => x.VendedorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Caracteristicas)
            .WithOne(x => x.Veiculo)
            .HasForeignKey<VeiculoCaracteristica>(x => x.VeiculoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Midias)
            .WithOne(x => x.Veiculo)
            .HasForeignKey(x => x.VeiculoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.LojaId, x.Ativo, x.Vendido, x.Destaque });
        builder.HasIndex(x => new { x.MarcaId, x.Modelo, x.AnoModelo });
        builder.HasIndex(x => x.Placa);
    }
}
