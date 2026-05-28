using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Persistence.Configurations;

public sealed class MarcaConfiguration : IEntityTypeConfiguration<Marca>
{
    public void Configure(EntityTypeBuilder<Marca> builder)
    {
        builder.ToTable("Marca");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome).IsRequired().HasMaxLength(100);
        builder.Property(x => x.LogoUrl).HasMaxLength(400);
        builder.Property(x => x.Ativo).IsRequired();
        builder.Property(x => x.DataCadastro).IsRequired();

        builder.HasIndex(x => x.Nome).IsUnique();
    }
}
