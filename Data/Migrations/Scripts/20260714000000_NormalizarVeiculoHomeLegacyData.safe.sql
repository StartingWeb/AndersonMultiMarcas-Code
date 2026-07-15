/*
  Script administrativo: normalizacao de dados legados de Veiculo.
  Objetivo: executar fora da requisicao publica da Home a mesma correcao
  defensiva que antes era feita a cada acesso.

  Uso esperado:
  - executar manualmente/administrativamente quando houver suspeita de dados legados;
  - pode ser reexecutado com seguranca;
  - em bases saudaveis deve afetar 0 linhas.
*/

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

IF OBJECT_ID(N'[Veiculo]', N'U') IS NOT NULL
BEGIN
    UPDATE [Veiculo]
    SET
        [Cambio] = COALESCE([Cambio], 'NaoInformado'),
        [Combustivel] = COALESCE([Combustivel], 'NaoInformado'),
        [PrecoVenda] = COALESCE([PrecoVenda], 0),
        [Destaque] = COALESCE([Destaque], 0),
        [Vendido] = COALESCE([Vendido], 0),
        [AnoModelo] = COALESCE([AnoModelo], YEAR(GETDATE()))
    WHERE
        [Cambio] IS NULL
        OR [Combustivel] IS NULL
        OR [PrecoVenda] IS NULL
        OR [Destaque] IS NULL
        OR [Vendido] IS NULL
        OR [AnoModelo] IS NULL;

    SELECT @@ROWCOUNT AS [RowsAffected];
END;
