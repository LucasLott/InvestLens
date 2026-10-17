# Funcionalidades por plano

`ILCAD006` possui ID identity, chave técnica única `NM_Funcionalidade` e `SN_Pro`
com valores `N` (Free e Pro) ou `S` (Pro). A carga inicial registra 15 itens,
sem alterar registros existentes. Free: `BUSCA_ATIVOS`, `CONSULTA_ATIVOS`,
`COTACAO_ATUAL`, `DADOS_BASICOS_EMPRESA`, `INDICADORES_BASICOS` e
`CONFIGURACAO_CRITERIOS`. Pro: `INDICADORES_AVANCADOS`, `FILTROS_AVANCADOS`,
`HISTORICO_DIVIDENDOS`, `HISTORICO_PRECOS`, `DADOS_FINANCEIROS`,
`DEMONSTRACOES_FINANCEIRAS`, `PRECO_MAXIMO`, `ANALISE_FUNDAMENTALISTA` e
`COMPARACAO_ATIVOS`.

O catálogo inclui recursos planejados. Apenas `CONFIGURACAO_CRITERIOS` possui
endpoint de usuário operacional; não há endpoints financeiros. O frontend mantém
um mapeamento central de descrições e status de implementação e sinaliza os demais
como em desenvolvimento. Os ícones indicam inclusão prevista nos planos, e não
disponibilidade técnica. Essa sinalização não participa da autorização.

`GET /api/v1/funcionalidades` apresenta o catálogo público; `GET
/api/v1/minhas-funcionalidades` exige JWT e calcula a disponibilidade a partir
do plano efetivo, sem usar a claim de plano. A policy da configuração exige a
funcionalidade e também a policy SameUser. Sem configuração ou valor válido, o
acesso é negado. A página de planos compara o catálogo, a contratação apresenta
os benefícios do Pro e Minha Assinatura exibe somente funcionalidades efetivas.

O serviço de assinatura verifica estado, vigência e pagamento confirmado da mesma
assinatura, usuário e plano, com data na vigência e não futura. O mesmo plano
efetivo alimenta Minha Assinatura e a autorização. A verificação é conservadora:
pagamentos anteriores ao início da vigência não são aceitos, e a associação de
pagamentos a períodos de renovação depende do processamento financeiro futuro.
Não existe ativação de Pro nesta etapa.

O script de estrutura foi executado duas vezes em um banco SQL Server LocalDB
isolado: foram verificadas a carga de 15 itens, a inscrição no catálogo, as
restrições e a preservação de uma alteração manual após a reexecução. Para
executá-lo com `sqlcmd`, habilite `QUOTED_IDENTIFIER` com a opção `-I` e use
`-b` para interromper a execução em caso de erro. A concorrência da carga não
foi validada. A disponibilidade do banco é obrigatória: não há matriz de
permissões alternativa em memória. Os testes automatizados da interface cobrem
a comparação, a indicação de recursos planejados, os filtros por plano e o
catálogo vazio. Os testes HTTP de configuração isolam a policy de funcionalidade;
o teste do endpoint de funcionalidades verifica a negativa da policy.
