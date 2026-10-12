# Planos de acesso

`ILCAD002` é o cadastro de planos. Seus registros iniciais idempotentes são
`Free` (R$ 0,00) e `Pro` (R$ 49,90), ambos ativos. O nome possui unicidade e é a
identificação estável usada pelas migrações; nenhum código depende de um ID fixo.

`ILCAD001.ID_Plano` é obrigatório e possui a FK `FK_ILCAD001_ILCAD002`. A evolução
cria a coluna anulável, cadastra/localiza o plano Free, associa usuários existentes,
valida a ausência de nulos e só então aplica `NOT NULL` e a FK. O cadastro público
(`st_UsuarioAdd`) localiza internamente o plano Free ativo; o request não recebe
`ID_Plano` e não pode selecionar Pro.

O login retorna `ID_Plano` por `fn_DadosLogin` e o JWT acrescenta a claim `IdPlano`.
Na rotação do refresh token, `st_RefreshToken` relê o usuário e seu plano corrente
antes de gerar o novo access token, portanto uma mudança de plano aparece no próximo
refresh. A claim é adequada para interface, mas futura autorização premium deve
consultar o estado atual do plano no backend, pois um access token existente pode
estar desatualizado.

As credenciais `Bolsai:ApiKey` e `BolsaiPro:ApiKey` permanecem fora do banco, JWT e
documentação de valores. A escolha da credencial e qualquer pagamento, checkout,
ativação pública de Pro ou endpoint premium permanecem fora deste escopo.
