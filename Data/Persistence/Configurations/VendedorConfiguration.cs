using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Persistence.Configurations;

public sealed class VendedorConfiguration : IEntityTypeConfiguration<Vendedor>
{
    public void Configure(EntityTypeBuilder<Vendedor> builder)
    {
        builder.ToTable("Vendedor");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LojaId).IsRequired();
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(150);
        builder.Property(x => x.FotoUrl).HasMaxLength(400);
        builder.Property(x => x.Cargo).HasMaxLength(120);
        builder.Property(x => x.Ativo).IsRequired();
        builder.Property(x => x.DataCadastro).IsRequired();

        builder.Property(x => x.Email)
            .HasConversion(
                x => x.HasValue ? x.Value.Valor : null,
                x => string.IsNullOrWhiteSpace(x) ? null : new Email(x))
            .HasColumnName("Email")
            .HasMaxLength(180);

        builder.Property(x => x.Telefone)
            .HasConversion(
                x => x.HasValue ? x.Value.Valor : null,
                x => string.IsNullOrWhiteSpace(x) ? null : new Telefone(x))
            .HasColumnName("Telefone")
            .HasMaxLength(20);

        builder.Property(x => x.Whatsapp)
            .HasConversion(
                x => x.HasValue ? x.Value.Valor : null,
                x => string.IsNullOrWhiteSpace(x) ? null : new Telefone(x))
            .HasColumnName("Whatsapp")
            .HasMaxLength(20);

        builder.Property(x => x.Cpf)
            .HasConversion(
                x => x.HasValue ? x.Value.Valor : null,
                x => string.IsNullOrWhiteSpace(x) ? null : new Documento(x))
            .HasColumnName("Cpf")
            .HasMaxLength(11);

        builder.HasOne(x => x.Loja)
            .WithMany(x => x.Vendedores)
            .HasForeignKey(x => x.LojaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.LojaId, x.Nome });
    }
}
