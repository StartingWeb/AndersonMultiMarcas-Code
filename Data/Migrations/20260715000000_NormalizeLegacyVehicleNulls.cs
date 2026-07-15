using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeLegacyVehicleNulls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET QUOTED_IDENTIFIER ON;
                SET ANSI_NULLS ON;

                IF OBJECT_ID(N'[Veiculo]', N'U') IS NOT NULL
                BEGIN
                    UPDATE [Veiculo]
                    SET
                        [Cambio] = COALESCE([Cambio], 'NaoInformado'),
                        [Combustivel] = COALESCE([Combustivel], 'NaoInformado'),
                        [PrecoVenda] = COALESCE([PrecoVenda], 0),
                        [Destaque] = COALESCE([Destaque], 0),
                        [Vendido] = COALESCE([Vendido], 0),
                        [AnoModelo] = COALESCE([AnoModelo], YEAR(GETDATE()))
                    WHERE
                        [Cambio] IS NULL
                        OR [Combustivel] IS NULL
                        OR [PrecoVenda] IS NULL
                        OR [Destaque] IS NULL
                        OR [Vendido] IS NULL
                        OR [AnoModelo] IS NULL;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data normalization only. The previous null values cannot be reconstructed safely.
        }
    }
}
