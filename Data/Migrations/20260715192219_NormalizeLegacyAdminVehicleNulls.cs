using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeLegacyAdminVehicleNulls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var veiculoBitColumns = new[]
            {
                "AceitaTroca",
                "Financiavel",
                "Destaque",
                "Seminovo",
                "Vendido",
                "ImportadoMidia",
                "MotoEletrica"
            };

            foreach (var column in veiculoBitColumns)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'[Veiculo]', N'U') IS NOT NULL
                       AND COL_LENGTH('Veiculo', '{column}') IS NULL
                    BEGIN
                        ALTER TABLE [Veiculo]
                        ADD [{column}] bit NOT NULL
                            CONSTRAINT [DF_Veiculo_{column}] DEFAULT 0;
                    END;
                    """);
            }

            var veiculoIntColumns = new[]
            {
                "QuantidadeCliques",
                "QuantidadeVisualizacoes"
            };

            foreach (var column in veiculoIntColumns)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'[Veiculo]', N'U') IS NOT NULL
                       AND COL_LENGTH('Veiculo', '{column}') IS NULL
                    BEGIN
                        ALTER TABLE [Veiculo]
                        ADD [{column}] int NOT NULL
                            CONSTRAINT [DF_Veiculo_{column}] DEFAULT 0;
                    END;
                    """);
            }

            var auditedTables = new[]
            {
                "Veiculo",
                "VeiculoMidia",
                "VeiculoCaracteristica"
            };

            foreach (var table in auditedTables)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'[{table}]', N'U') IS NOT NULL
                       AND COL_LENGTH('{table}', 'Ativo') IS NULL
                    BEGIN
                        ALTER TABLE [{table}]
                        ADD [Ativo] bit NOT NULL
                            CONSTRAINT [DF_{table}_Ativo_AdminNormalize] DEFAULT 1;
                    END;

                    IF OBJECT_ID(N'[{table}]', N'U') IS NOT NULL
                       AND COL_LENGTH('{table}', 'DataCadastro') IS NULL
                    BEGIN
                        ALTER TABLE [{table}]
                        ADD [DataCadastro] datetime2 NOT NULL
                            CONSTRAINT [DF_{table}_DataCadastro_AdminNormalize] DEFAULT SYSUTCDATETIME();
                    END;
                    """);
            }

            var caracteristicaBitColumns = new[]
            {
                "ArCondicionado", "ArQuente", "DirecaoHidraulica", "DirecaoEletrica", "VidroEletrico",
                "TravaEletrica", "RetrovisorEletrico", "BancoDeCouro", "AjusteEletricoBancos",
                "AquecimentoBancos", "VolanteMultifuncional", "PilotoAutomatico",
                "ControleAutomaticoVelocidade", "LimitadorVelocidade", "ComputadorBordo",
                "ChavePresencial", "PartidaBotao", "SensorChuva", "SensorCrepuscular", "TetoSolar",
                "TetoPanoramico", "AirbagMotorista", "AirbagPassageiro", "AirbagLateral",
                "AirbagCortina", "FreiosAbs", "ControleTracao", "ControleEstabilidade",
                "AssistentePartidaRampa", "Isofix", "Alarme", "CameraDeRe",
                "SensorEstacionamentoDianteiro", "SensorEstacionamentoTraseiro", "FarolNeblina",
                "FarolLed", "FarolMilha", "CentralMultimidia", "Som", "Bluetooth", "Usb",
                "EntradaAuxiliar", "Radio", "GPS", "CarregadorInducao", "AppleCarPlay",
                "AndroidAuto", "RodaLigaLeve", "KitMultimidia", "Engate", "Bagageiro",
                "CapotaMaritima", "Estribo", "SantoAntonio", "ProtetorCacamba",
                "PortaMalasEletrico", "TerceiraFileira", "CambioAutomatico", "CambioManual",
                "CambioCvt", "CambioAutomatizado", "TracaoDianteira", "TracaoTraseira",
                "TracaoIntegral", "StartStop", "Turbo", "Hibrido", "Eletrico"
            };

            foreach (var column in caracteristicaBitColumns)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
                       AND COL_LENGTH('VeiculoCaracteristica', '{column}') IS NULL
                    BEGIN
                        ALTER TABLE [VeiculoCaracteristica]
                        ADD [{column}] bit NOT NULL
                            CONSTRAINT [DF_VeiculoCaracteristica_{column}] DEFAULT 0;
                    END;
                    """);
            }

            migrationBuilder.Sql(
                """
                SET QUOTED_IDENTIFIER ON;
                SET ANSI_NULLS ON;

                IF OBJECT_ID(N'[Veiculo]', N'U') IS NOT NULL
                BEGIN
                    DECLARE @FallbackLojaId int = (SELECT TOP 1 [Id] FROM [Loja] ORDER BY [Id]);
                    DECLARE @FallbackMarcaId int = (SELECT TOP 1 [Id] FROM [Marca] ORDER BY [Id]);

                    UPDATE [Veiculo]
                    SET
                        [LojaId] = COALESCE([LojaId], @FallbackLojaId),
                        [MarcaId] = COALESCE([MarcaId], @FallbackMarcaId),
                        [Titulo] = COALESCE(NULLIF(LTRIM(RTRIM([Titulo])), ''), COALESCE(NULLIF(LTRIM(RTRIM([Modelo])), ''), CONCAT('Veículo ', [Id]))),
                        [Modelo] = COALESCE(NULLIF(LTRIM(RTRIM([Modelo])), ''), COALESCE(NULLIF(LTRIM(RTRIM([Titulo])), ''), CONCAT('Veículo ', [Id]))),
                        [AnoModelo] = COALESCE([AnoModelo], YEAR(GETDATE())),
                        [Combustivel] = COALESCE(NULLIF(LTRIM(RTRIM([Combustivel])), ''), 'NaoInformado'),
                        [Cambio] = COALESCE(NULLIF(LTRIM(RTRIM([Cambio])), ''), 'NaoInformado'),
                        [PrecoVenda] = COALESCE([PrecoVenda], 0),
                        [AceitaTroca] = COALESCE([AceitaTroca], 0),
                        [Financiavel] = COALESCE([Financiavel], 0),
                        [Destaque] = COALESCE([Destaque], 0),
                        [Seminovo] = COALESCE([Seminovo], 0),
                        [Vendido] = COALESCE([Vendido], 0),
                        [ImportadoMidia] = COALESCE([ImportadoMidia], 0),
                        [MotoEletrica] = COALESCE([MotoEletrica], 0),
                        [QuantidadeCliques] = COALESCE([QuantidadeCliques], 0),
                        [QuantidadeVisualizacoes] = COALESCE([QuantidadeVisualizacoes], 0),
                        [Ativo] = COALESCE([Ativo], 1),
                        [DataCadastro] = COALESCE([DataCadastro], SYSUTCDATETIME())
                    WHERE
                        [LojaId] IS NULL
                        OR [MarcaId] IS NULL
                        OR [Titulo] IS NULL OR LTRIM(RTRIM([Titulo])) = ''
                        OR [Modelo] IS NULL OR LTRIM(RTRIM([Modelo])) = ''
                        OR [AnoModelo] IS NULL
                        OR [Combustivel] IS NULL OR LTRIM(RTRIM([Combustivel])) = ''
                        OR [Cambio] IS NULL OR LTRIM(RTRIM([Cambio])) = ''
                        OR [PrecoVenda] IS NULL
                        OR [AceitaTroca] IS NULL
                        OR [Financiavel] IS NULL
                        OR [Destaque] IS NULL
                        OR [Seminovo] IS NULL
                        OR [Vendido] IS NULL
                        OR [ImportadoMidia] IS NULL
                        OR [MotoEletrica] IS NULL
                        OR [QuantidadeCliques] IS NULL
                        OR [QuantidadeVisualizacoes] IS NULL
                        OR [Ativo] IS NULL
                        OR [DataCadastro] IS NULL;
                END;

                IF OBJECT_ID(N'[VeiculoMidia]', N'U') IS NOT NULL
                BEGIN
                    UPDATE [VeiculoMidia]
                    SET
                        [NomeArquivo] = COALESCE(NULLIF(LTRIM(RTRIM([NomeArquivo])), ''), CONCAT('midia-', [Id])),
                        [Url] = COALESCE(NULLIF(LTRIM(RTRIM([Url])), ''), '/img/carroDefault.png'),
                        [Tipo] = COALESCE(NULLIF(LTRIM(RTRIM([Tipo])), ''), 'Imagem'),
                        [Capa] = COALESCE([Capa], 0),
                        [Ordem] = COALESCE([Ordem], 0),
                        [Ativo] = COALESCE([Ativo], 1),
                        [DataCadastro] = COALESCE([DataCadastro], SYSUTCDATETIME())
                    WHERE
                        [NomeArquivo] IS NULL OR LTRIM(RTRIM([NomeArquivo])) = ''
                        OR [Url] IS NULL OR LTRIM(RTRIM([Url])) = ''
                        OR [Tipo] IS NULL OR LTRIM(RTRIM([Tipo])) = ''
                        OR [Capa] IS NULL
                        OR [Ordem] IS NULL
                        OR [Ativo] IS NULL
                        OR [DataCadastro] IS NULL;
                END;

                IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
                BEGIN
                    UPDATE [VeiculoCaracteristica]
                    SET
                        [ArCondicionado] = COALESCE([ArCondicionado], 0),
                        [ArQuente] = COALESCE([ArQuente], 0),
                        [DirecaoHidraulica] = COALESCE([DirecaoHidraulica], 0),
                        [DirecaoEletrica] = COALESCE([DirecaoEletrica], 0),
                        [VidroEletrico] = COALESCE([VidroEletrico], 0),
                        [TravaEletrica] = COALESCE([TravaEletrica], 0),
                        [RetrovisorEletrico] = COALESCE([RetrovisorEletrico], 0),
                        [BancoDeCouro] = COALESCE([BancoDeCouro], 0),
                        [AjusteEletricoBancos] = COALESCE([AjusteEletricoBancos], 0),
                        [AquecimentoBancos] = COALESCE([AquecimentoBancos], 0),
                        [VolanteMultifuncional] = COALESCE([VolanteMultifuncional], 0),
                        [PilotoAutomatico] = COALESCE([PilotoAutomatico], 0),
                        [ControleAutomaticoVelocidade] = COALESCE([ControleAutomaticoVelocidade], 0),
                        [LimitadorVelocidade] = COALESCE([LimitadorVelocidade], 0),
                        [ComputadorBordo] = COALESCE([ComputadorBordo], 0),
                        [ChavePresencial] = COALESCE([ChavePresencial], 0),
                        [PartidaBotao] = COALESCE([PartidaBotao], 0),
                        [SensorChuva] = COALESCE([SensorChuva], 0),
                        [SensorCrepuscular] = COALESCE([SensorCrepuscular], 0),
                        [TetoSolar] = COALESCE([TetoSolar], 0),
                        [TetoPanoramico] = COALESCE([TetoPanoramico], 0),
                        [AirbagMotorista] = COALESCE([AirbagMotorista], 0),
                        [AirbagPassageiro] = COALESCE([AirbagPassageiro], 0),
                        [AirbagLateral] = COALESCE([AirbagLateral], 0),
                        [AirbagCortina] = COALESCE([AirbagCortina], 0),
                        [FreiosAbs] = COALESCE([FreiosAbs], 0),
                        [ControleTracao] = COALESCE([ControleTracao], 0),
                        [ControleEstabilidade] = COALESCE([ControleEstabilidade], 0),
                        [AssistentePartidaRampa] = COALESCE([AssistentePartidaRampa], 0),
                        [Isofix] = COALESCE([Isofix], 0),
                        [Alarme] = COALESCE([Alarme], 0),
                        [CameraDeRe] = COALESCE([CameraDeRe], 0),
                        [SensorEstacionamentoDianteiro] = COALESCE([SensorEstacionamentoDianteiro], 0),
                        [SensorEstacionamentoTraseiro] = COALESCE([SensorEstacionamentoTraseiro], 0),
                        [FarolNeblina] = COALESCE([FarolNeblina], 0),
                        [FarolLed] = COALESCE([FarolLed], 0),
                        [FarolMilha] = COALESCE([FarolMilha], 0),
                        [CentralMultimidia] = COALESCE([CentralMultimidia], 0),
                        [Som] = COALESCE([Som], 0),
                        [Bluetooth] = COALESCE([Bluetooth], 0),
                        [Usb] = COALESCE([Usb], 0),
                        [EntradaAuxiliar] = COALESCE([EntradaAuxiliar], 0),
                        [Radio] = COALESCE([Radio], 0),
                        [GPS] = COALESCE([GPS], 0),
                        [CarregadorInducao] = COALESCE([CarregadorInducao], 0),
                        [AppleCarPlay] = COALESCE([AppleCarPlay], 0),
                        [AndroidAuto] = COALESCE([AndroidAuto], 0),
                        [RodaLigaLeve] = COALESCE([RodaLigaLeve], 0),
                        [KitMultimidia] = COALESCE([KitMultimidia], 0),
                        [Engate] = COALESCE([Engate], 0),
                        [Bagageiro] = COALESCE([Bagageiro], 0),
                        [CapotaMaritima] = COALESCE([CapotaMaritima], 0),
                        [Estribo] = COALESCE([Estribo], 0),
                        [SantoAntonio] = COALESCE([SantoAntonio], 0),
                        [ProtetorCacamba] = COALESCE([ProtetorCacamba], 0),
                        [PortaMalasEletrico] = COALESCE([PortaMalasEletrico], 0),
                        [TerceiraFileira] = COALESCE([TerceiraFileira], 0),
                        [CambioAutomatico] = COALESCE([CambioAutomatico], 0),
                        [CambioManual] = COALESCE([CambioManual], 0),
                        [CambioCvt] = COALESCE([CambioCvt], 0),
                        [CambioAutomatizado] = COALESCE([CambioAutomatizado], 0),
                        [Turbo] = COALESCE([Turbo], 0),
                        [Hibrido] = COALESCE([Hibrido], 0),
                        [Eletrico] = COALESCE([Eletrico], 0),
                        [TracaoDianteira] = COALESCE([TracaoDianteira], 0),
                        [TracaoTraseira] = COALESCE([TracaoTraseira], 0),
                        [TracaoIntegral] = COALESCE([TracaoIntegral], 0),
                        [StartStop] = COALESCE([StartStop], 0),
                        [Ativo] = COALESCE([Ativo], 1),
                        [DataCadastro] = COALESCE([DataCadastro], SYSUTCDATETIME())
                    WHERE
                        [ArCondicionado] IS NULL
                        OR [ArQuente] IS NULL
                        OR [DirecaoHidraulica] IS NULL
                        OR [DirecaoEletrica] IS NULL
                        OR [VidroEletrico] IS NULL
                        OR [TravaEletrica] IS NULL
                        OR [RetrovisorEletrico] IS NULL
                        OR [BancoDeCouro] IS NULL
                        OR [AjusteEletricoBancos] IS NULL
                        OR [AquecimentoBancos] IS NULL
                        OR [VolanteMultifuncional] IS NULL
                        OR [PilotoAutomatico] IS NULL
                        OR [ControleAutomaticoVelocidade] IS NULL
                        OR [LimitadorVelocidade] IS NULL
                        OR [ComputadorBordo] IS NULL
                        OR [ChavePresencial] IS NULL
                        OR [PartidaBotao] IS NULL
                        OR [SensorChuva] IS NULL
                        OR [SensorCrepuscular] IS NULL
                        OR [TetoSolar] IS NULL
                        OR [TetoPanoramico] IS NULL
                        OR [AirbagMotorista] IS NULL
                        OR [AirbagPassageiro] IS NULL
                        OR [AirbagLateral] IS NULL
                        OR [AirbagCortina] IS NULL
                        OR [FreiosAbs] IS NULL
                        OR [ControleTracao] IS NULL
                        OR [ControleEstabilidade] IS NULL
                        OR [AssistentePartidaRampa] IS NULL
                        OR [Isofix] IS NULL
                        OR [Alarme] IS NULL
                        OR [CameraDeRe] IS NULL
                        OR [SensorEstacionamentoDianteiro] IS NULL
                        OR [SensorEstacionamentoTraseiro] IS NULL
                        OR [FarolNeblina] IS NULL
                        OR [FarolLed] IS NULL
                        OR [FarolMilha] IS NULL
                        OR [CentralMultimidia] IS NULL
                        OR [Som] IS NULL
                        OR [Bluetooth] IS NULL
                        OR [Usb] IS NULL
                        OR [EntradaAuxiliar] IS NULL
                        OR [Radio] IS NULL
                        OR [GPS] IS NULL
                        OR [CarregadorInducao] IS NULL
                        OR [AppleCarPlay] IS NULL
                        OR [AndroidAuto] IS NULL
                        OR [RodaLigaLeve] IS NULL
                        OR [KitMultimidia] IS NULL
                        OR [Engate] IS NULL
                        OR [Bagageiro] IS NULL
                        OR [CapotaMaritima] IS NULL
                        OR [Estribo] IS NULL
                        OR [SantoAntonio] IS NULL
                        OR [ProtetorCacamba] IS NULL
                        OR [PortaMalasEletrico] IS NULL
                        OR [TerceiraFileira] IS NULL
                        OR [CambioAutomatico] IS NULL
                        OR [CambioManual] IS NULL
                        OR [CambioCvt] IS NULL
                        OR [CambioAutomatizado] IS NULL
                        OR [Turbo] IS NULL
                        OR [Hibrido] IS NULL
                        OR [Eletrico] IS NULL
                        OR [TracaoDianteira] IS NULL
                        OR [TracaoTraseira] IS NULL
                        OR [TracaoIntegral] IS NULL
                        OR [StartStop] IS NULL
                        OR [Ativo] IS NULL
                        OR [DataCadastro] IS NULL;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data normalization only. Previous null legacy values cannot be reconstructed safely.
        }
    }
}
