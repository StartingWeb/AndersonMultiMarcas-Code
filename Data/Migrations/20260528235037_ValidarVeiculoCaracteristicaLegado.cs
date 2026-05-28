using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class ValidarVeiculoCaracteristicaLegado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var colunasBooleanas = new[]
            {
                "AirbagCortina","AirbagLateral","AirbagMotorista","AirbagPassageiro","AjusteEletricoBancos","Alarme",
                "AquecimentoBancos","AssistentePartidaRampa","Bagageiro","CambioAutomatizado","CambioCvt","CambioManual",
                "CameraDeRe","CapotaMaritima","CarregadorInducao","ChavePresencial","ComputadorBordo",
                "ControleAutomaticoVelocidade","ControleEstabilidade","ControleTracao","Engate","EntradaAuxiliar","Estribo",
                "FarolLed","FarolMilha","FarolNeblina","FreiosAbs","GPS","Isofix","KitMultimidia","LimitadorVelocidade",
                "PartidaBotao","PilotoAutomatico","PortaMalasEletrico","ProtetorCacamba","Radio","RodaLigaLeve","SantoAntonio",
                "SensorChuva","SensorCrepuscular","SensorEstacionamentoDianteiro","SensorEstacionamentoTraseiro","Som","StartStop",
                "TerceiraFileira","TetoPanoramico","TetoSolar","TracaoDianteira","TracaoIntegral","TracaoTraseira","Usb",
                "VolanteMultifuncional"
            };

            foreach (var coluna in colunasBooleanas)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
                       AND COL_LENGTH('VeiculoCaracteristica', '{coluna}') IS NULL
                    BEGIN
                        ALTER TABLE [VeiculoCaracteristica]
                        ADD [{coluna}] bit NOT NULL
                            CONSTRAINT [DF_VeiculoCaracteristica_{coluna}] DEFAULT 0;
                    END;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intencionalmente vazio: migration de compatibilizacao, sem operacoes destrutivas.
        }
    }
}
