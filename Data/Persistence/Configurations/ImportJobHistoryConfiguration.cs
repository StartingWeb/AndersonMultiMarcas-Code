using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Persistence.Configurations;

public sealed class ImportJobHistoryConfiguration : IEntityTypeConfiguration<ImportJobHistory>
{
    public void Configure(EntityTypeBuilder<ImportJobHistory> builder)
    {
        builder.ToTable("ImportJobHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImportJobId).IsRequired();
        builder.Property(x => x.Tipo).IsRequired().HasMaxLength(80);
        builder.Property(x => x.UsuarioId).HasMaxLength(80);
        builder.Property(x => x.UsuarioNome).HasMaxLength(200);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.Quantidade);
        builder.Property(x => x.DuracaoMs);
        builder.Property(x => x.Resultado).HasMaxLength(120);
        builder.Property(x => x.Mensagem).HasMaxLength(2000);

        builder.HasIndex(x => new { x.ImportJobId, x.CriadoEm });
        builder.HasIndex(x => new { x.Tipo, x.CriadoEm });
    }
}
