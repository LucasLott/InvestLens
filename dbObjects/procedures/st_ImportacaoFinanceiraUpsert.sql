CREATE OR ALTER PROCEDURE dbo.st_ImportacaoFinanceiraUpsert @CD_Ticker           Varchar(20)
                                                           ,@CD_TickerPrimario   Varchar(20)   = NULL
                                                           ,@NM_Empresa          Varchar(80)   = NULL
                                                           ,@NM_RazaoSocial      Varchar(255)  = NULL
                                                           ,@DS_Setor            Varchar(255)  = NULL
                                                           ,@NM_Fonte            Varchar(80)
                                                           ,@DT_Fundamentos      Date          = NULL
                                                           ,@VL_PL               Decimal(18,8) = NULL
                                                           ,@PE_DividendYield    Decimal(18,8) = NULL
                                                           ,@PE_ROE              Decimal(18,8) = NULL
                                                           ,@VL_DividaPatrimonio Decimal(18,8) = NULL
                                                           ,@PE_MargemLiquida    Decimal(18,8) = NULL
                                                           ,@DT_Cotacao          Date          = NULL
                                                           ,@VL_PrecoFechamento  Decimal(18,8) = NULL
                                                           ,@ReturnCode          Smallint     OUTPUT
                                                           ,@ErrMsg              Varchar(255) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  DECLARE @NO_Tran    Integer = @@TRANCOUNT
         ,@ID_Empresa Integer
         ,@ID_Ativo   Integer;

  SELECT @ReturnCode        = 0
        ,@ErrMsg            = NULL
        ,@CD_Ticker         = UPPER(LTRIM(RTRIM(@CD_Ticker)))
        ,@CD_TickerPrimario = NULLIF(UPPER(LTRIM(RTRIM(@CD_TickerPrimario))), '');

  IF NULLIF(@CD_Ticker, '') IS NULL OR NULLIF(@NM_Fonte, '') IS NULL
  BEGIN
    SELECT @ReturnCode = 1
          ,@ErrMsg     = 'Ticker e fonte são obrigatórios.';
    THROW 50001, @ErrMsg, 1;
  END

  BEGIN TRY
    IF @NO_Tran = 0 BEGIN TRAN;

    -- ticker_primary é o identificador canônico fornecido pela origem; sem ele,
    -- não há deduplicação especulativa por nomes de empresas.
    IF @CD_TickerPrimario IS NOT NULL
    BEGIN
      SELECT @ID_Empresa = ID_Empresa
      FROM dbo.ILFIN001 WITH (UPDLOCK, HOLDLOCK)
      WHERE CD_TickerPrimario = @CD_TickerPrimario;
    END

    IF @ID_Empresa IS NULL AND @CD_TickerPrimario IS NULL
    BEGIN
      SELECT @ID_Empresa = ID_Empresa
      FROM dbo.ILFIN002 WITH (UPDLOCK, HOLDLOCK)
      WHERE CD_Ticker = @CD_Ticker;
    END

    IF @ID_Empresa IS NULL
    BEGIN
      INSERT dbo.ILFIN001 (NM_Empresa
                          ,NM_RazaoSocial
                          ,DS_Setor
                          ,CD_TickerPrimario
                          ,NM_Fonte)
      VALUES (@NM_Empresa
             ,@NM_RazaoSocial
             ,@DS_Setor
             ,@CD_TickerPrimario
             ,@NM_Fonte);

      SET @ID_Empresa = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
      UPDATE dbo.ILFIN001
      SET NM_Empresa = COALESCE(@NM_Empresa, NM_Empresa)
         ,NM_RazaoSocial = COALESCE(@NM_RazaoSocial, NM_RazaoSocial)
         ,DS_Setor = COALESCE(@DS_Setor, DS_Setor)
         ,DH_Alteracao = SYSDATETIME()
      WHERE ID_Empresa = @ID_Empresa;
    END

    SELECT @ID_Ativo = ID_Ativo
    FROM dbo.ILFIN002 WITH (UPDLOCK, HOLDLOCK)
    WHERE CD_Ticker = @CD_Ticker;
    
    IF @ID_Ativo IS NULL
    BEGIN
      INSERT dbo.ILFIN002 (ID_Empresa
                          ,CD_Ticker
                          ,NM_Fonte)
      VALUES (@ID_Empresa
             ,@CD_Ticker
             ,@NM_Fonte);

      SET @ID_Ativo = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
      UPDATE dbo.ILFIN002
      SET ID_Empresa   = @ID_Empresa
         ,NM_Fonte     = @NM_Fonte
         ,DH_Alteracao = SYSDATETIME()
      WHERE ID_Ativo = @ID_Ativo;
    END

    IF @DT_Fundamentos IS NOT NULL AND (@PE_ROE IS NOT NULL OR @VL_DividaPatrimonio IS NOT NULL OR @PE_MargemLiquida IS NOT NULL)
    BEGIN
      UPDATE dbo.ILFIN003
      SET DH_Coleta           = SYSDATETIME()
         ,PE_ROE              = COALESCE(@PE_ROE, PE_ROE)
         ,VL_DividaPatrimonio = COALESCE(@VL_DividaPatrimonio, VL_DividaPatrimonio)
         ,PE_MargemLiquida    = COALESCE(@PE_MargemLiquida, PE_MargemLiquida)
      WHERE ID_Empresa    = @ID_Empresa
        AND NM_Fonte      = @NM_Fonte
        AND DT_Referencia = @DT_Fundamentos;

      IF @@ROWCOUNT = 0
      BEGIN
        INSERT dbo.ILFIN003 (ID_Empresa
                            ,NM_Fonte
                            ,DT_Referencia
                            ,DH_Coleta
                            ,PE_ROE
                            ,VL_DividaPatrimonio
                            ,PE_MargemLiquida)
        VALUES (@ID_Empresa
               ,@NM_Fonte
               ,@DT_Fundamentos
               ,SYSDATETIME()
               ,@PE_ROE
               ,@VL_DividaPatrimonio
               ,@PE_MargemLiquida);
      END
    END
    IF @DT_Fundamentos IS NOT NULL AND (@VL_PL IS NOT NULL OR @PE_DividendYield IS NOT NULL)
    BEGIN
      UPDATE dbo.ILFIN004
      SET DH_Coleta        = SYSDATETIME()
         ,VL_PL            = COALESCE(@VL_PL, VL_PL)
         ,PE_DividendYield = COALESCE(@PE_DividendYield, PE_DividendYield)
      WHERE ID_Ativo      = @ID_Ativo
        AND NM_Fonte      = @NM_Fonte
        AND DT_Referencia = @DT_Fundamentos;

      IF @@ROWCOUNT = 0
      BEGIN
        INSERT dbo.ILFIN004 (ID_Ativo
                            ,NM_Fonte
                            ,DT_Referencia
                            ,DH_Coleta
                            ,VL_PL
                            ,PE_DividendYield)
        VALUES (@ID_Ativo
               ,@NM_Fonte
               ,@DT_Fundamentos
               ,SYSDATETIME()
               ,@VL_PL
               ,@PE_DividendYield);
      END
    END
    IF @DT_Cotacao IS NOT NULL AND @VL_PrecoFechamento IS NOT NULL
    BEGIN
      UPDATE dbo.ILFIN005
      SET DH_Coleta          = SYSDATETIME()
         ,VL_PrecoFechamento = @VL_PrecoFechamento
      WHERE ID_Ativo      = @ID_Ativo
        AND NM_Fonte      = @NM_Fonte
        AND DT_Referencia = @DT_Cotacao;
      
      IF @@ROWCOUNT = 0
      BEGIN 
        INSERT dbo.ILFIN005 (ID_Ativo
                            ,NM_Fonte
                            ,DT_Referencia
                            ,DH_Coleta
                            ,VL_PrecoFechamento)
        VALUES (@ID_Ativo
               ,@NM_Fonte
               ,@DT_Cotacao
               ,SYSDATETIME()
               ,@VL_PrecoFechamento);
      END
    END

    IF @NO_Tran = 0 AND @@TRANCOUNT > 0 COMMIT TRAN;

    SELECT IdEmpresa = @ID_Empresa
          ,IdAtivo   = @ID_Ativo;

    SELECT @ReturnCode = 0
          ,@ErrMsg     = NULL;
  END TRY
  BEGIN CATCH
    IF @NO_Tran = 0 AND XACT_STATE() <> 0 ROLLBACK TRAN;

    IF @ReturnCode = 0
      SELECT @ReturnCode = 2
            ,@ErrMsg     = LEFT(ERROR_MESSAGE(),255);

    THROW;
  END CATCH
END
