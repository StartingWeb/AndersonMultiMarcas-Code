using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Persistence.Configurations;

public sealed class VeiculoCaracteristicaConfiguration : IEntityTypeConfiguration<VeiculoCaracteristica>
{
    public void Configure(EntityTypeBuilder<VeiculoCaracteristica> builder)
    {
        builder.ToTable("VeiculoCaracteristica");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VeiculoId).IsRequired();

        builder.Property(x => x.ArCondicionado).HasColumnType("bit").IsRequired();
        builder.Property(x => x.ArQuente).HasColumnType("bit").IsRequired();
        builder.Property(x => x.DirecaoHidraulica).HasColumnType("bit").IsRequired();
        builder.Property(x => x.DirecaoEletrica).HasColumnType("bit").IsRequired();
        builder.Property(x => x.VidroEletrico).HasColumnType("bit").IsRequired();
        builder.Property(x => x.TravaEletrica).HasColumnType("bit").IsRequired();
        builder.Property(x => x.RetrovisorEletrico).HasColumnType("bit").IsRequired();
        builder.Property(x => x.BancoDeCouro).HasColumnType("bit").IsRequired();
        builder.Property(x => x.AjusteEletricoBancos).HasColumnType("bit").IsRequired();
        builder.Property(x => x.AquecimentoBancos).HasColumnType("bit").IsRequired();
        builder.Property(x => x.VolanteMultifuncional).HasColumnType("bit").IsRequired();
        builder.Property(x => x.PilotoAutomatico).HasColumnType("bit").IsRequired();
        builder.Property(x => x.ControleAutomaticoVelocidade).HasColumnType("bit").IsRequired();
        builder.Property(x => x.LimitadorVelocidade).HasColumnType("bit").IsRequired();
        builder.Property(x => x.ComputadorBordo).HasColumnType("bit").IsRequired();
        builder.Property(x => x.ChavePresencial).HasColumnType("bit").IsRequired();
        builder.Property(x => x.PartidaBotao).HasColumnType("bit").IsRequired();
        builder.Property(x => x.SensorChuva).HasColumnType("bit").IsRequired();
        builder.Property(x => x.SensorCrepuscular).HasColumnType("bit").IsRequired();
        builder.Property(x => x.TetoSolar).HasColumnType("bit").IsRequired();
        builder.Property(x => x.TetoPanoramico).HasColumnType("bit").IsRequired();
        builder.Property(x => x.AirbagMotorista).HasColumnType("bit").IsRequired();
        builder.Property(x => x.AirbagPassageiro).HasColumnType("bit").IsRequired();
        builder.Property(x => x.AirbagLateral).HasColumnType("bit").IsRequired();
        builder.Property(x => x.AirbagCortina).HasColumnType("bit").IsRequired();
        builder.Property(x => x.FreiosAbs).HasColumnType("bit").IsRequired();
        builder.Property(x => x.ControleTracao).HasColumnType("bit").IsRequired();
        builder.Property(x => x.ControleEstabilidade).HasColumnType("bit").IsRequired();
        builder.Property(x => x.AssistentePartidaRampa).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Isofix).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Alarme).HasColumnType("bit").IsRequired();
        builder.Property(x => x.CameraDeRe).HasColumnType("bit").IsRequired();
        builder.Property(x => x.SensorEstacionamentoDianteiro).HasColumnType("bit").IsRequired();
        builder.Property(x => x.SensorEstacionamentoTraseiro).HasColumnType("bit").IsRequired();
        builder.Property(x => x.FarolNeblina).HasColumnType("bit").IsRequired();
        builder.Property(x => x.FarolLed).HasColumnType("bit").IsRequired();
        builder.Property(x => x.FarolMilha).HasColumnType("bit").IsRequired();
        builder.Property(x => x.CentralMultimidia).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Som).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Bluetooth).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Usb).HasColumnType("bit").IsRequired();
        builder.Property(x => x.EntradaAuxiliar).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Radio).HasColumnType("bit").IsRequired();
        builder.Property(x => x.GPS).HasColumnType("bit").IsRequired();
        builder.Property(x => x.CarregadorInducao).HasColumnType("bit").IsRequired();
        builder.Property(x => x.AppleCarPlay).HasColumnType("bit").IsRequired();
        builder.Property(x => x.AndroidAuto).HasColumnType("bit").IsRequired();
        builder.Property(x => x.RodaLigaLeve).HasColumnType("bit").IsRequired();
        builder.Property(x => x.KitMultimidia).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Engate).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Bagageiro).HasColumnType("bit").IsRequired();
        builder.Property(x => x.CapotaMaritima).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Estribo).HasColumnType("bit").IsRequired();
        builder.Property(x => x.SantoAntonio).HasColumnType("bit").IsRequired();
        builder.Property(x => x.ProtetorCacamba).HasColumnType("bit").IsRequired();
        builder.Property(x => x.PortaMalasEletrico).HasColumnType("bit").IsRequired();
        builder.Property(x => x.TerceiraFileira).HasColumnType("bit").IsRequired();
        builder.Property(x => x.CambioAutomatico).HasColumnType("bit").IsRequired();
        builder.Property(x => x.CambioManual).HasColumnType("bit").IsRequired();
        builder.Property(x => x.CambioCvt).HasColumnType("bit").IsRequired();
        builder.Property(x => x.CambioAutomatizado).HasColumnType("bit").IsRequired();
        builder.Property(x => x.TracaoDianteira).HasColumnType("bit").IsRequired();
        builder.Property(x => x.TracaoTraseira).HasColumnType("bit").IsRequired();
        builder.Property(x => x.TracaoIntegral).HasColumnType("bit").IsRequired();
        builder.Property(x => x.StartStop).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Turbo).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Hibrido).HasColumnType("bit").IsRequired();
        builder.Property(x => x.Eletrico).HasColumnType("bit").IsRequired();

        builder.Property(x => x.Ativo).HasColumnType("bit").IsRequired();
        builder.Property(x => x.DataCadastro).IsRequired();

        builder.HasIndex(x => x.VeiculoId).IsUnique();
    }
}
