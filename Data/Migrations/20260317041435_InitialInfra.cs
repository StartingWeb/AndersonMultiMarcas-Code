using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialInfra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Loja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Cnpj = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Endereco = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Complemento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Cidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Cep = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loja", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Marca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marca", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LojaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Whatsapp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Cpf = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FotoUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendedor_Loja_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Loja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Veiculo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LojaId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MarcaId = table.Column<int>(type: "int", nullable: false),
                    VendedorId = table.Column<int>(type: "int", nullable: true),
                    Modelo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Versao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AnoFabricacao = table.Column<int>(type: "int", nullable: true),
                    AnoModelo = table.Column<int>(type: "int", nullable: true),
                    Cor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Combustivel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Cambio = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Quilometragem = table.Column<int>(type: "int", nullable: true),
                    Placa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Chassi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Renavam = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrecoVenda = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrecoPromocional = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrecoFipe = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AceitaTroca = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Financiavel = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Destaque = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Seminovo = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Vendido = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DataVenda = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UrlVideo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ObservacoesInternas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veiculo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Veiculo_Loja_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Loja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Veiculo_Marca_MarcaId",
                        column: x => x.MarcaId,
                        principalTable: "Marca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Veiculo_Vendedor_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VeiculoCaracteristica",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VeiculoId = table.Column<int>(type: "int", nullable: false),
                    ArCondicionado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ArQuente = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DirecaoHidraulica = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DirecaoEletrica = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VidroEletrico = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TravaEletrica = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RetrovisorEletrico = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BancoDeCouro = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AjusteEletricoBancos = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AquecimentoBancos = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VolanteMultifuncional = table.Column<bool>(type: "bit", nullable: false),
                    PilotoAutomatico = table.Column<bool>(type: "bit", nullable: false),
                    ControleAutomaticoVelocidade = table.Column<bool>(type: "bit", nullable: false),
                    LimitadorVelocidade = table.Column<bool>(type: "bit", nullable: false),
                    ComputadorBordo = table.Column<bool>(type: "bit", nullable: false),
                    ChavePresencial = table.Column<bool>(type: "bit", nullable: false),
                    PartidaBotao = table.Column<bool>(type: "bit", nullable: false),
                    SensorChuva = table.Column<bool>(type: "bit", nullable: false),
                    SensorCrepuscular = table.Column<bool>(type: "bit", nullable: false),
                    TetoSolar = table.Column<bool>(type: "bit", nullable: false),
                    TetoPanoramico = table.Column<bool>(type: "bit", nullable: false),
                    AirbagMotorista = table.Column<bool>(type: "bit", nullable: false),
                    AirbagPassageiro = table.Column<bool>(type: "bit", nullable: false),
                    AirbagLateral = table.Column<bool>(type: "bit", nullable: false),
                    AirbagCortina = table.Column<bool>(type: "bit", nullable: false),
                    FreiosAbs = table.Column<bool>(type: "bit", nullable: false),
                    ControleTracao = table.Column<bool>(type: "bit", nullable: false),
                    ControleEstabilidade = table.Column<bool>(type: "bit", nullable: false),
                    AssistentePartidaRampa = table.Column<bool>(type: "bit", nullable: false),
                    Isofix = table.Column<bool>(type: "bit", nullable: false),
                    Alarme = table.Column<bool>(type: "bit", nullable: false),
                    CameraDeRe = table.Column<bool>(type: "bit", nullable: false),
                    SensorEstacionamentoDianteiro = table.Column<bool>(type: "bit", nullable: false),
                    SensorEstacionamentoTraseiro = table.Column<bool>(type: "bit", nullable: false),
                    FarolNeblina = table.Column<bool>(type: "bit", nullable: false),
                    FarolLed = table.Column<bool>(type: "bit", nullable: false),
                    FarolMilha = table.Column<bool>(type: "bit", nullable: false),
                    CentralMultimidia = table.Column<bool>(type: "bit", nullable: false),
                    Som = table.Column<bool>(type: "bit", nullable: false),
                    Bluetooth = table.Column<bool>(type: "bit", nullable: false),
                    Usb = table.Column<bool>(type: "bit", nullable: false),
                    EntradaAuxiliar = table.Column<bool>(type: "bit", nullable: false),
                    Radio = table.Column<bool>(type: "bit", nullable: false),
                    GPS = table.Column<bool>(type: "bit", nullable: false),
                    CarregadorInducao = table.Column<bool>(type: "bit", nullable: false),
                    AppleCarPlay = table.Column<bool>(type: "bit", nullable: false),
                    AndroidAuto = table.Column<bool>(type: "bit", nullable: false),
                    RodaLigaLeve = table.Column<bool>(type: "bit", nullable: false),
                    KitMultimidia = table.Column<bool>(type: "bit", nullable: false),
                    Engate = table.Column<bool>(type: "bit", nullable: false),
                    Bagageiro = table.Column<bool>(type: "bit", nullable: false),
                    CapotaMaritima = table.Column<bool>(type: "bit", nullable: false),
                    Estribo = table.Column<bool>(type: "bit", nullable: false),
                    SantoAntonio = table.Column<bool>(type: "bit", nullable: false),
                    ProtetorCacamba = table.Column<bool>(type: "bit", nullable: false),
                    PortaMalasEletrico = table.Column<bool>(type: "bit", nullable: false),
                    TerceiraFileira = table.Column<bool>(type: "bit", nullable: false),
                    CambioAutomatico = table.Column<bool>(type: "bit", nullable: false),
                    CambioManual = table.Column<bool>(type: "bit", nullable: false),
                    CambioCvt = table.Column<bool>(type: "bit", nullable: false),
                    CambioAutomatizado = table.Column<bool>(type: "bit", nullable: false),
                    TracaoDianteira = table.Column<bool>(type: "bit", nullable: false),
                    TracaoTraseira = table.Column<bool>(type: "bit", nullable: false),
                    TracaoIntegral = table.Column<bool>(type: "bit", nullable: false),
                    StartStop = table.Column<bool>(type: "bit", nullable: false),
                    Turbo = table.Column<bool>(type: "bit", nullable: false),
                    Hibrido = table.Column<bool>(type: "bit", nullable: false),
                    Eletrico = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeiculoCaracteristica", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VeiculoCaracteristica_Veiculo_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VeiculoMidia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VeiculoId = table.Column<int>(type: "int", nullable: false),
                    NomeArquivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Container = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: true),
                    Capa = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Ordem = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeiculoMidia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VeiculoMidia_Veiculo_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_Ativo",
                table: "Veiculo",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_Destaque",
                table: "Veiculo",
                column: "Destaque");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_LojaId",
                table: "Veiculo",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_MarcaId",
                table: "Veiculo",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_VendedorId",
                table: "Veiculo",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_Vendido",
                table: "Veiculo",
                column: "Vendido");

            migrationBuilder.CreateIndex(
                name: "IX_VeiculoCaracteristica_VeiculoId",
                table: "VeiculoCaracteristica",
                column: "VeiculoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VeiculoMidia_VeiculoId",
                table: "VeiculoMidia",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_VeiculoMidia_VeiculoId_Capa",
                table: "VeiculoMidia",
                columns: new[] { "VeiculoId", "Capa" },
                unique: true,
                filter: "[Capa] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_VeiculoMidia_VeiculoId_Ordem",
                table: "VeiculoMidia",
                columns: new[] { "VeiculoId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_Vendedor_LojaId",
                table: "Vendedor",
                column: "LojaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VeiculoCaracteristica");

            migrationBuilder.DropTable(
                name: "VeiculoMidia");

            migrationBuilder.DropTable(
                name: "Veiculo");

            migrationBuilder.DropTable(
                name: "Marca");

            migrationBuilder.DropTable(
                name: "Vendedor");

            migrationBuilder.DropTable(
                name: "Loja");
        }
    }
}
