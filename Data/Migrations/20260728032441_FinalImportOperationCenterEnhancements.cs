using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalImportOperationCenterEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelatorioConsolidadoJson",
                table: "ImportJob",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RelatorioGeradoEm",
                table: "ImportJob",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportJobHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportJobId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    UsuarioNome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: true),
                    DuracaoMs = table.Column<long>(type: "bigint", nullable: true),
                    Resultado = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Mensagem = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobHistory_ImportJob_ImportJobId",
                        column: x => x.ImportJobId,
                        principalTable: "ImportJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobHistory_ImportJobId_CriadoEm",
                table: "ImportJobHistory",
                columns: new[] { "ImportJobId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobHistory_Tipo_CriadoEm",
                table: "ImportJobHistory",
                columns: new[] { "Tipo", "CriadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportJobHistory");

            migrationBuilder.DropColumn(
                name: "RelatorioConsolidadoJson",
                table: "ImportJob");

            migrationBuilder.DropColumn(
                name: "RelatorioGeradoEm",
                table: "ImportJob");
        }
    }
}
