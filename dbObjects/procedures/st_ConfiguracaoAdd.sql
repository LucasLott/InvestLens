CREATE OR ALTER PROCEDURE dbo.st_ConfiguracaoAdd @ID_Usuario                 Integer
                                                ,@PE_PL_Maximo               Decimal(8,4)
                                                ,@PE_DY_Minimo               Decimal(8,4)
                                                ,@PE_ROE_Minimo              Decimal(8,4)
                                                ,@PE_DividaPatrimonio_Maximo Decimal(8,4)
                                                ,@PE_MargemLiquida_Minimo    Decimal(8,4)
                                                ,@PE_RetornoPreco            Decimal(8,4)
                                                ,@ReturnCode                 Smallint     OUTPUT
                                                ,@ErrMsg                     Varchar(255) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  DECLARE @NO_Tran Integer = @@TRANCOUNT;

  BEGIN TRY
    SELECT @ReturnCode = 0
          ,@ErrMsg     = '';

    IF (@ID_Usuario IS NULL OR @ID_Usuario <= 0) SET @ErrMsg += CHAR(13) + 'ID_Usuario';
    IF (@PE_PL_Maximo IS NULL OR @PE_PL_Maximo < 0) SET @ErrMsg += CHAR(13) + 'PE_PL_Maximo';
    IF (@PE_DY_Minimo IS NULL OR @PE_DY_Minimo < 0) SET @ErrMsg += CHAR(13) + 'PE_DY_Minimo';
    IF (@PE_ROE_Minimo IS NULL OR @PE_ROE_Minimo < 0) SET @ErrMsg += CHAR(13) + 'PE_ROE_Minimo';
    IF (@PE_DividaPatrimonio_Maximo IS NULL OR @PE_DividaPatrimonio_Maximo < 0) SET @ErrMsg += CHAR(13) + 'PE_DividaPatrimonio_Maximo';
    IF (@PE_MargemLiquida_Minimo IS NULL OR @PE_MargemLiquida_Minimo < 0) SET @ErrMsg += CHAR(13) + 'PE_MargemLiquida_Minimo';
    IF (@PE_RetornoPreco IS NULL OR @PE_RetornoPreco <= 0) SET @ErrMsg += CHAR(13) + 'PE_RetornoPreco';

    IF (@ErrMsg <> '')
    BEGIN
      SELECT @ReturnCode = 1
            ,@ErrMsg     = 'Parâmetro(s) inválido(s):' + @ErrMsg;

      THROW 50001, @ErrMsg, 1;
    END

    IF (@NO_Tran = 0) BEGIN TRAN;

    IF NOT EXISTS(SELECT 1
                  FROM dbo.ILCAD001 WITH (HOLDLOCK)
                  WHERE ID_Usuario = @ID_Usuario)
    BEGIN
      SELECT @ReturnCode = 1
            ,@ErrMsg     = 'Usuário não encontrado.';

      THROW 50001, @ErrMsg, 2;
    END

    -- Serializar a consulta e a inclusão; a UNIQUE permanece como garantia definitiva.
    IF EXISTS(SELECT 1
              FROM dbo.ILCFG001 WITH (UPDLOCK, HOLDLOCK)
              WHERE ID_Usuario = @ID_Usuario)
    BEGIN
      SELECT @ReturnCode = 1
            ,@ErrMsg     = 'Usuário já possui configuração.';

      THROW 50001, @ErrMsg, 3;
    END

    INSERT INTO dbo.ILCFG001 (ID_Usuario
                             ,PE_PL_Maximo
                             ,PE_DY_Minimo
                             ,PE_ROE_Minimo
                             ,PE_DividaPatrimonio_Maximo
                             ,PE_MargemLiquida_Minimo
                             ,PE_RetornoPreco
                             ,DH_Inclusao
                             ,DH_Alteracao)
    SELECT @ID_Usuario
          ,@PE_PL_Maximo
          ,@PE_DY_Minimo
          ,@PE_ROE_Minimo
          ,@PE_DividaPatrimonio_Maximo
          ,@PE_MargemLiquida_Minimo
          ,@PE_RetornoPreco
          ,SYSDATETIME()
          ,NULL;

    IF (@NO_Tran = 0 AND @@TRANCOUNT > 0) COMMIT TRAN;

    SELECT @ReturnCode = 0
          ,@ErrMsg     = NULL;
  END TRY
  BEGIN CATCH
    IF (@NO_Tran = 0 AND XACT_STATE() <> 0) ROLLBACK TRAN;

    SELECT @ErrMsg     = CASE WHEN @ReturnCode = 1
                           THEN @ErrMsg
                           ELSE LEFT(ERROR_MESSAGE(), 255)
                         END
          ,@ReturnCode = CASE WHEN @ReturnCode = 1
                           THEN 1
                           ELSE 2
                         END;

    THROW;
  END CATCH

  RETURN;
END
