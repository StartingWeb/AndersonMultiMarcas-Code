using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Persistence.Configurations;

public sealed class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("ImportJob");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).IsRequired().HasMaxLength(40);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.IniciadoEm);
        builder.Property(x => x.FinalizadoEm);
        builder.Property(x => x.CanceladoEm);
        builder.Property(x => x.UsuarioId).HasMaxLength(80);
        builder.Property(x => x.UsuarioNome).HasMaxLength(200);
        builder.Property(x => x.UrlBase).IsRequired().HasMaxLength(300);
        builder.Property(x => x.DryRun).IsRequired();
        builder.Property(x => x.SomenteSemBlobName).IsRequired();
        builder.Property(x => x.Sobrescrever).IsRequired();
        builder.Property(x => x.PreparacaoConcluida).IsRequired();
        builder.Property(x => x.IdInicial);
        builder.Property(x => x.QuantidadeMaxima);
        builder.Property(x => x.TotalVeiculos).IsRequired();
        builder.Property(x => x.VeiculosProcessados).IsRequired();
        builder.Property(x => x.TotalImagens).IsRequired();
        builder.Property(x => x.ImagensImportadas).IsRequired();
        builder.Property(x => x.ImagensIgnoradas).IsRequired();
        builder.Property(x => x.ImagensComErro).IsRequired();
        builder.Property(x => x.UltimaMensagem).HasMaxLength(1000);
        builder.Property(x => x.UltimaAtualizacaoEm);
        builder.Property(x => x.VeiculoAtualId);
        builder.Property(x => x.RelatorioConsolidadoJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.RelatorioGeradoEm);
        builder.Property(x => x.LockId).HasMaxLength(80);
        builder.Property(x => x.LockExpiraEm);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.Items)
            .WithOne(x => x.ImportJob)
            .HasForeignKey(x => x.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Logs)
            .WithOne(x => x.ImportJob)
            .HasForeignKey(x => x.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Historico)
            .WithOne(x => x.ImportJob)
            .HasForeignKey(x => x.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CriadoEm);
        builder.HasIndex(x => x.LockExpiraEm);
    }
}
