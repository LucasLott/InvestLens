/*
  InvestLens: remoção da estrutura de tabelas para recriação local.
  Executar antes de 001_CreateTables.sql quando uma mudança estrutural exigir
  reconstruir o banco de desenvolvimento.

  Este script remove somente tabelas. Functions, procedures, sequences e
  demais objetos permanecem preservados.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- Remover primeiro as tabelas dependentes para respeitar as chaves estrangeiras.
DROP TABLE IF EXISTS dbo.ILAUT002;
DROP TABLE IF EXISTS dbo.ILAUT001;
DROP TABLE IF EXISTS dbo.ILCFG001;
DROP TABLE IF EXISTS dbo.ILCAD001;
DROP TABLE IF EXISTS dbo.ILCAD000;
GO
