/*
  InvestLens: estrutura inicial e evoluções durante o desenvolvimento.
  Executar no banco de destino com SSMS ou sqlcmd (suporte a GO).
  Interromper a execução no primeiro erro (sqlcmd -b).
  Não executar alterações de estrutura simultaneamente com gravações da aplicação.

  PK_<Tabela>; UK_<Tabela>_<Coluna>; FK_<Origem>_<Destino>;
  DF_<Tabela>_<Coluna>. Todas as evoluções permanecem neste arquivo.
  Os dados incompatíveis geram erro explícito; não são excluídos ou corrigidos
  silenciosamente. A versão anterior conhecida da ILCAD000 é migrada abaixo.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILCAD000
  (
    ID_Tabela INT IDENTITY(1,1) NOT NULL
   ,NM_Fisico VARCHAR(80) NOT NULL
   ,NM_Logico VARCHAR(80) NOT NULL
   ,DH_Inclusao DATETIME2(0) NOT NULL
  );
END;
GO

-- Evolução do catálogo original: preservar os registros existentes.
IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD000', 'ID_Tabela') IS NULL
BEGIN
  ALTER TABLE dbo.ILCAD000
  ADD ID_Tabela INT IDENTITY(1,1) NOT NULL;
END;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD000', 'DH_Inclusao') IS NULL
  AND OBJECT_ID('dbo.DF_ILCAD000_DH_Inclusao', 'D') IS NULL
BEGIN
  -- A data dos registros legados passa a ser a data desta evolução.
  ALTER TABLE dbo.ILCAD000
  ADD DH_Inclusao DATETIME2(0) NOT NULL
    CONSTRAINT DF_ILCAD000_DH_Inclusao DEFAULT (SYSDATETIME()) WITH VALUES;
END;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD000', 'NM_Fisico') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.key_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCAD000')
    AND name = 'UK_ILCAD000_NM_Fisico'
)
  AND NOT EXISTS
(
  -- Uma UNIQUE equivalente já fornece a restrição e o índice necessários.
  SELECT 1
  FROM sys.key_constraints Chave
       INNER JOIN sys.index_columns Coluna
       ON Coluna.object_id = Chave.parent_object_id
         AND Coluna.index_id = Chave.unique_index_id
  WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCAD000')
    AND Chave.type = 'UQ'
    AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD000'), 'NM_Fisico', 'ColumnId')
    AND Coluna.key_ordinal = 1
    AND NOT EXISTS
    (
      SELECT 1
      FROM sys.index_columns OutraColuna
      WHERE OutraColuna.object_id = Coluna.object_id
        AND OutraColuna.index_id = Coluna.index_id
        AND OutraColuna.key_ordinal > 1
    )
)
BEGIN
  IF EXISTS
  (
    SELECT NM_Fisico
    FROM dbo.ILCAD000
    GROUP BY NM_Fisico
    HAVING COUNT_BIG(*) > 1
  )
  BEGIN
    THROW 51000, 'Valores duplicados em ILCAD000.NM_Fisico; UNIQUE não criada.', 1;
  END;

  ALTER TABLE dbo.ILCAD000
  ADD CONSTRAINT UK_ILCAD000_NM_Fisico UNIQUE (NM_Fisico);
END;
GO

-- Migrar somente a PK conhecida da versão anterior (NM_Fisico).
IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD000', 'ID_Tabela') IS NOT NULL
  AND EXISTS
(
  SELECT 1
  FROM sys.key_constraints Chave
       INNER JOIN sys.index_columns Coluna
       ON Coluna.object_id = Chave.parent_object_id
         AND Coluna.index_id = Chave.unique_index_id
  WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCAD000')
    AND Chave.name = 'PK_ILCAD000'
    AND Chave.type = 'PK'
    AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD000'), 'NM_Fisico', 'ColumnId')
    AND Coluna.key_ordinal = 1
    AND NOT EXISTS
    (
      SELECT 1
      FROM sys.index_columns OutraColuna
      WHERE OutraColuna.object_id = Coluna.object_id
        AND OutraColuna.index_id = Coluna.index_id
        AND OutraColuna.key_ordinal > 1
    )
)
BEGIN
  IF EXISTS
  (
    SELECT 1
    FROM sys.foreign_keys Referencia
         INNER JOIN sys.key_constraints Chave
         ON Chave.parent_object_id = Referencia.referenced_object_id
           AND Chave.unique_index_id = Referencia.key_index_id
    WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCAD000')
      AND Chave.name = 'PK_ILCAD000'
  )
  BEGIN
    THROW 51001, 'A PK antiga da ILCAD000 possui FKs dependentes; revisar antes de migrar.', 1;
  END;

  ALTER TABLE dbo.ILCAD000 DROP CONSTRAINT PK_ILCAD000;
END;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD000', 'ID_Tabela') IS NOT NULL
BEGIN
  IF COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD000'), 'ID_Tabela', 'IsIdentity') <> 1
  BEGIN
    THROW 51002, 'ILCAD000.ID_Tabela deve ser IDENTITY; revisar a estrutura existente.', 1;
  END;

  IF NOT EXISTS
  (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.ILCAD000')
      AND type = 'PK'
  )
  BEGIN
    IF EXISTS
    (
      SELECT ID_Tabela
      FROM dbo.ILCAD000
      GROUP BY ID_Tabela
      HAVING COUNT_BIG(*) > 1
    )
      OR EXISTS
    (
      SELECT 1
      FROM dbo.ILCAD000
      WHERE ID_Tabela IS NULL
    )
    BEGIN
      THROW 51003, 'Dados incompatíveis com a PK de ILCAD000.', 1;
    END;

    ALTER TABLE dbo.ILCAD000
    ADD CONSTRAINT PK_ILCAD000 PRIMARY KEY (ID_Tabela);
  END;

  IF NOT EXISTS
  (
    SELECT 1
    FROM sys.key_constraints Chave
         INNER JOIN sys.index_columns Coluna
         ON Coluna.object_id = Chave.parent_object_id
           AND Coluna.index_id = Chave.unique_index_id
    WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCAD000')
      AND Chave.type = 'PK'
      AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD000'), 'ID_Tabela', 'ColumnId')
      AND Coluna.key_ordinal = 1
      AND NOT EXISTS
      (
        SELECT 1
        FROM sys.index_columns OutraColuna
        WHERE OutraColuna.object_id = Coluna.object_id
          AND OutraColuna.index_id = Coluna.index_id
          AND OutraColuna.key_ordinal > 1
      )
  )
  BEGIN
    THROW 51004, 'Chave primária incompatível em ILCAD000; revisar a estrutura existente.', 1;
  END;
END;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD000', 'DH_Inclusao') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCAD000')
    AND
    (
      name = 'DF_ILCAD000_DH_Inclusao'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD000'), 'DH_Inclusao', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCAD000
  ADD CONSTRAINT DF_ILCAD000_DH_Inclusao DEFAULT (SYSDATETIME()) FOR DH_Inclusao;
END;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM dbo.ILCAD000
  WHERE NM_Fisico = 'ILCAD000'
)
BEGIN
  INSERT INTO dbo.ILCAD000
  (
    NM_Fisico
   ,NM_Logico
  )
  VALUES
  (
    'ILCAD000'
   ,'Controle de Tabelas'
  );
END;
GO

IF NOT EXISTS
(
  SELECT 1
  FROM sys.sequences
  WHERE schema_id = SCHEMA_ID('dbo')
    AND name = 'SEQ_ILCAD001_CD_Usuario'
)
BEGIN
  -- Não inferir o próximo código de dados existentes nem reiniciar sequências.
  -- Uma tabela legada preenchida sem sequência requer reconciliação explícita.
  IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  BEGIN
    DECLARE @PossuiDados BIT = 0;
    EXEC sys.sp_executesql
      N'SELECT @Existe = CASE WHEN EXISTS (SELECT 1 FROM dbo.ILCAD001) THEN 1 ELSE 0 END;'
     ,N'@Existe BIT OUTPUT'
     ,@Existe = @PossuiDados OUTPUT;

    IF @PossuiDados = 1
    BEGIN
      THROW 51005, 'ILCAD001 possui dados sem sequência; reconciliar os códigos antes de criar a sequência.', 1;
    END;
  END;

  CREATE SEQUENCE dbo.SEQ_ILCAD001_CD_Usuario
    AS INT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    MAXVALUE 999
    NO CYCLE;
END;

IF EXISTS
(
  SELECT 1
  FROM sys.sequences
  WHERE schema_id = SCHEMA_ID('dbo')
    AND name = 'SEQ_ILCAD001_CD_Usuario'
    AND
    (
      system_type_id <> TYPE_ID('int')
      OR minimum_value <> 1
      OR maximum_value <> 999
      OR increment <> 1
      OR is_cycling = 1
    )
)
BEGIN
  THROW 51006, 'SEQ_ILCAD001_CD_Usuario incompatível: requer INT, faixa 1..999, incremento 1 e NO CYCLE.', 1;
END;
GO

IF OBJECT_ID('dbo.ILCAD001', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILCAD001
  (
    ID_Usuario INT IDENTITY(1,1) NOT NULL
   ,CD_Usuario CHAR(3) NOT NULL
   ,NM_Usuario VARCHAR(80) NOT NULL
   ,CPF        VARCHAR(11) NOT NULL
   ,DS_Email VARCHAR(255) NOT NULL
   ,TX_Senha VARCHAR(MAX) NOT NULL -- Exclusivamente hash; nunca senha em texto puro.
   ,FL_Ativo BIT NOT NULL
   ,DH_Inclusao DATETIME2(0) NOT NULL
  );
END;
GO

-- Evolução aditiva: aplicável também a tabelas já existentes.
IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD001', 'DH_Alteracao') IS NULL
BEGIN
  ALTER TABLE dbo.ILCAD001
  ADD DH_Alteracao DATETIME2(0) NULL;
END;
GO

IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD001', 'ID_Usuario') IS NOT NULL
BEGIN
  IF COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD001'), 'ID_Usuario', 'IsIdentity') <> 1
  BEGIN
    THROW 51002, 'ILCAD001.ID_Usuario deve ser IDENTITY; revisar a estrutura existente.', 1;
  END;

  IF NOT EXISTS
  (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.ILCAD001')
      AND type = 'PK'
  )
  BEGIN
    IF EXISTS
    (
      SELECT ID_Usuario
      FROM dbo.ILCAD001
      GROUP BY ID_Usuario
      HAVING COUNT_BIG(*) > 1
    )
      OR EXISTS
    (
      SELECT 1
      FROM dbo.ILCAD001
      WHERE ID_Usuario IS NULL
    )
    BEGIN
      THROW 51003, 'Dados incompatíveis com a PK de ILCAD001.', 1;
    END;

    ALTER TABLE dbo.ILCAD001
    ADD CONSTRAINT PK_ILCAD001 PRIMARY KEY (ID_Usuario);
  END;

  IF NOT EXISTS
  (
    SELECT 1
    FROM sys.key_constraints Chave
         INNER JOIN sys.index_columns Coluna
         ON Coluna.object_id = Chave.parent_object_id
           AND Coluna.index_id = Chave.unique_index_id
    WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCAD001')
      AND Chave.type = 'PK'
      AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD001'), 'ID_Usuario', 'ColumnId')
      AND Coluna.key_ordinal = 1
      AND NOT EXISTS
      (
        SELECT 1
        FROM sys.index_columns OutraColuna
        WHERE OutraColuna.object_id = Coluna.object_id
          AND OutraColuna.index_id = Coluna.index_id
          AND OutraColuna.key_ordinal > 1
      )
  )
  BEGIN
    THROW 51004, 'Chave primária incompatível em ILCAD001; revisar a estrutura existente.', 1;
  END;
END;
GO

IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD001', 'CD_Usuario') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.key_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCAD001')
    AND name = 'UK_ILCAD001_CD_Usuario'
)
  AND NOT EXISTS
(
  -- Uma UNIQUE equivalente já fornece a restrição e o índice necessários.
  SELECT 1
  FROM sys.key_constraints Chave
       INNER JOIN sys.index_columns Coluna
       ON Coluna.object_id = Chave.parent_object_id
         AND Coluna.index_id = Chave.unique_index_id
  WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCAD001')
    AND Chave.type = 'UQ'
    AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD001'), 'CD_Usuario', 'ColumnId')
    AND Coluna.key_ordinal = 1
    AND NOT EXISTS
    (
      SELECT 1
      FROM sys.index_columns OutraColuna
      WHERE OutraColuna.object_id = Coluna.object_id
        AND OutraColuna.index_id = Coluna.index_id
        AND OutraColuna.key_ordinal > 1
    )
)
BEGIN
  IF EXISTS
  (
    SELECT CD_Usuario
    FROM dbo.ILCAD001
    GROUP BY CD_Usuario
    HAVING COUNT_BIG(*) > 1
  )
  BEGIN
    THROW 51000, 'Valores duplicados em ILCAD001.CD_Usuario; UNIQUE não criada.', 1;
  END;

  ALTER TABLE dbo.ILCAD001
  ADD CONSTRAINT UK_ILCAD001_CD_Usuario UNIQUE (CD_Usuario);
END;
GO

IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD001', 'CPF') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.key_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCAD001')
    AND name = 'UK_ILCAD001_CPF'
)
  AND NOT EXISTS
(
  -- Uma UNIQUE equivalente já fornece a restrição e o índice necessários.
  SELECT 1
  FROM sys.key_constraints Chave
       INNER JOIN sys.index_columns Coluna
       ON Coluna.object_id = Chave.parent_object_id
         AND Coluna.index_id = Chave.unique_index_id
  WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCAD001')
    AND Chave.type = 'UQ'
    AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD001'), 'CPF', 'ColumnId')
    AND Coluna.key_ordinal = 1
    AND NOT EXISTS
    (
      SELECT 1
      FROM sys.index_columns OutraColuna
      WHERE OutraColuna.object_id = Coluna.object_id
        AND OutraColuna.index_id = Coluna.index_id
        AND OutraColuna.key_ordinal > 1
    )
)
BEGIN
  IF EXISTS
  (
    SELECT CPF
    FROM dbo.ILCAD001
    GROUP BY CPF
    HAVING COUNT_BIG(*) > 1
  )
  BEGIN
    THROW 51008, 'Valores duplicados em ILCAD001.CPF; UNIQUE não criada.', 1;
  END;

  ALTER TABLE dbo.ILCAD001
  ADD CONSTRAINT UK_ILCAD001_CPF UNIQUE (CPF);
END;
GO

IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD001', 'DS_Email') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.key_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCAD001')
    AND name = 'UK_ILCAD001_DS_Email'
)
  AND NOT EXISTS
(
  -- Uma UNIQUE equivalente já fornece a restrição e o índice necessários.
  SELECT 1
  FROM sys.key_constraints Chave
       INNER JOIN sys.index_columns Coluna
       ON Coluna.object_id = Chave.parent_object_id
         AND Coluna.index_id = Chave.unique_index_id
  WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCAD001')
    AND Chave.type = 'UQ'
    AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD001'), 'DS_Email', 'ColumnId')
    AND Coluna.key_ordinal = 1
    AND NOT EXISTS
    (
      SELECT 1
      FROM sys.index_columns OutraColuna
      WHERE OutraColuna.object_id = Coluna.object_id
        AND OutraColuna.index_id = Coluna.index_id
        AND OutraColuna.key_ordinal > 1
    )
)
BEGIN
  IF EXISTS
  (
    SELECT DS_Email
    FROM dbo.ILCAD001
    GROUP BY DS_Email
    HAVING COUNT_BIG(*) > 1
  )
  BEGIN
    THROW 51000, 'Valores duplicados em ILCAD001.DS_Email; UNIQUE não criada.', 1;
  END;

  ALTER TABLE dbo.ILCAD001
  ADD CONSTRAINT UK_ILCAD001_DS_Email UNIQUE (DS_Email);
END;
GO

IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD001', 'CD_Usuario') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCAD001')
    AND
    (
      name = 'DF_ILCAD001_CD_Usuario'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD001'), 'CD_Usuario', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCAD001
  ADD CONSTRAINT DF_ILCAD001_CD_Usuario DEFAULT (RIGHT('000' + CONVERT(VARCHAR(3), NEXT VALUE FOR dbo.SEQ_ILCAD001_CD_Usuario), 3)) FOR CD_Usuario;
END;
GO

IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD001', 'FL_Ativo') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCAD001')
    AND
    (
      name = 'DF_ILCAD001_FL_Ativo'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD001'), 'FL_Ativo', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCAD001
  ADD CONSTRAINT DF_ILCAD001_FL_Ativo DEFAULT (1) FOR FL_Ativo;
END;
GO

IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD001', 'DH_Inclusao') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCAD001')
    AND
    (
      name = 'DF_ILCAD001_DH_Inclusao'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD001'), 'DH_Inclusao', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCAD001
  ADD CONSTRAINT DF_ILCAD001_DH_Inclusao DEFAULT (SYSDATETIME()) FOR DH_Inclusao;
END;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM dbo.ILCAD000
  WHERE NM_Fisico = 'ILCAD001'
)
BEGIN
  INSERT INTO dbo.ILCAD000
  (
    NM_Fisico
   ,NM_Logico
  )
  VALUES
  (
    'ILCAD001'
   ,'Cadastro de Usuários'
  );
END;
GO

-- Uma configuração por usuário. Percentuais: 6.0000 representa 6%.
-- P/L e Dívida/Patrimônio são razões. Os critérios são informados explicitamente.
IF OBJECT_ID('dbo.ILCFG001', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILCFG001
  (
    ID_Configuracao INT IDENTITY(1,1) NOT NULL
   ,ID_Usuario INT NOT NULL
   ,PE_PL_Maximo DECIMAL(8,4) NOT NULL
   ,PE_DY_Minimo DECIMAL(8,4) NOT NULL
   ,PE_ROE_Minimo DECIMAL(8,4) NOT NULL
   ,PE_DividaPatrimonio_Maximo DECIMAL(8,4) NOT NULL
   ,PE_MargemLiquida_Minimo DECIMAL(8,4) NOT NULL
   ,PE_RetornoPreco DECIMAL(8,4) NOT NULL
   ,DH_Inclusao DATETIME2(0) NOT NULL
   ,DH_Alteracao DATETIME2(0) NULL
  );
END;
GO

-- Evolução aditiva para a versão anterior da tabela.
IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'DH_Alteracao') IS NULL
BEGIN
  ALTER TABLE dbo.ILCFG001
  ADD DH_Alteracao DATETIME2(0) NULL;
END;
GO

-- Rejeitar estruturas desconhecidas antes de alterar constraints ou colunas legadas.
IF EXISTS
(
  SELECT 1
  FROM (VALUES ('ID_Configuracao', 'int', 0, 0, 0)
              ,('ID_Usuario', 'int', 0, 0, 0)
              ,('PE_PL_Maximo', 'decimal', 8, 4, 0)
              ,('PE_DY_Minimo', 'decimal', 8, 4, 0)
              ,('PE_ROE_Minimo', 'decimal', 8, 4, 0)
              ,('PE_DividaPatrimonio_Maximo', 'decimal', 8, 4, 0)
              ,('PE_MargemLiquida_Minimo', 'decimal', 8, 4, 0)
              ,('PE_RetornoPreco', 'decimal', 8, 4, 0)
              ,('DH_Inclusao', 'datetime2', 0, 0, 0)
              ,('DH_Alteracao', 'datetime2', 0, 0, 1)) Esperado(NM_Coluna, NM_Tipo, NO_Precisao, NO_Escala, FL_Nulo)
       LEFT JOIN sys.columns Coluna
       ON Coluna.object_id = OBJECT_ID('dbo.ILCFG001')
         AND Coluna.name = Esperado.NM_Coluna
  WHERE Coluna.column_id IS NULL
    OR Coluna.system_type_id <> TYPE_ID(Esperado.NM_Tipo)
    OR Coluna.is_nullable <> Esperado.FL_Nulo
    OR Coluna.is_computed = 1
    OR (Esperado.NM_Tipo = 'decimal' AND Coluna.precision <> Esperado.NO_Precisao)
    OR (Esperado.NM_Tipo IN ('decimal', 'datetime2') AND Coluna.scale <> Esperado.NO_Escala)
)
BEGIN
  THROW 51020, 'Estrutura incompatível em ILCFG001; revisar tipos, nulabilidade e colunas obrigatórias.', 1;
END;

IF EXISTS
(
  SELECT ID_Usuario
  FROM dbo.ILCFG001
  GROUP BY ID_Usuario
  HAVING COUNT_BIG(*) > 1
)
BEGIN
  THROW 51021, 'ILCFG001 possui mais de uma configuração por usuário; reconciliar os dados antes de migrar.', 1;
END;
GO

-- Preservar dados legados sem exigir código, nome ou flag nas novas inclusões.
-- A sequência antiga, se existente, permanece inalterada e deixa de ser utilizada.
IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
BEGIN
  DECLARE @NO_Tran Integer = @@TRANCOUNT
         ,@TX_ComandoSQL NVARCHAR(MAX);

  BEGIN TRY
    IF (@NO_Tran = 0) BEGIN TRAN;

    IF EXISTS
    (
      SELECT 1
      FROM sys.columns Coluna
           INNER JOIN (VALUES ('CD_Configuracao', 'char', 3)
                             ,('NM_Configuracao', 'varchar', 80)
                             ,('FL_Ativo', 'bit', 1)) Esperado(NM_Coluna, NM_Tipo, NO_Tamanho)
           ON Coluna.name = Esperado.NM_Coluna
      WHERE Coluna.object_id = OBJECT_ID('dbo.ILCFG001')
        AND (Coluna.system_type_id <> TYPE_ID(Esperado.NM_Tipo)
          OR Coluna.max_length <> Esperado.NO_Tamanho
          OR Coluna.is_computed = 1)
    )
    BEGIN
      THROW 51022, 'Colunas legadas de ILCFG001 incompatíveis; revisar antes de migrar.', 1;
    END;

    SELECT @TX_ComandoSQL = STRING_AGG(CAST(N'ALTER TABLE dbo.ILCFG001 DROP CONSTRAINT ' + QUOTENAME(Padrao.name) + N';' AS NVARCHAR(MAX)), NCHAR(10))
    FROM sys.default_constraints Padrao
         INNER JOIN sys.columns Coluna
         ON Coluna.object_id = Padrao.parent_object_id
           AND Coluna.column_id = Padrao.parent_column_id
    WHERE Padrao.parent_object_id = OBJECT_ID('dbo.ILCFG001')
      AND Coluna.name IN ('CD_Configuracao', 'NM_Configuracao', 'FL_Ativo'
                        ,'PE_PL_Maximo', 'PE_DY_Minimo', 'PE_ROE_Minimo'
                        ,'PE_DividaPatrimonio_Maximo', 'PE_MargemLiquida_Minimo', 'PE_RetornoPreco');

    IF (@TX_ComandoSQL IS NOT NULL) EXEC sys.sp_executesql @TX_ComandoSQL;

    -- A UNIQUE antiga só admite um NULL, impedindo novas inclusões sem código.
    SELECT @TX_ComandoSQL = STRING_AGG(CAST(N'ALTER TABLE dbo.ILCFG001 DROP CONSTRAINT ' + QUOTENAME(Chave.name) + N';' AS NVARCHAR(MAX)), NCHAR(10))
    FROM sys.key_constraints Chave
         INNER JOIN sys.index_columns Coluna
         ON Coluna.object_id = Chave.parent_object_id
           AND Coluna.index_id = Chave.unique_index_id
    WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCFG001')
      AND Chave.type = 'UQ'
      AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'CD_Configuracao', 'ColumnId')
      AND Coluna.key_ordinal = 1
      AND NOT EXISTS
      (
        SELECT 1
        FROM sys.index_columns OutraColuna
        WHERE OutraColuna.object_id = Coluna.object_id
          AND OutraColuna.index_id = Coluna.index_id
          AND OutraColuna.key_ordinal > 1
      );

    IF (@TX_ComandoSQL IS NOT NULL) EXEC sys.sp_executesql @TX_ComandoSQL;

    SELECT @TX_ComandoSQL = STRING_AGG(CAST(N'ALTER TABLE dbo.ILCFG001 ALTER COLUMN ' + QUOTENAME(Coluna.name) + N' ' + Esperado.NM_Tipo + N' NULL;' AS NVARCHAR(MAX)), NCHAR(10))
    FROM sys.columns Coluna
         INNER JOIN (VALUES ('CD_Configuracao', 'CHAR(3)')
                           ,('NM_Configuracao', 'VARCHAR(80)')
                           ,('FL_Ativo', 'BIT')) Esperado(NM_Coluna, NM_Tipo)
         ON Coluna.name = Esperado.NM_Coluna
    WHERE Coluna.object_id = OBJECT_ID('dbo.ILCFG001')
      AND Coluna.is_nullable = 0;

    IF (@TX_ComandoSQL IS NOT NULL) EXEC sys.sp_executesql @TX_ComandoSQL;

    IF (@NO_Tran = 0 AND @@TRANCOUNT > 0) COMMIT TRAN;
  END TRY
  BEGIN CATCH
    IF (@NO_Tran = 0 AND XACT_STATE() <> 0) ROLLBACK TRAN;

    THROW;
  END CATCH;
END;
GO


IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'ID_Configuracao') IS NOT NULL
BEGIN
  IF COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'ID_Configuracao', 'IsIdentity') <> 1
  BEGIN
    THROW 51002, 'ILCFG001.ID_Configuracao deve ser IDENTITY; revisar a estrutura existente.', 1;
  END;

  IF NOT EXISTS
  (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
      AND type = 'PK'
  )
  BEGIN
    IF EXISTS
    (
      SELECT ID_Configuracao
      FROM dbo.ILCFG001
      GROUP BY ID_Configuracao
      HAVING COUNT_BIG(*) > 1
    )
      OR EXISTS
    (
      SELECT 1
      FROM dbo.ILCFG001
      WHERE ID_Configuracao IS NULL
    )
    BEGIN
      THROW 51003, 'Dados incompatíveis com a PK de ILCFG001.', 1;
    END;

    ALTER TABLE dbo.ILCFG001
    ADD CONSTRAINT PK_ILCFG001 PRIMARY KEY (ID_Configuracao);
  END;

  IF NOT EXISTS
  (
    SELECT 1
    FROM sys.key_constraints Chave
         INNER JOIN sys.index_columns Coluna
         ON Coluna.object_id = Chave.parent_object_id
           AND Coluna.index_id = Chave.unique_index_id
    WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCFG001')
      AND Chave.type = 'PK'
      AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'ID_Configuracao', 'ColumnId')
      AND Coluna.key_ordinal = 1
      AND NOT EXISTS
      (
        SELECT 1
        FROM sys.index_columns OutraColuna
        WHERE OutraColuna.object_id = Coluna.object_id
          AND OutraColuna.index_id = Coluna.index_id
          AND OutraColuna.key_ordinal > 1
      )
  )
  BEGIN
    THROW 51004, 'Chave primária incompatível em ILCFG001; revisar a estrutura existente.', 1;
  END;
END;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'ID_Usuario') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.key_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND name = 'UK_ILCFG001_ID_Usuario'
)
  AND NOT EXISTS
(
  -- Uma UNIQUE equivalente já fornece a restrição e o índice necessários.
  SELECT 1
  FROM sys.key_constraints Chave
       INNER JOIN sys.index_columns Coluna
       ON Coluna.object_id = Chave.parent_object_id
         AND Coluna.index_id = Chave.unique_index_id
  WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND Chave.type = 'UQ'
    AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'ID_Usuario', 'ColumnId')
    AND Coluna.key_ordinal = 1
    AND NOT EXISTS
    (
      SELECT 1
      FROM sys.index_columns OutraColuna
      WHERE OutraColuna.object_id = Coluna.object_id
        AND OutraColuna.index_id = Coluna.index_id
        AND OutraColuna.key_ordinal > 1
    )
)
BEGIN
  IF EXISTS
  (
    SELECT ID_Usuario
    FROM dbo.ILCFG001
    GROUP BY ID_Usuario
    HAVING COUNT_BIG(*) > 1
  )
  BEGIN
    THROW 51000, 'Valores duplicados em ILCFG001.ID_Usuario; UNIQUE não criada.', 1;
  END;

  ALTER TABLE dbo.ILCFG001
  ADD CONSTRAINT UK_ILCFG001_ID_Usuario UNIQUE (ID_Usuario);
END;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'DH_Inclusao') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND
    (
      name = 'DF_ILCFG001_DH_Inclusao'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'DH_Inclusao', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCFG001
  ADD CONSTRAINT DF_ILCFG001_DH_Inclusao DEFAULT (SYSDATETIME()) FOR DH_Inclusao;
END;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'ID_Usuario') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD001', 'ID_Usuario') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.foreign_keys
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND name = 'FK_ILCFG001_ILCAD001'
)
BEGIN
  IF EXISTS
  (
    SELECT 1
    FROM dbo.ILCFG001 Configuracao
         LEFT JOIN dbo.ILCAD001 Usuario
         ON Usuario.ID_Usuario = Configuracao.ID_Usuario
    WHERE Usuario.ID_Usuario IS NULL
  )
  BEGIN
    THROW 51007, 'ILCFG001 contém usuários inexistentes; corrigir os dados antes de criar a FK.', 1;
  END;

  -- A PK de ILCAD001.ID_Usuario foi validada no bloco anterior.
  ALTER TABLE dbo.ILCFG001 WITH CHECK
  ADD CONSTRAINT FK_ILCFG001_ILCAD001
    FOREIGN KEY (ID_Usuario) REFERENCES dbo.ILCAD001 (ID_Usuario);
END;
GO

-- Não aceitar constraints com o nome esperado, mas com definição incompatível.
IF NOT EXISTS
(
  SELECT 1
  FROM sys.key_constraints Chave
       INNER JOIN sys.indexes Indice
       ON Indice.object_id = Chave.parent_object_id
         AND Indice.index_id = Chave.unique_index_id
       INNER JOIN sys.index_columns Coluna
       ON Coluna.object_id = Chave.parent_object_id
         AND Coluna.index_id = Chave.unique_index_id
  WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND Chave.type = 'UQ'
    AND Indice.is_disabled = 0
    AND Indice.ignore_dup_key = 0
    AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'ID_Usuario', 'ColumnId')
    AND Coluna.key_ordinal = 1
    AND NOT EXISTS
    (
      SELECT 1
      FROM sys.index_columns OutraColuna
      WHERE OutraColuna.object_id = Coluna.object_id
        AND OutraColuna.index_id = Coluna.index_id
        AND OutraColuna.key_ordinal > 1
    )
)
BEGIN
  THROW 51023, 'UNIQUE de ILCFG001.ID_Usuario incompatível; revisar a estrutura existente.', 1;
END;

IF NOT EXISTS
(
  SELECT 1
  FROM sys.foreign_keys Chave
       INNER JOIN sys.foreign_key_columns Coluna
       ON Coluna.constraint_object_id = Chave.object_id
  WHERE Chave.parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND Chave.name = 'FK_ILCFG001_ILCAD001'
    AND Chave.referenced_object_id = OBJECT_ID('dbo.ILCAD001')
    AND Chave.delete_referential_action = 0
    AND Chave.update_referential_action = 0
    AND Coluna.parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'ID_Usuario', 'ColumnId')
    AND Coluna.referenced_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCAD001'), 'ID_Usuario', 'ColumnId')
    AND NOT EXISTS
    (
      SELECT 1
      FROM sys.foreign_key_columns OutraColuna
      WHERE OutraColuna.constraint_object_id = Chave.object_id
        AND OutraColuna.constraint_column_id > 1
    )
)
BEGIN
  THROW 51024, 'FK de ILCFG001.ID_Usuario incompatível; não utilizar cascata.', 1;
END;

ALTER TABLE dbo.ILCFG001 WITH CHECK CHECK CONSTRAINT FK_ILCFG001_ILCAD001;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.check_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND name = 'CK_ILCFG001_Criterios'
)
BEGIN
  IF EXISTS
  (
    SELECT 1
    FROM dbo.ILCFG001
    WHERE PE_PL_Maximo < 0
      OR PE_DY_Minimo < 0
      OR PE_ROE_Minimo < 0
      OR PE_DividaPatrimonio_Maximo < 0
      OR PE_MargemLiquida_Minimo < 0
      OR PE_RetornoPreco <= 0
  )
  BEGIN
    THROW 51025, 'ILCFG001 contém critérios inválidos; corrigir antes de criar a CHECK.', 1;
  END;

  ALTER TABLE dbo.ILCFG001 WITH CHECK
  ADD CONSTRAINT CK_ILCFG001_Criterios CHECK
  (
    PE_PL_Maximo >= 0
    AND PE_DY_Minimo >= 0
    AND PE_ROE_Minimo >= 0
    AND PE_DividaPatrimonio_Maximo >= 0
    AND PE_MargemLiquida_Minimo >= 0
    AND PE_RetornoPreco > 0
  );
END;
GO

ALTER TABLE dbo.ILCFG001 WITH CHECK CHECK CONSTRAINT CK_ILCFG001_Criterios;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM dbo.ILCAD000
  WHERE NM_Fisico = 'ILCFG001'
)
BEGIN
  INSERT INTO dbo.ILCAD000
  (
    NM_Fisico
   ,NM_Logico
  )
  VALUES
  (
    'ILCFG001'
   ,'Configuração de Critérios de Análise'
  );
END;
GO

-- Financeiro: cadastros independentes de empresa e ativo, seguidos por
-- snapshots históricos. A fonte é textual para preservar rastreabilidade sem
-- antecipar um cadastro genérico de provedores.
IF OBJECT_ID('dbo.ILFIN001', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILFIN001
  (
    ID_Empresa INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ILFIN001 PRIMARY KEY
   ,NM_Empresa VARCHAR(80) NULL
   ,NM_RazaoSocial VARCHAR(255) NULL
   ,DS_Setor VARCHAR(255) NULL
   ,CD_TickerPrimario VARCHAR(20) COLLATE Latin1_General_100_BIN2 NULL
   ,NM_Fonte VARCHAR(80) NOT NULL
   ,DH_Inclusao DATETIME2(0) NOT NULL CONSTRAINT DF_ILFIN001_DH_Inclusao DEFAULT (SYSDATETIME())
   ,DH_Alteracao DATETIME2(0) NULL
  );
END;
GO

IF OBJECT_ID('dbo.ILFIN001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILFIN001', 'CD_TickerPrimario') IS NULL
BEGIN
  ALTER TABLE dbo.ILFIN001
  ADD CD_TickerPrimario VARCHAR(20) COLLATE Latin1_General_100_BIN2 NULL;
END;
GO

IF OBJECT_ID('dbo.ILFIN001', 'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ILFIN001') AND name = 'UX_ILFIN001_CD_TickerPrimario')
BEGIN
  CREATE UNIQUE INDEX UX_ILFIN001_CD_TickerPrimario
  ON dbo.ILFIN001 (CD_TickerPrimario)
  WHERE CD_TickerPrimario IS NOT NULL;
END;
GO

IF OBJECT_ID('dbo.ILFIN002', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILFIN002
  (
    ID_Ativo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ILFIN002 PRIMARY KEY
   ,ID_Empresa INT NOT NULL CONSTRAINT FK_ILFIN002_ILFIN001 REFERENCES dbo.ILFIN001(ID_Empresa)
   ,CD_Ticker VARCHAR(20) COLLATE Latin1_General_100_BIN2 NOT NULL
   ,NM_Fonte VARCHAR(80) NOT NULL
   ,DH_Inclusao DATETIME2(0) NOT NULL CONSTRAINT DF_ILFIN002_DH_Inclusao DEFAULT (SYSDATETIME())
   ,DH_Alteracao DATETIME2(0) NULL
   ,CONSTRAINT UK_ILFIN002_CD_Ticker UNIQUE (CD_Ticker)
   ,CONSTRAINT CK_ILFIN002_CD_Ticker_Normalizado CHECK
    (CD_Ticker = UPPER(LTRIM(RTRIM(CD_Ticker)))
     AND LEN(CD_Ticker) > 0
     AND DATALENGTH(CD_Ticker) = LEN(CD_Ticker))
  );

  CREATE INDEX IX_ILFIN002_ID_Empresa
  ON dbo.ILFIN002 (ID_Empresa);
END;
GO

-- Indicadores contábeis são da empresa. Percentuais seguem a representação
-- humana do InvestLens; dívida/patrimônio permanece um múltiplo.
IF OBJECT_ID('dbo.ILFIN003', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILFIN003
  (
    ID_IndicadorEmpresa INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ILFIN003 PRIMARY KEY
   ,ID_Empresa INT NOT NULL CONSTRAINT FK_ILFIN003_ILFIN001 REFERENCES dbo.ILFIN001(ID_Empresa)
   ,NM_Fonte VARCHAR(80) NOT NULL
   ,DT_Referencia DATE NULL
   ,DH_Coleta DATETIME2(0) NOT NULL
   ,PE_ROE DECIMAL(18,8) NULL
   ,VL_DividaPatrimonio DECIMAL(18,8) NULL
   ,PE_MargemLiquida DECIMAL(18,8) NULL
   ,CONSTRAINT CK_ILFIN003_Indicador CHECK
    (PE_ROE IS NOT NULL OR VL_DividaPatrimonio IS NOT NULL OR PE_MargemLiquida IS NOT NULL)
  );

  CREATE UNIQUE INDEX UX_ILFIN003_Empresa_Fonte_Referencia
  ON dbo.ILFIN003 (ID_Empresa, NM_Fonte, DT_Referencia)
  WHERE DT_Referencia IS NOT NULL;
END;
GO

-- P/L e Dividend Yield dependem da negociação do ativo e, portanto, não são
-- compartilhados entre os ativos de uma mesma empresa.
IF OBJECT_ID('dbo.ILFIN004', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILFIN004
  (
    ID_IndicadorAtivo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ILFIN004 PRIMARY KEY
   ,ID_Ativo INT NOT NULL CONSTRAINT FK_ILFIN004_ILFIN002 REFERENCES dbo.ILFIN002(ID_Ativo)
   ,NM_Fonte VARCHAR(80) NOT NULL
   ,DT_Referencia DATE NULL
   ,DH_Coleta DATETIME2(0) NOT NULL
   ,VL_PL DECIMAL(18,8) NULL
   ,PE_DividendYield DECIMAL(18,8) NULL
   ,CONSTRAINT CK_ILFIN004_Indicador CHECK
    (VL_PL IS NOT NULL OR PE_DividendYield IS NOT NULL)
  );

  CREATE UNIQUE INDEX UX_ILFIN004_Ativo_Fonte_Referencia
  ON dbo.ILFIN004 (ID_Ativo, NM_Fonte, DT_Referencia)
  WHERE DT_Referencia IS NOT NULL;
END;
GO

-- Cada linha é uma cotação efetivamente datada pela fonte. Não há coluna de
-- "cotação atual": a consulta atual é obtida pela maior data de referência.
IF OBJECT_ID('dbo.ILFIN005', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILFIN005
  (
    ID_Cotacao INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ILFIN005 PRIMARY KEY
   ,ID_Ativo INT NOT NULL CONSTRAINT FK_ILFIN005_ILFIN002 REFERENCES dbo.ILFIN002(ID_Ativo)
   ,NM_Fonte VARCHAR(80) NOT NULL
   ,DT_Referencia DATE NULL
   ,DH_Coleta DATETIME2(0) NOT NULL
   ,VL_PrecoFechamento DECIMAL(18,8) NOT NULL
  );

  CREATE UNIQUE INDEX UX_ILFIN005_Ativo_Fonte_Referencia
  ON dbo.ILFIN005 (ID_Ativo, NM_Fonte, DT_Referencia)
  WHERE DT_Referencia IS NOT NULL;
END;
GO

-- O registro do catálogo acompanha cada tabela financeira e pode ser
-- executado repetidamente sem duplicar metadados.
IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILFIN001', 'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILFIN001')
  INSERT dbo.ILCAD000(NM_Fisico, NM_Logico) VALUES ('ILFIN001', 'Cadastro de Empresas Emissoras');

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILFIN002', 'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILFIN002')
  INSERT dbo.ILCAD000(NM_Fisico, NM_Logico) VALUES ('ILFIN002', 'Cadastro de Ativos Negociados');

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILFIN003', 'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILFIN003')
  INSERT dbo.ILCAD000(NM_Fisico, NM_Logico) VALUES ('ILFIN003', 'Indicadores Fundamentalistas da Empresa');

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILFIN004', 'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILFIN004')
  INSERT dbo.ILCAD000(NM_Fisico, NM_Logico) VALUES ('ILFIN004', 'Indicadores Fundamentalistas do Ativo');

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILFIN005', 'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILFIN005')
  INSERT dbo.ILCAD000(NM_Fisico, NM_Logico) VALUES ('ILFIN005', 'Cotações de Fechamento');
GO

-- Planos de acesso. O nome é a identificação estável usada pela migração e
-- pelos cadastros iniciais; IDs IDENTITY não são assumidos pela aplicação.
IF OBJECT_ID('dbo.ILCAD002', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILCAD002
  (
    ID_Plano INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ILCAD002 PRIMARY KEY
   ,NM_Plano VARCHAR(80) NOT NULL
   ,VL_Plano DECIMAL(18,2) NOT NULL
   ,FL_Ativo BIT NOT NULL
   ,DH_Inclusao DATETIME2(0) NOT NULL CONSTRAINT DF_ILCAD002_DH_Inclusao DEFAULT (SYSDATETIME())
   ,DH_Alteracao DATETIME2(0) NULL
   ,CONSTRAINT UK_ILCAD002_NM_Plano UNIQUE (NM_Plano)
   ,CONSTRAINT CK_ILCAD002_VL_Plano CHECK (VL_Plano >= 0)
  );
END;
GO

IF OBJECT_ID('dbo.ILCAD002', 'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD002 WHERE NM_Plano = 'Free')
BEGIN
  INSERT dbo.ILCAD002 (NM_Plano, VL_Plano, FL_Ativo)
  VALUES ('Free', 0.00, 1);
END;

IF OBJECT_ID('dbo.ILCAD002', 'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD002 WHERE NM_Plano = 'Pro')
BEGIN
  INSERT dbo.ILCAD002 (NM_Plano, VL_Plano, FL_Ativo)
  VALUES ('Pro', 49.90, 1);
END;
GO

-- Em bancos existentes a associação é preenchida antes de se tornar obrigatória.
IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD001', 'ID_Plano') IS NULL
BEGIN
  ALTER TABLE dbo.ILCAD001
  ADD ID_Plano INT NULL;
END;
GO

IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILCAD002', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCAD001', 'ID_Plano') IS NOT NULL
BEGIN
  DECLARE @ID_PlanoFree INT;

  SELECT @ID_PlanoFree = ID_Plano
  FROM dbo.ILCAD002
  WHERE NM_Plano = 'Free';

  IF @ID_PlanoFree IS NULL
  BEGIN
    THROW 51030, 'Plano Free não encontrado; não é possível migrar usuários.', 1;
  END;

  UPDATE dbo.ILCAD001
  SET ID_Plano = @ID_PlanoFree
  WHERE ID_Plano IS NULL;

  IF EXISTS (SELECT 1 FROM dbo.ILCAD001 WHERE ID_Plano IS NULL)
  BEGIN
    THROW 51031, 'Existem usuários sem plano; NOT NULL não aplicado.', 1;
  END;

  IF EXISTS
  (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ILCAD001')
      AND name = 'ID_Plano'
      AND is_nullable = 1
  )
  BEGIN
    ALTER TABLE dbo.ILCAD001
    ALTER COLUMN ID_Plano INT NOT NULL;
  END;
END;
GO

IF OBJECT_ID('dbo.ILCAD001', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILCAD002', 'U') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.foreign_keys
  WHERE parent_object_id = OBJECT_ID('dbo.ILCAD001')
    AND name = 'FK_ILCAD001_ILCAD002'
)
BEGIN
  IF EXISTS
  (
    SELECT 1
    FROM dbo.ILCAD001 Usuario
         LEFT JOIN dbo.ILCAD002 Plano
         ON Plano.ID_Plano = Usuario.ID_Plano
    WHERE Plano.ID_Plano IS NULL
  )
  BEGIN
    THROW 51032, 'ILCAD001 contém plano inexistente; FK não criada.', 1;
  END;

  ALTER TABLE dbo.ILCAD001 WITH CHECK
  ADD CONSTRAINT FK_ILCAD001_ILCAD002
    FOREIGN KEY (ID_Plano) REFERENCES dbo.ILCAD002 (ID_Plano);
END;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILCAD002', 'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILCAD002')
BEGIN
  INSERT dbo.ILCAD000 (NM_Fisico, NM_Logico)
  VALUES ('ILCAD002', 'Plano');
END;
GO

-- Consumo diário compartilhado da credencial bolsai; a janela é definida pela aplicação.
IF OBJECT_ID('dbo.ILLOG001', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILLOG001
  (
    ID_ConsumoApi INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ILLOG001 PRIMARY KEY
   ,NM_Fonte VARCHAR(80) NOT NULL
   ,DT_Janela DATE NOT NULL
   ,QT_Requisicao INT NOT NULL
   ,DH_Alteracao DATETIME2(0) NOT NULL CONSTRAINT DF_ILLOG001_DH_Alteracao DEFAULT (SYSDATETIME())
   ,CONSTRAINT UK_ILLOG001_Fonte_Janela UNIQUE (NM_Fonte, DT_Janela)
   ,CONSTRAINT CK_ILLOG001_QT_Requisicao CHECK (QT_Requisicao >= 0)
  );
END;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILLOG001', 'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILLOG001')
  INSERT dbo.ILCAD000(NM_Fisico,NM_Logico) VALUES ('ILLOG001','Consumo Diário de API');
GO

-- Refresh sessions: UTC timestamps, absolute lifetime, hashes only.
IF OBJECT_ID('dbo.ILAUT001', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILAUT001
  (
    ID_Sessao UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ILAUT001 PRIMARY KEY,
    ID_Usuario INT NOT NULL CONSTRAINT FK_ILAUT001_ILCAD001 REFERENCES dbo.ILCAD001(ID_Usuario),
    DH_Expiracao DATETIME2(7) NOT NULL,
    DH_Revogacao DATETIME2(7) NULL
  );
END;
GO

IF OBJECT_ID('dbo.ILAUT002', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILAUT002
  (
    TX_Hash CHAR(64) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT PK_ILAUT002 PRIMARY KEY,
    ID_Sessao UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_ILAUT002_ILAUT001 REFERENCES dbo.ILAUT001(ID_Sessao),
    DH_Consumo DATETIME2(7) NULL
  );
  CREATE INDEX IX_ILAUT002_ID_Sessao ON dbo.ILAUT002(ID_Sessao);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILAUT001')
  INSERT dbo.ILCAD000(NM_Fisico, NM_Logico) VALUES ('ILAUT001', 'Sessoes de autenticacao');
  
IF NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILAUT002')
  INSERT dbo.ILCAD000(NM_Fisico, NM_Logico) VALUES ('ILAUT002', 'Hashes de refresh tokens');
GO

-- Assinatura corrente por usuário. Vigência é [início, fim) em UTC.
IF OBJECT_ID('dbo.ILCAD003', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILCAD003
  (
    ID_Assinatura INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ILCAD003 PRIMARY KEY,
    ID_Usuario INT NOT NULL CONSTRAINT FK_ILCAD003_ILCAD001 REFERENCES dbo.ILCAD001(ID_Usuario),
    ID_Plano INT NOT NULL CONSTRAINT FK_ILCAD003_ILCAD002 REFERENCES dbo.ILCAD002(ID_Plano),
    DH_Inicio_Assinatura DATETIME2(0) NOT NULL,
    DH_Fim_Assinatura DATETIME2(0) NOT NULL,
    CD_Status VARCHAR(12) NOT NULL,
    DH_Inclusao DATETIME2(0) NOT NULL CONSTRAINT DF_ILCAD003_DH_Inclusao DEFAULT(SYSDATETIME()),
    DH_Alteracao DATETIME2(0) NULL,
    CONSTRAINT UK_ILCAD003_ID_Usuario UNIQUE(ID_Usuario),
    CONSTRAINT CK_ILCAD003_Status CHECK(CD_Status IN ('PENDENTE','ATIVA','EXPIRADA','CANCELADA')),
    CONSTRAINT CK_ILCAD003_Vigencia CHECK(DH_Inicio_Assinatura < DH_Fim_Assinatura)
  );
END;
GO

IF OBJECT_ID('dbo.ILCAD004', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILCAD004
  (
    ID_Pagamento INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ILCAD004 PRIMARY KEY,
    ID_Usuario INT NOT NULL CONSTRAINT FK_ILCAD004_ILCAD001 REFERENCES dbo.ILCAD001(ID_Usuario),
    ID_Assinatura INT NULL CONSTRAINT FK_ILCAD004_ILCAD003 REFERENCES dbo.ILCAD003(ID_Assinatura),
    ID_Plano INT NOT NULL CONSTRAINT FK_ILCAD004_ILCAD002 REFERENCES dbo.ILCAD002(ID_Plano),
    VL_Pagamento DECIMAL(18,2) NOT NULL,
    CD_Status VARCHAR(12) NOT NULL,
    DS_Referencia_Externa VARCHAR(255) NOT NULL,
    DH_Pagamento DATETIME2(0) NULL,
    DH_Inclusao DATETIME2(0) NOT NULL CONSTRAINT DF_ILCAD004_DH_Inclusao DEFAULT(SYSDATETIME()),
    DH_Alteracao DATETIME2(0) NULL,
    CONSTRAINT UK_ILCAD004_Referencia UNIQUE(DS_Referencia_Externa),
    CONSTRAINT CK_ILCAD004_Status CHECK(CD_Status IN ('PENDENTE','CONFIRMADO','RECUSADO','CANCELADO','ESTORNADO')),
    CONSTRAINT CK_ILCAD004_Valor CHECK(VL_Pagamento >= 0)
  );
END;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILCAD003')
  INSERT dbo.ILCAD000(NM_Fisico, NM_Logico) VALUES ('ILCAD003', 'Assinaturas');
IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILCAD004')
  INSERT dbo.ILCAD000(NM_Fisico, NM_Logico) VALUES ('ILCAD004', 'Pagamentos');
GO

-- Solicitações não alteram assinatura nem pagamento. PEN é o único estado pendente.
IF OBJECT_ID('dbo.ILCAD005', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILCAD005
  (
    ID_Solicitacao INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ILCAD005 PRIMARY KEY
   ,ID_Usuario INT NOT NULL CONSTRAINT FK_ILCAD005_ILCAD001 REFERENCES dbo.ILCAD001(ID_Usuario)
   ,ID_Assinatura INT NULL CONSTRAINT FK_ILCAD005_ILCAD003 REFERENCES dbo.ILCAD003(ID_Assinatura)
   ,ID_Plano_Origem INT NOT NULL CONSTRAINT FK_ILCAD005_PlanoOrigem REFERENCES dbo.ILCAD002(ID_Plano)
   ,ID_Plano_Destino INT NULL CONSTRAINT FK_ILCAD005_PlanoDestino REFERENCES dbo.ILCAD002(ID_Plano)
   ,CD_Tipo CHAR(3) NOT NULL
   ,CD_Status CHAR(3) NOT NULL
   ,DH_Inclusao DATETIME2(0) NOT NULL CONSTRAINT DF_ILCAD005_DH_Inclusao DEFAULT (SYSUTCDATETIME())
   ,DH_Alteracao DATETIME2(0) NULL
   ,CONSTRAINT CK_ILCAD005_Tipo CHECK ((CD_Tipo = 'ALT' AND ID_Plano_Destino IS NOT NULL AND ID_Plano_Destino <> ID_Plano_Origem) OR (CD_Tipo = 'CAN' AND ID_Plano_Destino IS NULL AND ID_Assinatura IS NOT NULL))
   ,CONSTRAINT CK_ILCAD005_Status CHECK (CD_Status IN ('PEN', 'CON', 'REJ'))
  );
  CREATE UNIQUE INDEX UX_ILCAD005_Usuario_Pendente ON dbo.ILCAD005(ID_Usuario) WHERE CD_Status = 'PEN';
END;
GO

IF OBJECT_ID('dbo.ILCAD000', 'U') IS NOT NULL
  AND OBJECT_ID('dbo.ILCAD005', 'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILCAD005')
  INSERT dbo.ILCAD000(NM_Fisico, NM_Logico) VALUES ('ILCAD005', 'Solicitações de assinatura');
GO
