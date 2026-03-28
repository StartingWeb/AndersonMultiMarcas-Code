using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AuthProfilesAndMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "AspNetRoles",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AspNetMenus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Icone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MenuPaiId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetMenus_AspNetMenus_MenuPaiId",
                        column: x => x.MenuPaiId,
                        principalTable: "AspNetMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetMenuRoles",
                columns: table => new
                {
                    MenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetMenuRoles", x => new { x.MenuId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetMenuRoles_AspNetMenus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "AspNetMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetMenuRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetMenuRoles_RoleId",
                table: "AspNetMenuRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetMenus_MenuPaiId_Ordem",
                table: "AspNetMenus",
                columns: new[] { "MenuPaiId", "Ordem" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetMenuRoles");

            migrationBuilder.DropTable(
                name: "AspNetMenus");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "AspNetRoles");
        }
    }
}
