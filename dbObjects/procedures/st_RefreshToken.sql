CREATE OR ALTER PROCEDURE dbo.st_RefreshToken @DS_Operacao        Varchar(10)
                                             ,@TX_Hash            Char(64)
                                             ,@ID_Usuario         Integer      = NULL
                                             ,@DH_Expiracao       Datetime2(7) = NULL
                                             ,@TX_HashSubstituto  Char(64)     = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  DECLARE @NO_Tran       Integer = @@TRANCOUNT
         ,@ID_Sessao     Uniqueidentifier
         ,@DH_Revogacao  Datetime2(7)
         ,@DH_Consumo    Datetime2(7);

  DECLARE @TB_Resultado TABLE (ID_Usuario   Integer
                              ,CD_Usuario   Varchar(3)
                              ,NM_Usuario   Varchar(80)
                              ,DS_Email     Varchar(255)
                              ,DH_Expiracao Datetime2(7));

  IF (@DS_Operacao NOT IN ('criar', 'rotacionar', 'revogar') OR @DS_Operacao IS NULL)
  BEGIN
    THROW 51010, 'Operação de refresh inválida.', 1;
  END

  BEGIN TRY
    IF (@NO_Tran = 0) BEGIN TRAN;

    IF (@DS_Operacao = 'criar')
    BEGIN
      IF (@DH_Expiracao IS NULL OR @DH_Expiracao <= SYSUTCDATETIME())
      BEGIN
        THROW 51011, 'Expiração da sessão inválida.', 1;
      END

      IF NOT EXISTS(SELECT 1
                    FROM dbo.ILCAD001 WITH (HOLDLOCK)
                    WHERE ID_Usuario = @ID_Usuario
                      AND FL_Ativo = 1)
      BEGIN
        THROW 51012, 'Usuário inativo.', 1;
      END

      SET @ID_Sessao = NEWID();

      INSERT INTO dbo.ILAUT001 (ID_Sessao
                               ,ID_Usuario
                               ,DH_Expiracao)
      SELECT @ID_Sessao
            ,@ID_Usuario
            ,@DH_Expiracao;

      INSERT INTO dbo.ILAUT002 (TX_Hash
                               ,ID_Sessao)
      SELECT @TX_Hash
            ,@ID_Sessao;

      IF (@NO_Tran = 0 AND @@TRANCOUNT > 0) COMMIT TRAN;

      RETURN;
    END

    SELECT @ID_Sessao = ID_Sessao
    FROM dbo.ILAUT002
    WHERE TX_Hash = @TX_Hash;

    IF (@ID_Sessao IS NULL)
    BEGIN
      IF (@NO_Tran = 0 AND @@TRANCOUNT > 0) COMMIT TRAN;

      RETURN;
    END

    -- Bloquear a sessão antes dos tokens para serializar rotação e revogação entre instâncias da API.
    SELECT @ID_Usuario   = ID_Usuario
          ,@DH_Expiracao = DH_Expiracao
          ,@DH_Revogacao = DH_Revogacao
    FROM dbo.ILAUT001 WITH (UPDLOCK, HOLDLOCK)
    WHERE ID_Sessao = @ID_Sessao;

    SELECT @DH_Consumo = DH_Consumo
    FROM dbo.ILAUT002
    WHERE TX_Hash = @TX_Hash;

    IF (@DS_Operacao  = 'revogar'         OR
        @DH_Consumo   IS NOT NULL         OR 
        @DH_Revogacao IS NOT NULL         OR
        @DH_Expiracao <= SYSUTCDATETIME() OR 
        NOT EXISTS(SELECT 1
                   FROM dbo.ILCAD001 WITH (HOLDLOCK)
                   WHERE ID_Usuario = @ID_Usuario
                     AND FL_Ativo = 1))
    BEGIN
      UPDATE dbo.ILAUT001
      SET DH_Revogacao = COALESCE(DH_Revogacao, SYSUTCDATETIME())
      WHERE ID_Sessao = @ID_Sessao;

      -- Confirmar a revogação quando a transação pertence à procedure.
      -- Em transação externa, o chamador deve confirmar a revogação antes de retornar a falha de autenticação.
      IF (@NO_Tran = 0 AND @@TRANCOUNT > 0) COMMIT TRAN;

      RETURN;
    END

    IF (@TX_HashSubstituto IS NULL OR @TX_HashSubstituto = @TX_Hash)
    BEGIN
      THROW 51013, 'Hash substituto inválido.', 1;
    END

    UPDATE dbo.ILAUT002
    SET DH_Consumo = SYSUTCDATETIME()
    WHERE TX_Hash = @TX_Hash;

    INSERT INTO dbo.ILAUT002 (TX_Hash
                             ,ID_Sessao)
    SELECT @TX_HashSubstituto
          ,@ID_Sessao;

    INSERT INTO @TB_Resultado (ID_Usuario
                              ,CD_Usuario
                              ,NM_Usuario
                              ,DS_Email
                              ,DH_Expiracao)
    SELECT ID_Usuario
          ,CD_Usuario
          ,NM_Usuario
          ,DS_Email
          ,@DH_Expiracao
    FROM dbo.ILCAD001
    WHERE ID_Usuario = @ID_Usuario;

    IF (@NO_Tran = 0 AND @@TRANCOUNT > 0) COMMIT TRAN;

    SELECT ID_Usuario   AS IdUsuario
          ,CD_Usuario   AS Codigo
          ,NM_Usuario   AS Nome
          ,DS_Email     AS Email
          ,DH_Expiracao AS Expiracao
    FROM @TB_Resultado;
  END TRY
  BEGIN CATCH
    IF (@NO_Tran = 0 AND XACT_STATE() <> 0) ROLLBACK TRAN;

    THROW;
  END CATCH

  RETURN;
END
