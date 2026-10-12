# Atualização financeira automática

`FinancialUpdateWorker` é um `BackgroundService` hospedado na API e fica
desabilitado por padrão. Ele cria um escopo por execução e reutiliza
`IFinancialUpdateService`, que seleciona ativos cadastrados no SQL Server em ordem
determinística de menor atualização e chama a importação da Etapa 4 sequencialmente.

Configure `FinancialUpdate:Enabled`, `RunOnStartup`, `IntervalMinutes`, `BatchSize`,
`MaxRequestsPerExecution` e `MinimumIntervalBetweenRequestsSeconds` em configuração
segura. O orçamento é preventivo e estimado em até três chamadas por ativo (empresa,
cotação e fundamentos); como a cota Pro não foi confirmada, os valores não representam
limites oficiais. O worker não seleciona chave por plano de usuário e usa somente a
credencial `Bolsai` aprovada.

Falhas isoladas são registradas e não derrubam o processo. HTTP 429 chega como erro da
integração e interrompe a execução atual, preservando importações já confirmadas.
Não há endpoint público, execução automática no startup por padrão, paralelismo ou
agendamento externo. Para múltiplas instâncias, a coordenação distribuída e histórico
persistente de consumo permanecem pendências: esta primeira versão só deve ser ativada
em uma instância até que o lock SQL seja aprovado.
