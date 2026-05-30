using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class ValidarDominioAtual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Loja]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Loja] (
                        [Id] int NOT NULL IDENTITY,
                        [Nome] nvarchar(150) NOT NULL,
                        [RazaoSocial] nvarchar(200) NOT NULL,
                        [Cnpj] nvarchar(14) NOT NULL,
                        [Email] nvarchar(180) NOT NULL,
                        [Telefone] nvarchar(20) NOT NULL,
                        [Endereco] nvarchar(180) NOT NULL,
                        [Numero] nvarchar(20) NOT NULL,
                        [Complemento] nvarchar(100) NULL,
                        [Bairro] nvarchar(100) NOT NULL,
                        [Cidade] nvarchar(100) NOT NULL,
                        [Uf] nvarchar(2) NOT NULL,
                        [Cep] nvarchar(8) NOT NULL,
                        [DataCadastro] datetime2 NOT NULL,
                        [Ativo] bit NOT NULL,
                        [DataAtualizacao] datetime2 NULL,
                        CONSTRAINT [PK_Loja] PRIMARY KEY ([Id])
                    );
                END;

                IF OBJECT_ID(N'[Marca]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Marca] (
                        [Id] int NOT NULL IDENTITY,
                        [Nome] nvarchar(100) NOT NULL,
                        [LogoUrl] nvarchar(400) NULL,
                        [DataCadastro] datetime2 NOT NULL,
                        [Ativo] bit NOT NULL,
                        CONSTRAINT [PK_Marca] PRIMARY KEY ([Id])
                    );
                END;

                IF OBJECT_ID(N'[Vendedor]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Vendedor] (
                        [Id] int NOT NULL IDENTITY,
                        [LojaId] int NOT NULL,
                        [Nome] nvarchar(150) NOT NULL,
                        [Email] nvarchar(180) NULL,
                        [Telefone] nvarchar(20) NULL,
                        [Whatsapp] nvarchar(20) NULL,
                        [Cpf] nvarchar(11) NULL,
                        [FotoUrl] nvarchar(400) NULL,
                        [Cargo] nvarchar(120) NULL,
                        [DataCadastro] datetime2 NOT NULL,
                        [Ativo] bit NOT NULL,
                        CONSTRAINT [PK_Vendedor] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Vendedor_Loja_LojaId] FOREIGN KEY ([LojaId]) REFERENCES [Loja] ([Id]) ON DELETE NO ACTION
                    );
                END;

                IF OBJECT_ID(N'[Veiculo]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Veiculo] (
                        [Id] int NOT NULL IDENTITY,
                        [LojaId] int NOT NULL,
                        [MarcaId] int NOT NULL,
                        [VendedorId] int NULL,
                        [Titulo] nvarchar(180) NOT NULL,
                        [Modelo] nvarchar(150) NOT NULL,
                        [Versao] nvarchar(150) NULL,
                        [AnoFabricacao] int NULL,
                        [AnoModelo] int NOT NULL,
                        [Cor] nvarchar(60) NULL,
                        [Combustivel] nvarchar(30) NOT NULL,
                        [Cambio] nvarchar(30) NOT NULL,
                        [Quilometragem] int NULL,
                        [Placa] nvarchar(10) NULL,
                        [PrecoVenda] decimal(18,2) NOT NULL,
                        [AceitaTroca] bit NOT NULL,
                        [Financiavel] bit NOT NULL,
                        [Destaque] bit NOT NULL,
                        [Seminovo] bit NOT NULL,
                        [Vendido] bit NOT NULL,
                        [DataVenda] datetime2 NULL,
                        [Descricao] nvarchar(4000) NULL,
                        [UrlVideo] nvarchar(400) NULL,
                        [ObservacoesInternas] nvarchar(2000) NULL,
                        [IdLegado] int NULL,
                        [ImportadoMidia] bit NOT NULL,
                        [MotoEletrica] bit NOT NULL,
                        [DataCadastro] datetime2 NOT NULL,
                        [Ativo] bit NOT NULL,
                        [DataAtualizacao] datetime2 NULL,
                        CONSTRAINT [PK_Veiculo] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Veiculo_Loja_LojaId] FOREIGN KEY ([LojaId]) REFERENCES [Loja] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_Veiculo_Marca_MarcaId] FOREIGN KEY ([MarcaId]) REFERENCES [Marca] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_Veiculo_Vendedor_VendedorId] FOREIGN KEY ([VendedorId]) REFERENCES [Vendedor] ([Id]) ON DELETE SET NULL
                    );
                END;

                IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [VeiculoCaracteristica] (
                        [Id] int NOT NULL IDENTITY,
                        [VeiculoId] int NOT NULL,
                        [ArCondicionado] bit NOT NULL,
                        [ArQuente] bit NOT NULL,
                        [DirecaoHidraulica] bit NOT NULL,
                        [DirecaoEletrica] bit NOT NULL,
                        [VidroEletrico] bit NOT NULL,
                        [TravaEletrica] bit NOT NULL,
                        [RetrovisorEletrico] bit NOT NULL,
                        [BancoDeCouro] bit NOT NULL,
                        [CentralMultimidia] bit NOT NULL,
                        [Bluetooth] bit NOT NULL,
                        [AndroidAuto] bit NOT NULL,
                        [AppleCarPlay] bit NOT NULL,
                        [CambioAutomatico] bit NOT NULL,
                        [Turbo] bit NOT NULL,
                        [Hibrido] bit NOT NULL,
                        [Eletrico] bit NOT NULL,
                        [DataCadastro] datetime2 NOT NULL,
                        [Ativo] bit NOT NULL,
                        CONSTRAINT [PK_VeiculoCaracteristica] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_VeiculoCaracteristica_Veiculo_VeiculoId] FOREIGN KEY ([VeiculoId]) REFERENCES [Veiculo] ([Id]) ON DELETE CASCADE
                    );
                END;

                IF OBJECT_ID(N'[VeiculoMidia]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [VeiculoMidia] (
                        [Id] int NOT NULL IDENTITY,
                        [VeiculoId] int NOT NULL,
                        [NomeArquivo] nvarchar(200) NOT NULL,
                        [Url] nvarchar(500) NOT NULL,
                        [BlobName] nvarchar(250) NULL,
                        [Container] nvarchar(250) NULL,
                        [Tipo] nvarchar(20) NOT NULL,
                        [ContentType] nvarchar(120) NULL,
                        [TamanhoBytes] bigint NULL,
                        [Capa] bit NOT NULL,
                        [Ordem] int NOT NULL,
                        [DataCadastro] datetime2 NOT NULL,
                        [Ativo] bit NOT NULL,
                        CONSTRAINT [PK_VeiculoMidia] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_VeiculoMidia_Veiculo_VeiculoId] FOREIGN KEY ([VeiculoId]) REFERENCES [Veiculo] ([Id]) ON DELETE CASCADE
                    );
                END;

                IF OBJECT_ID(N'[Loja]', N'U') IS NOT NULL AND COL_LENGTH('Loja','Cnpj') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Loja_Cnpj' AND object_id = OBJECT_ID(N'[Loja]'))
                    CREATE UNIQUE INDEX [IX_Loja_Cnpj] ON [Loja] ([Cnpj]) WHERE [Cnpj] IS NOT NULL;

                IF OBJECT_ID(N'[Loja]', N'U') IS NOT NULL AND COL_LENGTH('Loja','Nome') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Loja_Nome' AND object_id = OBJECT_ID(N'[Loja]'))
                    CREATE INDEX [IX_Loja_Nome] ON [Loja] ([Nome]);

                IF OBJECT_ID(N'[Marca]', N'U') IS NOT NULL AND COL_LENGTH('Marca','Nome') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Marca_Nome' AND object_id = OBJECT_ID(N'[Marca]'))
                    CREATE UNIQUE INDEX [IX_Marca_Nome] ON [Marca] ([Nome]);

                IF OBJECT_ID(N'[Veiculo]', N'U') IS NOT NULL
                    AND COL_LENGTH('Veiculo','LojaId') IS NOT NULL
                    AND COL_LENGTH('Veiculo','Ativo') IS NOT NULL
                    AND COL_LENGTH('Veiculo','Vendido') IS NOT NULL
                    AND COL_LENGTH('Veiculo','Destaque') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Veiculo_LojaId_Ativo_Vendido_Destaque' AND object_id = OBJECT_ID(N'[Veiculo]'))
                    CREATE INDEX [IX_Veiculo_LojaId_Ativo_Vendido_Destaque] ON [Veiculo] ([LojaId], [Ativo], [Vendido], [Destaque]);

                IF OBJECT_ID(N'[Veiculo]', N'U') IS NOT NULL
                    AND COL_LENGTH('Veiculo','MarcaId') IS NOT NULL
                    AND COL_LENGTH('Veiculo','Modelo') IS NOT NULL
                    AND COL_LENGTH('Veiculo','AnoModelo') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Veiculo_MarcaId_Modelo_AnoModelo' AND object_id = OBJECT_ID(N'[Veiculo]'))
                    CREATE INDEX [IX_Veiculo_MarcaId_Modelo_AnoModelo] ON [Veiculo] ([MarcaId], [Modelo], [AnoModelo]);

                IF OBJECT_ID(N'[Veiculo]', N'U') IS NOT NULL AND COL_LENGTH('Veiculo','Placa') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Veiculo_Placa' AND object_id = OBJECT_ID(N'[Veiculo]'))
                    CREATE INDEX [IX_Veiculo_Placa] ON [Veiculo] ([Placa]);

                IF OBJECT_ID(N'[Veiculo]', N'U') IS NOT NULL AND COL_LENGTH('Veiculo','VendedorId') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Veiculo_VendedorId' AND object_id = OBJECT_ID(N'[Veiculo]'))
                    CREATE INDEX [IX_Veiculo_VendedorId] ON [Veiculo] ([VendedorId]);

                IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL AND COL_LENGTH('VeiculoCaracteristica','VeiculoId') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_VeiculoCaracteristica_VeiculoId' AND object_id = OBJECT_ID(N'[VeiculoCaracteristica]'))
                    CREATE UNIQUE INDEX [IX_VeiculoCaracteristica_VeiculoId] ON [VeiculoCaracteristica] ([VeiculoId]);

                IF OBJECT_ID(N'[VeiculoMidia]', N'U') IS NOT NULL
                    AND COL_LENGTH('VeiculoMidia','VeiculoId') IS NOT NULL
                    AND COL_LENGTH('VeiculoMidia','Capa') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_VeiculoMidia_VeiculoId_Capa' AND object_id = OBJECT_ID(N'[VeiculoMidia]'))
                    CREATE INDEX [IX_VeiculoMidia_VeiculoId_Capa] ON [VeiculoMidia] ([VeiculoId], [Capa]);

                IF OBJECT_ID(N'[VeiculoMidia]', N'U') IS NOT NULL
                    AND COL_LENGTH('VeiculoMidia','VeiculoId') IS NOT NULL
                    AND COL_LENGTH('VeiculoMidia','Ordem') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_VeiculoMidia_VeiculoId_Ordem' AND object_id = OBJECT_ID(N'[VeiculoMidia]'))
                    CREATE INDEX [IX_VeiculoMidia_VeiculoId_Ordem] ON [VeiculoMidia] ([VeiculoId], [Ordem]);

                IF OBJECT_ID(N'[Vendedor]', N'U') IS NOT NULL
                    AND COL_LENGTH('Vendedor','LojaId') IS NOT NULL
                    AND COL_LENGTH('Vendedor','Nome') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Vendedor_LojaId_Nome' AND object_id = OBJECT_ID(N'[Vendedor]'))
                    CREATE INDEX [IX_Vendedor_LojaId_Nome] ON [Vendedor] ([LojaId], [Nome]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intencionalmente vazio: migration de validação/compatibilização para ambiente existente.
        }
    }
}

