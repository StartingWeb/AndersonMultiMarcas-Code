using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Persistence.Configurations;

public sealed class ImportJobLogConfiguration : IEntityTypeConfiguration<ImportJobLog>
{
    public void Configure(EntityTypeBuilder<ImportJobLog> builder)
    {
        builder.ToTable("ImportJobLog");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImportJobId).IsRequired();
        builder.Property(x => x.ImportJobItemId);
        builder.Property(x => x.VeiculoId);
        builder.Property(x => x.ImagemOrdem);
        builder.Property(x => x.UrlLegada).HasMaxLength(800);
        builder.Property(x => x.Etapa).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Mensagem).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.CriadoEm).IsRequired();

        builder.HasIndex(x => new { x.ImportJobId, x.Id });
        builder.HasIndex(x => new { x.ImportJobId, x.CriadoEm });
    }
}
