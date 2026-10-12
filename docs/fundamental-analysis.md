# Motor de análise fundamentalista

O motor puro `FundamentalAnalysisEngine` aplica a configuração do usuário aos
indicadores persistidos: P/L `<`, DY `>`, ROE `>`, dívida/patrimônio `<` e margem
líquida `>`. Percentuais usam a unidade humana já persistida (6 significa 6%), sem
nova normalização. Cada critério resulta em `Approved`, `Rejected` ou
`InsufficientData`; o ativo é rejeitado se algum falhar, insuficiente se nenhum
falhar mas algum estiver ausente, e aprovado somente se todos passarem.

Preço máximo é independente dos filtros: `dividendo médio anual / (retorno / 100)`.
O banco atual não persiste proventos confiáveis; por isso o motor exige média recebida
de histórico real e cinco anos completos, retornando dados insuficientes até que tal
modelagem seja aprovada. JCP não é somado sem uma política tributária explícita.
Não há chamadas à bolsai, acesso SQL, endpoint ou recomendação de investimento.
