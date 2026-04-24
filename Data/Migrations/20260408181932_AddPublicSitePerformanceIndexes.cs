using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicSitePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_Ativo_Vendido_DataCadastro",
                table: "Veiculo",
                columns: new[] { "Ativo", "Vendido", "DataCadastro" });

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_Ativo_Vendido_Destaque_DataCadastro",
                table: "Veiculo",
                columns: new[] { "Ativo", "Vendido", "Destaque", "DataCadastro" });

            migrationBuilder.CreateIndex(
                name: "IX_Marca_Nome",
                table: "Marca",
                column: "Nome");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Veiculo_Ativo_Vendido_DataCadastro",
                table: "Veiculo");

            migrationBuilder.DropIndex(
                name: "IX_Veiculo_Ativo_Vendido_Destaque_DataCadastro",
                table: "Veiculo");

            migrationBuilder.DropIndex(
                name: "IX_Marca_Nome",
                table: "Marca");
        }
    }
}
