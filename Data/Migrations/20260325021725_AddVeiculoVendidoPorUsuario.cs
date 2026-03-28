using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVeiculoVendidoPorUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VendidoPorUsuarioId",
                table: "Veiculo",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_VendidoPorUsuarioId",
                table: "Veiculo",
                column: "VendidoPorUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Veiculo_AspNetUsers_VendidoPorUsuarioId",
                table: "Veiculo",
                column: "VendidoPorUsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Veiculo_AspNetUsers_VendidoPorUsuarioId",
                table: "Veiculo");

            migrationBuilder.DropIndex(
                name: "IX_Veiculo_VendidoPorUsuarioId",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "VendidoPorUsuarioId",
                table: "Veiculo");
        }
    }
}
