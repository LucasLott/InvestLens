CREATE OR ALTER FUNCTION dbo.fn_ValidaCpf(@CPF Varchar(11))
RETURNS CHAR(1)
AS
BEGIN
  DECLARE @Indice         Tinyint = 1
         ,@Soma           Integer = 0
         ,@Resto          Tinyint
         ,@DigitoEsperado Tinyint;

  -- CPF possui exatamente 11 dígitos e não admite sequências repetidas.
  IF (@CPF IS NULL                                         OR
      LEN(@CPF) <> 11                                      OR
      @CPF COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%' OR
      @CPF = REPLICATE(LEFT(@CPF, 1), 11))
  BEGIN
    RETURN 'N';
  END

  -- Primeiro dígito verificador: pesos 10 a 2 sobre os primeiros nove dígitos.
  WHILE (@Indice <= 9)
  BEGIN
    SELECT @Soma += CONVERT(Tinyint, SUBSTRING(@CPF, @Indice, 1)) * (11 - @Indice)
          ,@Indice += 1;
  END

  SET @Resto = @Soma % 11;
  SET @DigitoEsperado = CASE WHEN @Resto < 2 THEN 0 ELSE 11 - @Resto END;

  IF (CONVERT(Tinyint, SUBSTRING(@CPF, 10, 1)) <> @DigitoEsperado)
  BEGIN
    RETURN 'N';
  END

  -- Segundo dígito verificador: pesos 11 a 2 sobre os primeiros dez dígitos.
  SET @Indice = 1;
  SET @Soma = 0;

  WHILE @Indice <= 10
  BEGIN
    SELECT @Soma += CONVERT(Tinyint, SUBSTRING(@CPF, @Indice, 1)) * (12 - @Indice)
          ,@Indice += 1;
  END

  SET @Resto = @Soma % 11;

  SET @DigitoEsperado = CASE WHEN @Resto < 2 THEN 0 ELSE 11 - @Resto END

  RETURN CASE WHEN CONVERT(Tinyint, SUBSTRING(@CPF, 11, 1)) = @DigitoEsperado
              THEN 'S'
              ELSE 'N'
         END
END
