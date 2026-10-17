# Planos no frontend

## Navegação

A tela inicial oferece o link **Conheça nossos planos**, que navega para a rota pública
`/planos`. A página consulta os planos ativos pela API pública; não há catálogo fixo como
fallback e planos que não carregaram não podem ser selecionados.

Ao selecionar um plano, visitantes seguem para `/cadastro` por meio do estado de navegação
do React Router. São enviados somente `idPlano` e `nome` como intenção de interesse, sem
armazenamento em `localStorage` e sem qualquer autorização ou ativação.

## Cadastro

O cadastro valida a intenção recebida consultando os planos públicos ativos. Apenas um plano
que corresponda ao identificador e nome retornados pela API é mostrado visualmente. Acesso
direto a `/cadastro`, estado inválido ou falha nessa consulta mantém o fluxo normal, sem erro
e sem plano selecionado.

O request de cadastro não recebe `ID_Plano`. O backend continua criando usuários no plano
Free. Quando o interesse é no Pro, a tela informa que a contratação estará disponível em
breve e que a conta será criada no Free.

## Usuários autenticados e limitações

Para usuários autenticados, selecionar um plano não redireciona para novo cadastro, não
altera o plano da conta e não ativa assinatura. Um plano pago segue para a página de
intenção de contratação; a conclusão financeira permanece indisponível.

A comparação consulta o catálogo público de funcionalidades. A página de contratação
mostra o catálogo do Pro, enquanto Minha Assinatura consulta as permissões efetivas
do usuário autenticado. Falhas de consulta não usam uma lista fixa como alternativa.
