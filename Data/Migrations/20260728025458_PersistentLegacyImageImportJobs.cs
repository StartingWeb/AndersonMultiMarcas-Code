using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class PersistentLegacyImageImportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportJob",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IniciadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanceladoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    UsuarioNome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UrlBase = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DryRun = table.Column<bool>(type: "bit", nullable: false),
                    SomenteSemBlobName = table.Column<bool>(type: "bit", nullable: false),
                    Sobrescrever = table.Column<bool>(type: "bit", nullable: false),
                    PreparacaoConcluida = table.Column<bool>(type: "bit", nullable: false),
                    IdInicial = table.Column<int>(type: "int", nullable: true),
                    QuantidadeMaxima = table.Column<int>(type: "int", nullable: true),
                    TotalVeiculos = table.Column<int>(type: "int", nullable: false),
                    VeiculosProcessados = table.Column<int>(type: "int", nullable: false),
                    TotalImagens = table.Column<int>(type: "int", nullable: false),
                    ImagensImportadas = table.Column<int>(type: "int", nullable: false),
                    ImagensIgnoradas = table.Column<int>(type: "int", nullable: false),
                    ImagensComErro = table.Column<int>(type: "int", nullable: false),
                    UltimaMensagem = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UltimaAtualizacaoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VeiculoAtualId = table.Column<int>(type: "int", nullable: true),
                    LockId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    LockExpiraEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJob", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportJobId = table.Column<int>(type: "int", nullable: false),
                    VeiculoId = table.Column<int>(type: "int", nullable: false),
                    VeiculoMidiaId = table.Column<int>(type: "int", nullable: true),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Capa = table.Column<bool>(type: "bit", nullable: false),
                    UrlLegada = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    NomeArquivoDestino = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    BlobNameDestino = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContainerDestino = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    UrlDestino = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Tentativas = table.Column<int>(type: "int", nullable: false),
                    MaxTentativas = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: true),
                    Erro = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IniciadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    LockExpiraEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobItem_ImportJob_ImportJobId",
                        column: x => x.ImportJobId,
                        principalTable: "ImportJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportJobItem_VeiculoMidia_VeiculoMidiaId",
                        column: x => x.VeiculoMidiaId,
                        principalTable: "VeiculoMidia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportJobItem_Veiculo_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportJobId = table.Column<int>(type: "int", nullable: false),
                    ImportJobItemId = table.Column<int>(type: "int", nullable: true),
                    VeiculoId = table.Column<int>(type: "int", nullable: true),
                    ImagemOrdem = table.Column<int>(type: "int", nullable: true),
                    UrlLegada = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    Etapa = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobLog_ImportJobItem_ImportJobItemId",
                        column: x => x.ImportJobItemId,
                        principalTable: "ImportJobItem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ImportJobLog_ImportJob_ImportJobId",
                        column: x => x.ImportJobId,
                        principalTable: "ImportJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJob_CriadoEm",
                table: "ImportJob",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJob_LockExpiraEm",
                table: "ImportJob",
                column: "LockExpiraEm");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJob_Status",
                table: "ImportJob",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobItem_ImportJobId_BlobNameDestino",
                table: "ImportJobItem",
                columns: new[] { "ImportJobId", "BlobNameDestino" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobItem_ImportJobId_Status",
                table: "ImportJobItem",
                columns: new[] { "ImportJobId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobItem_ImportJobId_UrlLegada",
                table: "ImportJobItem",
                columns: new[] { "ImportJobId", "UrlLegada" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobItem_ImportJobId_VeiculoId",
                table: "ImportJobItem",
                columns: new[] { "ImportJobId", "VeiculoId" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobItem_VeiculoId",
                table: "ImportJobItem",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobItem_VeiculoMidiaId",
                table: "ImportJobItem",
                column: "VeiculoMidiaId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobLog_ImportJobId_CriadoEm",
                table: "ImportJobLog",
                columns: new[] { "ImportJobId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobLog_ImportJobId_Id",
                table: "ImportJobLog",
                columns: new[] { "ImportJobId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobLog_ImportJobItemId",
                table: "ImportJobLog",
                column: "ImportJobItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportJobLog");

            migrationBuilder.DropTable(
                name: "ImportJobItem");

            migrationBuilder.DropTable(
                name: "ImportJob");
        }
    }
}
