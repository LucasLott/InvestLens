# Integração bolsai — Etapa 2

Verificado em 11/10/2026 na [documentação oficial](https://usebolsai.com/docs). A autenticação é `X-API-Key` e a Base URL é `https://api.usebolsai.com/api/v1/`.

## Endpoints usados

| Endpoint | Finalidade | Situação |
|---|---|---|
| `GET /companies/{ticker}` | identificação, nome e setor | confirmado por documentação |
| `GET /stocks/{ticker}/quote` | preço de fechamento e data de pregão | confirmado por documentação |
| `GET /fundamentals/{ticker}` | P/L, DY, ROE, dívida/patrimônio e margem líquida | confirmado por documentação |
| `GET /dividends/{ticker}?years=5` | dividendos/JCP e datas | confirmado por documentação; pode depender do plano |

O diagnóstico opt-in consulta individualmente PETR4, VALE3 e ITUB4, sem paralelismo e sem retry. Execute `BOLSAI_RUN_CONNECTIVITY_TESTS=true` junto ao teste `BolsaiConnectivityTests` para chamadas reais. Ele não é executado por padrão.

## Mapeamento

| bolsai | InvestLens | Tipo | unidade original / normalizada | situação |
|---|---|---|---|---|
| `pl` | `PriceToEarnings` | decimal? | múltiplo / múltiplo | documentação |
| `dividend_yield` | `DividendYield` | decimal? | percentual humano / percentual humano | documentação |
| `roe` | `ReturnOnEquity` | decimal? | percentual humano / percentual humano | documentação |
| `debt_equity` | `DebtToEquity` | decimal? | múltiplo / múltiplo | documentação |
| `net_margin` | `NetMargin` | decimal? | percentual humano / percentual humano | documentação |
| `close`, `trade_date` | `ClosePrice`, `QuoteDate` | decimal?, DateOnly? | preço monetário / sem conversão | documentação |
| `reference_date` | `FundamentalsReferenceDate` | DateOnly? | data do balanço | documentação |
| `payments[].*` | `DividendPayment` | nullable | valor por ação e datas / sem conversão | documentação |

`null` é preservado; não é convertido em zero. Valores negativos são preservados. Não foram criados DTOs financeiros persistentes.

## Plano e uso

A documentação informa plano gratuito com 200 requisições/dia, fundamentos e preços atuais; o screener requer Pro. `429` não gera retry. A página não apresenta autorização conclusiva para armazenamento, redistribuição ou uso comercial: confirmar com a bolsai antes de publicar ou persistir dados de terceiros.

## Pendências

Confirmar por respostas reais, para a chave em uso, a disponibilidade de dividendos/JCP e as unidades observadas nos três ativos. A Etapa 3 deverá decidir persistência, política de atualização e regras de filtro; nada disso foi implementado aqui.

## Importação manual

A Etapa 4 reutiliza o cliente configurado para uma importação interna manual. A
credencial padrão permanece na seção segura `Bolsai`; `BolsaiPro` é reservada para
uma futura seleção de recursos autorizados por assinatura válida, não pelo plano
declarado pelo cliente. Não há seleção de credencial via request, endpoint público
ou execução automática.
