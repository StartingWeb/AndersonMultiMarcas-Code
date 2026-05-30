using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Globalization;
using System.Text;

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
        builder.Property(x => x.Combustivel)
            .HasConversion(
                x => CombustivelToDb(x),
                x => CombustivelFromDb(x))
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.Cambio)
            .HasConversion(
                x => CambioToDb(x),
                x => CambioFromDb(x))
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.Quilometragem);
        builder.Property(x => x.Placa).HasMaxLength(10);
        builder.Property(x => x.PrecoVenda)
            .HasConversion(x => x.Valor, x => new Domain.ValueObjects.Dinheiro(x))
            .HasColumnType("decimal(18,2)");
        builder.Property(x => x.Descricao).HasMaxLength(4000);
        builder.Property(x => x.UrlVideo).HasMaxLength(400);
        builder.Property(x => x.ObservacoesInternas).HasMaxLength(2000);
        builder.Property(x => x.QuantidadeCliques).HasDefaultValue(0);
        builder.Property(x => x.QuantidadeVisualizacoes).HasDefaultValue(0);
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

    private static Cambio CambioFromDb(string? value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "manual" => Cambio.Manual,
            "automatico" => Cambio.Automatico,
            "auto" => Cambio.Automatico,
            "cvt" => Cambio.Cvt,
            "automatizado" => Cambio.Automatizado,
            _ => Cambio.NaoInformado
        };
    }

    private static string CambioToDb(Cambio value) => value switch
    {
        Cambio.Manual => "Manual",
        Cambio.Automatico => "Automático",
        Cambio.Cvt => "CVT",
        Cambio.Automatizado => "Automatizado",
        _ => "Não informado"
    };

    private static Combustivel CombustivelFromDb(string? value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "gasolina" => Combustivel.Gasolina,
            "alcool" => Combustivel.Etanol,
            "etanol" => Combustivel.Etanol,
            "flex" => Combustivel.Flex,
            "diesel" => Combustivel.Diesel,
            "gnv" => Combustivel.Gnv,
            "hibrido" => Combustivel.Hibrido,
            "eletrico" => Combustivel.Eletrico,
            _ => Combustivel.NaoInformado
        };
    }

    private static string CombustivelToDb(Combustivel value) => value switch
    {
        Combustivel.Gasolina => "Gasolina",
        Combustivel.Etanol => "Álcool",
        Combustivel.Flex => "Flex",
        Combustivel.Diesel => "Diesel",
        Combustivel.Gnv => "GNV",
        Combustivel.Hibrido => "Híbrido",
        Combustivel.Eletrico => "Elétrico",
        _ => "Não informado"
    };

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
