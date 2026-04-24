using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVeiculoLegacyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Chassi",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "PrecoFipe",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "PrecoPromocional",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "Renavam",
                table: "Veiculo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Chassi",
                table: "Veiculo",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecoFipe",
                table: "Veiculo",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecoPromocional",
                table: "Veiculo",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Renavam",
                table: "Veiculo",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
