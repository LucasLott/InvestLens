CREATE OR ALTER PROCEDURE dbo.st_RegistrarSolicitacao @ID_Usuario       Integer
                                                     ,@CD_Tipo          Char(3)
                                                     ,@ID_Plano_Destino Integer = NULL
                                                     ,@ID_Solicitacao   Integer      OUTPUT
                                                     ,@ReturnCode       Smallint     OUTPUT
                                                     ,@ErrMsg           Varchar(255) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  SELECT @ReturnCode     = 0
        ,@ErrMsg         = NULL
        ,@ID_Solicitacao = NULL;

  DECLARE @NO_Tran          Integer = @@TRANCOUNT
         ,@ID_Assinatura    Integer
         ,@ID_Plano_Origem  Integer
         ,@Existente        Integer
         ,@TipoExistente    Char(3)
         ,@DestinoExistente Integer;

  BEGIN TRY
    IF @NO_Tran = 0 BEGIN TRAN;

    -- O bloqueio do usuário serializa todas as solicitações desse usuário.
    IF NOT EXISTS (SELECT 1 FROM dbo.ILCAD001 WITH (UPDLOCK, HOLDLOCK) WHERE ID_Usuario = @ID_Usuario AND FL_Ativo = 1)
    BEGIN
      SELECT @ReturnCode = 1
            ,@ErrMsg     = 'Usuário indisponível.';

      THROW 50001, @ErrMsg, 1;
    END

    SELECT @ID_Assinatura   = ID_Assinatura
          ,@ID_Plano_Origem = ID_Plano
    FROM dbo.ILCAD003 WITH (UPDLOCK, HOLDLOCK)
    WHERE ID_Usuario = @ID_Usuario AND CD_Status = 'ATIVA'
      AND DH_Inicio_Assinatura <= SYSUTCDATETIME()
      AND DH_Fim_Assinatura > SYSUTCDATETIME()
      AND ID_Plano = (SELECT ID_Plano FROM dbo.ILCAD002 WHERE NM_Plano = 'Pro');

    IF @ID_Plano_Origem IS NULL
      SELECT @ID_Plano_Origem = ID_Plano
      FROM dbo.ILCAD002
      WHERE NM_Plano = 'Free';

    SELECT @Existente        = ID_Solicitacao
          ,@TipoExistente    = CD_Tipo
          ,@DestinoExistente = ID_Plano_Destino
    FROM dbo.ILCAD005 WITH (UPDLOCK, HOLDLOCK)
    WHERE ID_Usuario = @ID_Usuario
      AND CD_Status = 'PEN';

    IF @Existente IS NOT NULL
    BEGIN
      IF @TipoExistente = @CD_Tipo AND (@CD_Tipo = 'CAN' OR @DestinoExistente = @ID_Plano_Destino)
      BEGIN
        SET @ID_Solicitacao = @Existente;
        IF @NO_Tran = 0 COMMIT TRAN;
        RETURN;
      END
      SELECT @ReturnCode = 1
            ,@ErrMsg     = 'Existe solicitação pendente conflitante.';

      THROW 50001, @ErrMsg, 2;
    END

    IF @ID_Plano_Origem IS NULL OR @CD_Tipo NOT IN ('ALT', 'CAN')
      OR (@CD_Tipo = 'CAN' AND (@ID_Assinatura IS NULL OR @ID_Plano_Destino IS NOT NULL))
      OR (@CD_Tipo = 'ALT' AND (@ID_Plano_Destino IS NULL OR @ID_Plano_Destino = @ID_Plano_Origem
        OR NOT EXISTS (SELECT 1 FROM dbo.ILCAD002 WITH (UPDLOCK, HOLDLOCK) WHERE ID_Plano = @ID_Plano_Destino AND FL_Ativo = 1)))
    BEGIN
      SELECT @ReturnCode = 1
            ,@ErrMsg     = 'Solicitação inválida ou indisponível.';

      THROW 50001, @ErrMsg, 1;
    END;

    INSERT dbo.ILCAD005 (ID_Usuario
                        ,ID_Assinatura
                        ,ID_Plano_Origem
                        ,ID_Plano_Destino
                        ,CD_Tipo
                        ,CD_Status)
    VALUES (@ID_Usuario
           ,@ID_Assinatura
           ,@ID_Plano_Origem
           ,@ID_Plano_Destino
           ,@CD_Tipo
           ,'PEN');

    SET @ID_Solicitacao = CONVERT(INT, SCOPE_IDENTITY());

    IF @NO_Tran = 0 COMMIT TRAN;
  END TRY
  BEGIN CATCH
    IF @NO_Tran = 0 AND XACT_STATE() <> 0 ROLLBACK TRAN;

    IF @ReturnCode <> 1 SELECT @ReturnCode = 2, @ErrMsg = LEFT(ERROR_MESSAGE(), 255);
    THROW;
  END CATCH
END
