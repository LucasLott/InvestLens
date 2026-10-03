CREATE OR ALTER FUNCTION dbo.fn_DadosLogin(@DS_Email Varchar(255))
RETURNS TABLE
AS
RETURN (SELECT ID_Usuario
              ,CD_Usuario
              ,NM_Usuario
              ,DS_Email
              ,TX_Senha
              ,FL_Ativo
        FROM dbo.ILCAD001
        WHERE DS_Email = @DS_Email)