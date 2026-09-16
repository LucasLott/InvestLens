CREATE OR ALTER PROCEDURE dbo.st_UsuarioAdd @NM_Usuario Varchar(80)
                                           ,@DS_Email   Varchar(255)
                                           ,@TX_Senha   Varchar(MAX)
                                           ,@FL_Ativo   Bit
                                           ,@ReturnCode Smallint     OUTPUT
                                           ,@ErrMsg     Varchar(255) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  DECLARE @NO_Tran      Integer = @@TRANCOUNT
         ,@CD_Usuario   Char(3)
         ,@NO_Sequencia Integer;

  BEGIN TRY
    SELECT @ReturnCode = 0
          ,@ErrMsg     = '';

    IF (ISNULL(@NM_Usuario, '') = '') SET @ErrMsg += Char(13) + 'NM_Usuario';
    IF (ISNULL(@DS_EMail  , '') = '') SET @ErrMsg += Char(13) + 'DS_EMail';
    IF (ISNULL(@TX_Senha  , '') = '') SET @ErrMsg += Char(13) + 'TX_Senha';
    IF (ISNULL(@FL_Ativo  , -1) = -1) SET @ErrMsg += Char(13) + 'FL_Ativo';

    IF (ISNULL(@ErrMsg, '') <> '')
    BEGIN
      SELECT @ReturnCode = 1
            ,@ErrMsg = 'Parâmetro(s) ' + @ErrMsg;

      THROW 50001, @ErrMsg, 1;
    END

    IF EXISTS(SELECT 1
              FROM dbo.ILCAD001
              WHERE DS_Email = @DS_Email)
    BEGIN
      SELECT @ReturnCode = 1
            ,@ErrMsg     = 'E-mail já cadastrado!';

      THROW 50001, @ErrMsg, 1;
    END

    SET @NO_Sequencia = NEXT VALUE FOR dbo.SEQ_ILCAD001_CD_Usuario;
    SET @CD_Usuario = RIGHT('000' + CAST(@NO_Sequencia AS VARCHAR(3)), 3);

    IF (@NO_Tran = 0) BEGIN TRAN;

    INSERT INTO dbo.ILCAD001 (CD_Usuario
                             ,NM_Usuario
                             ,DS_Email
                             ,TX_Senha
                             ,FL_Ativo
                             ,DH_Inclusao
                             ,DH_Alteracao)
    SELECT @CD_Usuario
          ,@NM_Usuario
          ,@DS_Email
          ,@TX_Senha
          ,@FL_Ativo
          ,SYSDATETIME()
          ,NULL;

    IF (@NO_Tran = 0 AND @@TRANCOUNT > 0) COMMIT TRAN;

    SELECT @ReturnCode = 0
          ,@ErrMsg     = NULL;
  END TRY
  BEGIN CATCH
    IF (@NO_Tran = 0 AND XACT_STATE() <> 0) ROLLBACK TRAN;

    SELECT @ErrMsg     = CASE WHEN ISNULL(@ReturnCode, 0) = 0
                           THEN ERROR_MESSAGE()
                           ELSE @ErrMsg
                         END
          ,@ReturnCode = CASE WHEN ISNULL(@ReturnCode, 0) = 0
                           THEN 2
                           ELSE @ReturnCode
                         END;

    THROW;
  END CATCH

  RETURN;
END