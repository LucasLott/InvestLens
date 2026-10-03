CREATE OR ALTER FUNCTION dbo.fn_DadosConfiguracao(@ID_Usuario Integer)
RETURNS TABLE
AS
RETURN (SELECT ID_Configuracao
              ,ID_Usuario
              ,PE_PL_Maximo
              ,PE_DY_Minimo
              ,PE_ROE_Minimo
              ,PE_DividaPatrimonio_Maximo
              ,PE_MargemLiquida_Minimo
              ,PE_RetornoPreco
              ,DH_Inclusao
              ,DH_Alteracao
        FROM dbo.ILCFG001
        WHERE ID_Usuario = @ID_Usuario);
