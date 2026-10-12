# Acesso financeiro provisório

`FinancialFeature` centraliza a matriz transitória de funcionalidades. Pesquisa de
ativos, detalhes básicos, cotação atual e indicadores atuais são Free para qualquer
usuário autenticado. Histórico de proventos, histórico de preços/fundamentos,
screener em lote e preço máximo permanecem bloqueados por `FeatureNotAvailable`.

O bloqueio é deliberadamente conservador: `ID_Plano` e a claim `IdPlano` não
comprovam uma assinatura válida. A Etapa 9 fornecerá essa comprovação e a Etapa 10
substituirá a matriz fixa por permissões persistidas. O código de erro recomendado
para a camada HTTP é `FEATURE_NOT_AVAILABLE` com HTTP 403, sem revelar dados de
assinatura ou credenciais.

Não existe `docs/financial-api.md` nem endpoints financeiros no estado atual do
repositório; por isso esta alteração não inventa controllers ou rotas para proteger.
