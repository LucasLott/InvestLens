# Importação financeira manual

`FinancialImportService` orquestra a consulta `ICompanyMarketDataProvider` e a
persistência por `IFinancialImportRepository`. Não há controller, worker, agendamento
ou execução no startup. O mecanismo manual é destinado ao diagnóstico interno e só
deve ser chamado por código administrativo autorizado.

`st_ImportacaoFinanceiraUpsert` executa a persistência em transação, com locks de
atualização para serializar importações concorrentes do mesmo ticker. Empresas são
associadas por `ticker_primary`, identificador canônico vindo da bolsai e preservado
em `ILFIN001.CD_TickerPrimario`; sem ele não há deduplicação por nome. O ticker
continua único no ativo.

Fundamentos e cotações só são persistidos se tiverem data de referência confiável.
Valores ausentes não substituem valores já armazenados. Dividendos/JCP continuam
fora do escopo porque a disponibilidade real e a chave de deduplicação não foram
confirmadas. A fonte gravada é `bolsai`; percentuais permanecem em unidade humana.

As credenciais ficam em User Secrets/configuração segura. A operação usa o cliente
configurado `Bolsai`; a seleção de `BolsaiPro` e a autorização premium efetiva serão
definidas depois da validação de vigência de assinatura. Nenhuma chave é gravada,
logada ou retornada.
