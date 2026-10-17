# Gestão de planos

Planos ativos são obtidos pela API pública. A escolha é uma intenção no estado de navegação, revalidada pela API antes e depois do cadastro. O cadastro não recebe plano: a procedure existente cria a conta Free. Após autenticação, uma escolha paga válida leva a `/finalizar-contratacao`; a seleção não ativa uma assinatura. O endpoint existente `GET /api/v1/minha-assinatura` consulta o plano efetivo.

`ILCAD005` registra solicitações com usuário, assinatura opcional, plano de origem, destino, tipo `ALT` ou `CAN`, status `PEN`, `CON` ou `REJ` e datas. Uma restrição única filtrada impede múltiplas solicitações pendentes do mesmo usuário. A procedure `st_RegistrarSolicitacao` bloqueia o usuário durante a validação e gravação; um reenvio idêntico devolve a solicitação existente, e pedidos conflitantes retornam conflito. Ela não altera assinatura ou pagamento. A estrutura preserva registros anteriores e não armazena credenciais ou dados de pagamento.

Os endpoints autenticados são `GET /api/v1/minha-assinatura/solicitacoes`, `POST /api/v1/minha-assinatura/alteracoes` (body `{ "idPlanoDestino": 1 }`) e `POST /api/v1/minha-assinatura/cancelamento`. O usuário vem do JWT validado. As respostas descrevem a solicitação, não a efetivação da operação. A interface consulta os pedidos persistidos e exige confirmação antes de registrar outro pedido.

Não há endpoint funcional da Etapa 10 para obter funcionalidades; a interface informa a indisponibilidade e não inventa benefícios. Não há integração com Mercado Pago, checkout, webhook ou confirmação de pagamento. Uma integração futura deverá correlacionar os registros de usuário, plano, assinatura e solicitação com a operação externa e manter suas credenciais fora do código e do frontend.

Limitações: a procedure precisa ser validada em SQL Server isolado, inclusive sob concorrência. Sua determinação transacional do plano de origem replica a regra atual de vigência do serviço da Etapa 9 e precisa ser consolidada com essa regra para evitar divergências futuras. A cobertura HTTP e de frontend ainda precisa ser ampliada.
