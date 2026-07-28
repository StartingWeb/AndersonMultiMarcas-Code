using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Persistence.Configurations;

public sealed class ImportJobItemConfiguration : IEntityTypeConfiguration<ImportJobItem>
{
    public void Configure(EntityTypeBuilder<ImportJobItem> builder)
    {
        builder.ToTable("ImportJobItem");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImportJobId).IsRequired();
        builder.Property(x => x.VeiculoId).IsRequired();
        builder.Property(x => x.VeiculoMidiaId);
        builder.Property(x => x.Ordem).IsRequired();
        builder.Property(x => x.Capa).IsRequired();
        builder.Property(x => x.UrlLegada).IsRequired().HasMaxLength(800);
        builder.Property(x => x.NomeArquivoDestino).IsRequired().HasMaxLength(250);
        builder.Property(x => x.BlobNameDestino).IsRequired().HasMaxLength(300);
        builder.Property(x => x.ContainerDestino).HasMaxLength(250);
        builder.Property(x => x.UrlDestino).HasMaxLength(800);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(40);
        builder.Property(x => x.Tentativas).IsRequired();
        builder.Property(x => x.MaxTentativas).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120);
        builder.Property(x => x.TamanhoBytes);
        builder.Property(x => x.Erro).HasMaxLength(2000);
        builder.Property(x => x.IniciadoEm);
        builder.Property(x => x.FinalizadoEm);
        builder.Property(x => x.LockId).HasMaxLength(80);
        builder.Property(x => x.LockExpiraEm);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Veiculo)
            .WithMany()
            .HasForeignKey(x => x.VeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VeiculoMidia)
            .WithMany()
            .HasForeignKey(x => x.VeiculoMidiaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Logs)
            .WithOne(x => x.ImportJobItem)
            .HasForeignKey(x => x.ImportJobItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.ImportJobId, x.Status });
        builder.HasIndex(x => new { x.ImportJobId, x.VeiculoId });
        builder.HasIndex(x => new { x.ImportJobId, x.BlobNameDestino }).IsUnique();
        builder.HasIndex(x => new { x.ImportJobId, x.UrlLegada }).IsUnique();
    }
}
