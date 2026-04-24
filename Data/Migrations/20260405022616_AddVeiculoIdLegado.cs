using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVeiculoIdLegado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdLegado",
                table: "Veiculo",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_IdLegado",
                table: "Veiculo",
                column: "IdLegado",
                unique: true,
                filter: "[IdLegado] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Veiculo_IdLegado",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "IdLegado",
                table: "Veiculo");
        }
    }
}
