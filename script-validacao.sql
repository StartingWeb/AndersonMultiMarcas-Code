IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF OBJECT_ID(N'[AspNetRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[AspNetUsers]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [NomeCompleto] nvarchar(max) NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[AspNetRoleClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[AspNetUserClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[AspNetUserLogins]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[AspNetUserRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[AspNetUserTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetRoleClaims_RoleId' AND object_id = OBJECT_ID(N'[AspNetRoleClaims]'))
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'RoleNameIndex' AND object_id = OBJECT_ID(N'[AspNetRoles]'))
    CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserClaims_UserId' AND object_id = OBJECT_ID(N'[AspNetUserClaims]'))
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserLogins_UserId' AND object_id = OBJECT_ID(N'[AspNetUserLogins]'))
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserRoles_RoleId' AND object_id = OBJECT_ID(N'[AspNetUserRoles]'))
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'EmailIndex' AND object_id = OBJECT_ID(N'[AspNetUsers]'))
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UserNameIndex' AND object_id = OBJECT_ID(N'[AspNetUsers]'))
    CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260307182118_InitialMigration', N'9.0.13');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserTokens]') AND [c].[name] = N'UserId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserTokens] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [AspNetUserTokens] ALTER COLUMN [UserId] uniqueidentifier NOT NULL;

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'Id');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [AspNetUsers] ALTER COLUMN [Id] uniqueidentifier NOT NULL;

DROP INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles];
DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserRoles]') AND [c].[name] = N'RoleId');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserRoles] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [AspNetUserRoles] ALTER COLUMN [RoleId] uniqueidentifier NOT NULL;
CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserRoles]') AND [c].[name] = N'UserId');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserRoles] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [AspNetUserRoles] ALTER COLUMN [UserId] uniqueidentifier NOT NULL;

DROP INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins];
DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserLogins]') AND [c].[name] = N'UserId');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserLogins] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [AspNetUserLogins] ALTER COLUMN [UserId] uniqueidentifier NOT NULL;
CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

DROP INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims];
DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserClaims]') AND [c].[name] = N'UserId');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserClaims] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [AspNetUserClaims] ALTER COLUMN [UserId] uniqueidentifier NOT NULL;
CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetRoles]') AND [c].[name] = N'Id');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [AspNetRoles] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [AspNetRoles] ALTER COLUMN [Id] uniqueidentifier NOT NULL;

DROP INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims];
DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetRoleClaims]') AND [c].[name] = N'RoleId');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [AspNetRoleClaims] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [AspNetRoleClaims] ALTER COLUMN [RoleId] uniqueidentifier NOT NULL;
CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260528140115_AlignIdentityToGuid', N'9.0.13');

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
    CREATE UNIQUE INDEX [IX_Loja_Cnpj] ON [Loja] ([Cnpj]);

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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260528233929_ValidarDominioAtual', N'9.0.13');

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'AirbagCortina') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [AirbagCortina] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_AirbagCortina] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'AirbagLateral') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [AirbagLateral] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_AirbagLateral] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'AirbagMotorista') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [AirbagMotorista] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_AirbagMotorista] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'AirbagPassageiro') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [AirbagPassageiro] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_AirbagPassageiro] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'AjusteEletricoBancos') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [AjusteEletricoBancos] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_AjusteEletricoBancos] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'Alarme') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [Alarme] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_Alarme] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'AquecimentoBancos') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [AquecimentoBancos] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_AquecimentoBancos] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'AssistentePartidaRampa') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [AssistentePartidaRampa] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_AssistentePartidaRampa] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'Bagageiro') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [Bagageiro] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_Bagageiro] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'CambioAutomatizado') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [CambioAutomatizado] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_CambioAutomatizado] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'CambioCvt') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [CambioCvt] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_CambioCvt] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'CambioManual') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [CambioManual] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_CambioManual] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'CameraDeRe') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [CameraDeRe] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_CameraDeRe] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'CapotaMaritima') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [CapotaMaritima] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_CapotaMaritima] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'CarregadorInducao') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [CarregadorInducao] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_CarregadorInducao] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'ChavePresencial') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [ChavePresencial] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_ChavePresencial] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'ComputadorBordo') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [ComputadorBordo] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_ComputadorBordo] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'ControleAutomaticoVelocidade') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [ControleAutomaticoVelocidade] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_ControleAutomaticoVelocidade] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'ControleEstabilidade') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [ControleEstabilidade] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_ControleEstabilidade] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'ControleTracao') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [ControleTracao] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_ControleTracao] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'Engate') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [Engate] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_Engate] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'EntradaAuxiliar') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [EntradaAuxiliar] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_EntradaAuxiliar] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'Estribo') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [Estribo] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_Estribo] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'FarolLed') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [FarolLed] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_FarolLed] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'FarolMilha') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [FarolMilha] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_FarolMilha] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'FarolNeblina') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [FarolNeblina] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_FarolNeblina] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'FreiosAbs') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [FreiosAbs] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_FreiosAbs] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'GPS') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [GPS] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_GPS] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'Isofix') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [Isofix] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_Isofix] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'KitMultimidia') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [KitMultimidia] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_KitMultimidia] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'LimitadorVelocidade') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [LimitadorVelocidade] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_LimitadorVelocidade] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'PartidaBotao') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [PartidaBotao] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_PartidaBotao] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'PilotoAutomatico') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [PilotoAutomatico] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_PilotoAutomatico] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'PortaMalasEletrico') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [PortaMalasEletrico] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_PortaMalasEletrico] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'ProtetorCacamba') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [ProtetorCacamba] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_ProtetorCacamba] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'Radio') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [Radio] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_Radio] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'RodaLigaLeve') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [RodaLigaLeve] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_RodaLigaLeve] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'SantoAntonio') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [SantoAntonio] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_SantoAntonio] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'SensorChuva') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [SensorChuva] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_SensorChuva] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'SensorCrepuscular') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [SensorCrepuscular] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_SensorCrepuscular] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'SensorEstacionamentoDianteiro') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [SensorEstacionamentoDianteiro] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_SensorEstacionamentoDianteiro] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'SensorEstacionamentoTraseiro') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [SensorEstacionamentoTraseiro] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_SensorEstacionamentoTraseiro] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'Som') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [Som] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_Som] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'StartStop') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [StartStop] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_StartStop] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'TerceiraFileira') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [TerceiraFileira] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_TerceiraFileira] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'TetoPanoramico') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [TetoPanoramico] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_TetoPanoramico] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'TetoSolar') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [TetoSolar] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_TetoSolar] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'TracaoDianteira') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [TracaoDianteira] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_TracaoDianteira] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'TracaoIntegral') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [TracaoIntegral] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_TracaoIntegral] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'TracaoTraseira') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [TracaoTraseira] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_TracaoTraseira] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'Usb') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [Usb] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_Usb] DEFAULT 0;
END;

IF OBJECT_ID(N'[VeiculoCaracteristica]', N'U') IS NOT NULL
   AND COL_LENGTH('VeiculoCaracteristica', 'VolanteMultifuncional') IS NULL
BEGIN
    ALTER TABLE [VeiculoCaracteristica]
    ADD [VolanteMultifuncional] bit NOT NULL
        CONSTRAINT [DF_VeiculoCaracteristica_VolanteMultifuncional] DEFAULT 0;
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260528235037_ValidarVeiculoCaracteristicaLegado', N'9.0.13');

COMMIT;
GO

