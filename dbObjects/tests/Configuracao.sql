/*
  Executar SOMENTE em banco de teste isolado, após 001_CreateTables.sql,
  fn_DadosConfiguracao.sql, st_ConfiguracaoAdd.sql e st_ConfiguracaoUpd.sql.
  Exemplo: sqlcmd -S "(localdb)\MSSQLLocalDB" -d <banco-de-teste> -E -C -b -f 65001 -i dbObjects/tests/Configuracao.sql
  A carga de teste permanece no banco isolado para inspeção e teste de concorrência.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ID_Usuario Integer
       ,@ID_OutroUsuario Integer
       ,@ID_SemConfiguracao Integer
       ,@ID_Configuracao Integer
       ,@DH_Inclusao Datetime2(0)
       ,@ReturnCode Smallint
       ,@ErrMsg Varchar(255)
       ,@NO_Erro Integer
       ,@NO_Caso Integer = 0
       ,@NO_Procedimento Integer
       ,@PE_PL_Maximo Decimal(8,4)
       ,@PE_DY_Minimo Decimal(8,4)
       ,@PE_ROE_Minimo Decimal(8,4)
       ,@PE_DividaPatrimonio_Maximo Decimal(8,4)
       ,@PE_MargemLiquida_Minimo Decimal(8,4)
       ,@PE_RetornoPreco Decimal(8,4);

IF DB_NAME() NOT LIKE 'InvestLens_Config_Test[_]%'
BEGIN
  THROW 51999, 'Executar apenas em banco de teste InvestLens_Config_Test_*.', 1;
END;

INSERT INTO dbo.ILCAD001 (NM_Usuario
                         ,DS_Email
                         ,TX_Senha)
SELECT 'Teste configuração'
      ,'config1-' + CONVERT(VARCHAR(36),NEWID()) + '@example.test'
      ,'hash-exclusivo-de-teste';

SET @ID_Usuario = SCOPE_IDENTITY();

INSERT INTO dbo.ILCAD001 (NM_Usuario
                         ,DS_Email
                         ,TX_Senha)
SELECT 'Outro usuário'
      ,'config2-' + CONVERT(VARCHAR(36),NEWID()) + '@example.test'
      ,'hash-exclusivo-de-teste';

SET @ID_OutroUsuario = SCOPE_IDENTITY();

INSERT INTO dbo.ILCAD001 (NM_Usuario
                         ,DS_Email
                         ,TX_Senha)
SELECT 'Sem configuração'
      ,'config3-' + CONVERT(VARCHAR(36),NEWID()) + '@example.test'
      ,'hash-exclusivo-de-teste';

SET @ID_SemConfiguracao = SCOPE_IDENTITY();

EXEC dbo.st_ConfiguracaoAdd @ID_Usuario,15,6,15,1,20,6,@ReturnCode OUTPUT,@ErrMsg OUTPUT;

IF (@ReturnCode <> 0 OR @ErrMsg IS NOT NULL OR @@TRANCOUNT <> 0)
BEGIN
  THROW 51900, 'Contrato de sucesso do ADD inválido.', 1;
END;

IF NOT EXISTS(SELECT 1
              FROM dbo.fn_DadosConfiguracao(@ID_Usuario)
              WHERE PE_PL_Maximo = 15
                AND PE_DY_Minimo = 6
                AND PE_ROE_Minimo = 15
                AND PE_DividaPatrimonio_Maximo = 1
                AND PE_MargemLiquida_Minimo = 20
                AND PE_RetornoPreco = 6
                AND DH_Alteracao IS NULL)
BEGIN
  THROW 51901, 'Valores do ADD ou da function inválidos.', 1;
END;

IF EXISTS(SELECT 1 FROM dbo.fn_DadosConfiguracao(@ID_OutroUsuario))
BEGIN
  THROW 51902, 'Function retornou configuração de outro usuário.', 1;
END;

EXEC dbo.st_ConfiguracaoAdd @ID_OutroUsuario,0,0,0,0,0,0.0001,@ReturnCode OUTPUT,@ErrMsg OUTPUT;

SELECT @ID_Configuracao = ID_Configuracao
      ,@DH_Inclusao     = DH_Inclusao
FROM dbo.ILCFG001
WHERE ID_Usuario = @ID_Usuario;

EXEC dbo.st_ConfiguracaoUpd @ID_Usuario,16,7,17,2,21,8,@ReturnCode OUTPUT,@ErrMsg OUTPUT;

IF (@ReturnCode <> 0 OR @ErrMsg IS NOT NULL OR @@TRANCOUNT <> 0)
BEGIN
  THROW 51903, 'Contrato de sucesso do UPD inválido.', 1;
END;

IF NOT EXISTS(SELECT 1
              FROM dbo.fn_DadosConfiguracao(@ID_Usuario)
              WHERE ID_Configuracao = @ID_Configuracao
                AND DH_Inclusao = @DH_Inclusao
                AND DH_Alteracao IS NOT NULL
                AND PE_PL_Maximo = 16
                AND PE_DY_Minimo = 7
                AND PE_ROE_Minimo = 17
                AND PE_DividaPatrimonio_Maximo = 2
                AND PE_MargemLiquida_Minimo = 21
                AND PE_RetornoPreco = 8)
BEGIN
  THROW 51904, 'UPDATE não preservou identidade/datas ou não alterou os critérios.', 1;
END;

IF NOT EXISTS(SELECT 1
              FROM dbo.fn_DadosConfiguracao(@ID_OutroUsuario)
              WHERE PE_PL_Maximo = 0
                AND DH_Alteracao IS NULL)
BEGIN
  THROW 51905, 'UPDATE alterou outro usuário.', 1;
END;

-- THROW interrompe a cópia dos OUTPUTs ao chamador T-SQL; nas falhas verificar a exceção.
-- NULL e negativos em todos os critérios; retorno também rejeita zero.
WHILE (@NO_Caso < 13)
BEGIN
  SELECT @PE_PL_Maximo              = CASE @NO_Caso WHEN 0 THEN NULL WHEN 1 THEN -1 ELSE 15 END
        ,@PE_DY_Minimo              = CASE @NO_Caso WHEN 2 THEN NULL WHEN 3 THEN -1 ELSE 6 END
        ,@PE_ROE_Minimo             = CASE @NO_Caso WHEN 4 THEN NULL WHEN 5 THEN -1 ELSE 15 END
        ,@PE_DividaPatrimonio_Maximo = CASE @NO_Caso WHEN 6 THEN NULL WHEN 7 THEN -1 ELSE 1 END
        ,@PE_MargemLiquida_Minimo   = CASE @NO_Caso WHEN 8 THEN NULL WHEN 9 THEN -1 ELSE 20 END
        ,@PE_RetornoPreco           = CASE @NO_Caso WHEN 10 THEN NULL WHEN 11 THEN -1 WHEN 12 THEN 0 ELSE 6 END
        ,@NO_Procedimento          = 0;

  WHILE (@NO_Procedimento < 2)
  BEGIN
    SET @NO_Erro = 0;

    BEGIN TRY
      IF (@NO_Procedimento = 0)
        EXEC dbo.st_ConfiguracaoAdd @ID_SemConfiguracao,@PE_PL_Maximo,@PE_DY_Minimo,@PE_ROE_Minimo,@PE_DividaPatrimonio_Maximo,@PE_MargemLiquida_Minimo,@PE_RetornoPreco,@ReturnCode OUTPUT,@ErrMsg OUTPUT;
      ELSE
        EXEC dbo.st_ConfiguracaoUpd @ID_Usuario,@PE_PL_Maximo,@PE_DY_Minimo,@PE_ROE_Minimo,@PE_DividaPatrimonio_Maximo,@PE_MargemLiquida_Minimo,@PE_RetornoPreco,@ReturnCode OUTPUT,@ErrMsg OUTPUT;
    END TRY
    BEGIN CATCH
      SET @NO_Erro = ERROR_NUMBER();
    END CATCH;

    IF (@NO_Erro <> 50001 OR @@TRANCOUNT <> 0)
    BEGIN
      THROW 51906, 'Validação de critério/contrato de negócio inválida.', 1;
    END;

    SET @NO_Procedimento += 1;
  END;

  SET @NO_Caso += 1;
END;

-- Usuário inexistente, configuração duplicada e configuração ausente.
SET @NO_Caso = 0;

WHILE (@NO_Caso < 4)
BEGIN
  SET @NO_Erro = 0;

  BEGIN TRY
    IF (@NO_Caso = 0)
      EXEC dbo.st_ConfiguracaoAdd 2147483647,15,6,15,1,20,6,@ReturnCode OUTPUT,@ErrMsg OUTPUT;
    IF (@NO_Caso = 1)
      EXEC dbo.st_ConfiguracaoUpd 2147483647,15,6,15,1,20,6,@ReturnCode OUTPUT,@ErrMsg OUTPUT;
    IF (@NO_Caso = 2)
      EXEC dbo.st_ConfiguracaoAdd @ID_Usuario,15,6,15,1,20,6,@ReturnCode OUTPUT,@ErrMsg OUTPUT;
    IF (@NO_Caso = 3)
      EXEC dbo.st_ConfiguracaoUpd @ID_SemConfiguracao,15,6,15,1,20,6,@ReturnCode OUTPUT,@ErrMsg OUTPUT;
  END TRY
  BEGIN CATCH
    SET @NO_Erro = ERROR_NUMBER();
  END CATCH;

  IF (@NO_Erro <> 50001 OR @@TRANCOUNT <> 0)
  BEGIN
    THROW 51907, 'Validação de usuário/configuração inválida.', 1;
  END;

  SET @NO_Caso += 1;
END;

-- Transação externa bem-sucedida permanece aberta e pode desfazer ADD e UPD.
BEGIN TRAN;

EXEC dbo.st_ConfiguracaoAdd @ID_SemConfiguracao,15,6,15,1,20,6,@ReturnCode OUTPUT,@ErrMsg OUTPUT;
EXEC dbo.st_ConfiguracaoUpd @ID_Usuario,99,6,15,1,20,6,@ReturnCode OUTPUT,@ErrMsg OUTPUT;

IF (@@TRANCOUNT <> 1 OR XACT_STATE() <> 1)
BEGIN
  THROW 51908, 'Procedure encerrou a transação externa.', 1;
END;

ROLLBACK TRAN;

IF EXISTS(SELECT 1 FROM dbo.fn_DadosConfiguracao(@ID_SemConfiguracao))
  OR EXISTS(SELECT 1 FROM dbo.fn_DadosConfiguracao(@ID_Usuario) WHERE PE_PL_Maximo <> 16)
BEGIN
  THROW 51909, 'Transação externa não desfez as gravações.', 1;
END;

BEGIN TRAN;

BEGIN TRY
  EXEC dbo.st_ConfiguracaoAdd @ID_Usuario,15,6,15,1,20,6,@ReturnCode OUTPUT,@ErrMsg OUTPUT;
END TRY
BEGIN CATCH
  IF (@@TRANCOUNT <> 1 OR XACT_STATE() <> -1 OR ERROR_NUMBER() <> 50001)
  BEGIN
    THROW 51910, 'CATCH não preservou a propriedade da transação externa.', 1;
  END;
END CATCH;

ROLLBACK TRAN;

-- Escritas diretas também respeitam UNIQUE, FK e CHECK, independentemente da API.
SET @NO_Caso = 0;

WHILE (@NO_Caso < 3)
BEGIN
  SET @NO_Erro = 0;

  BEGIN TRY
    IF (@NO_Caso = 0)
      INSERT INTO dbo.ILCFG001 (ID_Usuario,PE_PL_Maximo,PE_DY_Minimo,PE_ROE_Minimo,PE_DividaPatrimonio_Maximo,PE_MargemLiquida_Minimo,PE_RetornoPreco)
      SELECT @ID_Usuario,15,6,15,1,20,6;
    IF (@NO_Caso = 1)
      INSERT INTO dbo.ILCFG001 (ID_Usuario,PE_PL_Maximo,PE_DY_Minimo,PE_ROE_Minimo,PE_DividaPatrimonio_Maximo,PE_MargemLiquida_Minimo,PE_RetornoPreco)
      SELECT 2147483647,15,6,15,1,20,6;
    IF (@NO_Caso = 2)
      UPDATE dbo.ILCFG001 SET PE_RetornoPreco = 0 WHERE ID_Usuario = @ID_Usuario;
  END TRY
  BEGIN CATCH
    SET @NO_Erro = ERROR_NUMBER();
  END CATCH;

  IF (@NO_Caso = 0 AND @NO_Erro NOT IN (2601,2627)) OR (@NO_Caso > 0 AND @NO_Erro <> 547)
  BEGIN
    THROW 51911, 'Constraint não protegeu escrita direta.', 1;
  END;

  SET @NO_Caso += 1;
END;

IF (SELECT COUNT(*) FROM dbo.ILCAD000 WHERE NM_Fisico = 'ILCFG001') <> 1
BEGIN
  THROW 51912, 'Registro do catálogo ausente ou duplicado.', 1;
END;

PRINT 'Configuração: inclusão, consulta, alteração, validações, constraints e transações aprovadas.';
