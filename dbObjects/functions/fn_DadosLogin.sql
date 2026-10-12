CREATE OR ALTER FUNCTION dbo.fn_DadosLogin(@DS_Email Varchar(255))
RETURNS TABLE
AS
RETURN (SELECT ID_Usuario
              ,CD_Usuario
              ,NM_Usuario
              ,DS_Email
              ,TX_Senha
              ,FL_Ativo
              ,Usuario.ID_Plano
        FROM dbo.ILCAD001 Usuario
        WHERE DS_Email = @DS_Email)
