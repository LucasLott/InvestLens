CREATE OR ALTER PROCEDURE dbo.st_BolsaiReservarRequisicao @NM_Fonte     Varchar(80)
                                                         ,@DT_Janela    Date
                                                         ,@QT_Orcamento Integer
                                                         ,@ReturnCode   Smallint     OUTPUT
                                                         ,@ErrMsg       Varchar(255) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;
  
  SELECT @ReturnCode = 0
        ,@ErrMsg    = NULL;

  BEGIN TRAN;
  BEGIN TRY
    UPDATE dbo.ILLOG001 WITH (UPDLOCK, SERIALIZABLE)
    SET QT_Requisicao = QT_Requisicao + 1
       ,DH_Alteracao  = SYSDATETIME()
    WHERE NM_Fonte  = @NM_Fonte
      AND DT_Janela = @DT_Janela
      AND QT_Requisicao < @QT_Orcamento;

    IF @@ROWCOUNT = 0
    BEGIN
      IF NOT EXISTS (SELECT 1
                     FROM dbo.ILLOG001 WITH (UPDLOCK, SERIALIZABLE)
                     WHERE NM_Fonte  = @NM_Fonte
                       AND DT_Janela = @DT_Janela)
      BEGIN
        INSERT dbo.ILLOG001(NM_Fonte
                           ,DT_Janela
                           ,QT_Requisicao)
        VALUES(@NM_Fonte
              ,@DT_Janela
              ,1);
      END
      ELSE
      BEGIN
        SELECT @ReturnCode = 1
              ,@ErrMsg     = 'Orçamento diário da bolsai esgotado.';
        
        THROW 50001,@ErrMsg,1;
      END
    END
    COMMIT;
  END TRY
  BEGIN CATCH 
    IF XACT_STATE() <> 0 ROLLBACK;
    
    THROW;
  END CATCH
END
