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

IF NOT EXISTS
(
  SELECT 1
  FROM sys.sequences
  WHERE schema_id = SCHEMA_ID('dbo')
    AND name = 'SEQ_ILCFG001_CD_Configuracao'
)
BEGIN
  -- Não inferir o próximo código de dados existentes nem reiniciar sequências.
  -- Uma tabela legada preenchida sem sequência requer reconciliação explícita.
  IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  BEGIN
    DECLARE @PossuiDados BIT = 0;
    EXEC sys.sp_executesql
      N'SELECT @Existe = CASE WHEN EXISTS (SELECT 1 FROM dbo.ILCFG001) THEN 1 ELSE 0 END;'
     ,N'@Existe BIT OUTPUT'
     ,@Existe = @PossuiDados OUTPUT;

    IF @PossuiDados = 1
    BEGIN
      THROW 51005, 'ILCFG001 possui dados sem sequência; reconciliar os códigos antes de criar a sequência.', 1;
    END;
  END;

  CREATE SEQUENCE dbo.SEQ_ILCFG001_CD_Configuracao
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
    AND name = 'SEQ_ILCFG001_CD_Configuracao'
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
  THROW 51006, 'SEQ_ILCFG001_CD_Configuracao incompatível: requer INT, faixa 1..999, incremento 1 e NO CYCLE.', 1;
END;
GO

-- Percentuais: 6.0000 representa 6%; em cálculos futuros, dividir por 100.
-- PE_PL_Maximo e PE_DividaPatrimonio_Maximo são razões, por convenção do projeto.
-- Os critérios fornecem DEFAULTs; nenhuma configuração ou usuário é inserido.

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.ILCFG001
  (
    ID_Configuracao INT IDENTITY(1,1) NOT NULL
   ,ID_Usuario INT NOT NULL
   ,CD_Configuracao CHAR(3) NOT NULL
   ,NM_Configuracao VARCHAR(80) NOT NULL
   ,PE_PL_Maximo DECIMAL(8,4) NOT NULL
   ,PE_DY_Minimo DECIMAL(8,4) NOT NULL
   ,PE_ROE_Minimo DECIMAL(8,4) NOT NULL
   ,PE_DividaPatrimonio_Maximo DECIMAL(8,4) NOT NULL
   ,PE_MargemLiquida_Minimo DECIMAL(8,4) NOT NULL
   ,PE_RetornoPreco DECIMAL(8,4) NOT NULL
   ,FL_Ativo BIT NOT NULL
   ,DH_Inclusao DATETIME2(0) NOT NULL
  );
END;
GO

-- Evolução aditiva: aplicável também a tabelas já existentes.
IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'DH_Alteracao') IS NULL
BEGIN
  ALTER TABLE dbo.ILCFG001
  ADD DH_Alteracao DATETIME2(0) NULL;
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
  AND COL_LENGTH('dbo.ILCFG001', 'CD_Configuracao') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.key_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND name = 'UK_ILCFG001_CD_Configuracao'
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
    AND Coluna.column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'CD_Configuracao', 'ColumnId')
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
    SELECT CD_Configuracao
    FROM dbo.ILCFG001
    GROUP BY CD_Configuracao
    HAVING COUNT_BIG(*) > 1
  )
  BEGIN
    THROW 51000, 'Valores duplicados em ILCFG001.CD_Configuracao; UNIQUE não criada.', 1;
  END;

  ALTER TABLE dbo.ILCFG001
  ADD CONSTRAINT UK_ILCFG001_CD_Configuracao UNIQUE (CD_Configuracao);
END;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'CD_Configuracao') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND
    (
      name = 'DF_ILCFG001_CD_Configuracao'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'CD_Configuracao', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCFG001
  ADD CONSTRAINT DF_ILCFG001_CD_Configuracao DEFAULT (RIGHT('000' + CONVERT(VARCHAR(3), NEXT VALUE FOR dbo.SEQ_ILCFG001_CD_Configuracao), 3)) FOR CD_Configuracao;
END;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'PE_PL_Maximo') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND
    (
      name = 'DF_ILCFG001_PE_PL_Maximo'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'PE_PL_Maximo', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCFG001
  ADD CONSTRAINT DF_ILCFG001_PE_PL_Maximo DEFAULT (15.0000) FOR PE_PL_Maximo;
END;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'PE_DY_Minimo') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND
    (
      name = 'DF_ILCFG001_PE_DY_Minimo'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'PE_DY_Minimo', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCFG001
  ADD CONSTRAINT DF_ILCFG001_PE_DY_Minimo DEFAULT (6.0000) FOR PE_DY_Minimo;
END;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'PE_ROE_Minimo') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND
    (
      name = 'DF_ILCFG001_PE_ROE_Minimo'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'PE_ROE_Minimo', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCFG001
  ADD CONSTRAINT DF_ILCFG001_PE_ROE_Minimo DEFAULT (15.0000) FOR PE_ROE_Minimo;
END;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'PE_DividaPatrimonio_Maximo') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND
    (
      name = 'DF_ILCFG001_PE_DividaPatrimonio_Maximo'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'PE_DividaPatrimonio_Maximo', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCFG001
  ADD CONSTRAINT DF_ILCFG001_PE_DividaPatrimonio_Maximo DEFAULT (1.0000) FOR PE_DividaPatrimonio_Maximo;
END;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'PE_MargemLiquida_Minimo') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND
    (
      name = 'DF_ILCFG001_PE_MargemLiquida_Minimo'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'PE_MargemLiquida_Minimo', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCFG001
  ADD CONSTRAINT DF_ILCFG001_PE_MargemLiquida_Minimo DEFAULT (20.0000) FOR PE_MargemLiquida_Minimo;
END;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'PE_RetornoPreco') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND
    (
      name = 'DF_ILCFG001_PE_RetornoPreco'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'PE_RetornoPreco', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCFG001
  ADD CONSTRAINT DF_ILCFG001_PE_RetornoPreco DEFAULT (6.0000) FOR PE_RetornoPreco;
END;
GO

IF OBJECT_ID('dbo.ILCFG001', 'U') IS NOT NULL
  AND COL_LENGTH('dbo.ILCFG001', 'FL_Ativo') IS NOT NULL
  AND NOT EXISTS
(
  SELECT 1
  FROM sys.default_constraints
  WHERE parent_object_id = OBJECT_ID('dbo.ILCFG001')
    AND
    (
      name = 'DF_ILCFG001_FL_Ativo'
      OR parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.ILCFG001'), 'FL_Ativo', 'ColumnId')
    )
)
BEGIN
  ALTER TABLE dbo.ILCFG001
  ADD CONSTRAINT DF_ILCFG001_FL_Ativo DEFAULT (1) FOR FL_Ativo;
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
