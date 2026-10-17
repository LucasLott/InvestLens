# Mercado Pago: diagnóstico para a Etapa 12

## Situação

A integração financeira ainda não foi conectada ou habilitada. O responsável
informou que a conta e a aplicação estão habilitadas para a API de Assinaturas em
teste. Essa informação ainda não foi comprovada por homologação funcional.

## Documentação oficial consultada

- A [API de Assinaturas](https://www.mercadopago.com.br/developers/pt/reference/online-payments/subscriptions/overview)
  documenta `/preapproval`, planos, faturas e consulta de pagamentos como recursos
  distintos. Uma assinatura criada não comprova uma cobrança paga.
- A [gestão de assinaturas](https://www.mercadopago.com.br/developers/pt/docs/subscriptions/subscription-management)
  documenta alteração de valor e cancelamento pelo provedor. A disponibilidade
  efetiva dessas operações na conta e no ambiente de teste ainda precisa ser verificada.
- O [Pix no Checkout Transparente](https://www.mercadopago.com.br/developers/pt/docs/checkout-api-orders/payment-integration/websites/pix)
  documenta pagamento Pix avulso. Isso não comprova suporte a Pix recorrente na
  API de Assinaturas nem habilitação dessa modalidade para esta conta.
- Os [webhooks](https://www.mercadopago.com.br/developers/pt/docs/subscriptions/additional-content/your-integrations/notifications/webhooks)
  documentam assinatura de notificações e URL de teste. O texto consultado contém
  orientações que devem ser verificadas para o produto de assinaturas antes de
  implementar a configuração e homologar a entrega.
- As [credenciais de teste](https://www.mercadopago.com.br/developers/pt/docs/checkout-api-orders/create-application)
  podem ter o mesmo prefixo textual das de produção. O prefixo não é prova de ambiente.
- Uma [orientação oficial de 2023 sobre testes de assinaturas](https://www.mercadopago.com.br/developers/pt/news/2023/11/16/Questions-on-how-to-test-your-integration--)
  descreve uso de credenciais de produção de uma conta vendedora de teste. O pedido
  atual proíbe credenciais de produção. Não foi validado se credenciais de teste
  são suficientes para o fluxo de assinatura escolhido; nenhuma credencial foi usada.

## Dependências internas

A Etapa 10 não possui endpoint funcional de funcionalidades. A procedure de
solicitações da Etapa 11 ainda precisa de validação em SQL Server isolado,
incluindo concorrência, e replica a regra de vigência da Etapa 9. A suíte de
integração apresentou falhas, incluindo testes em rotas anteriores a `/api/v1`.
Essas pendências precisam ser consideradas antes da ativação financeira.

## Próximas verificações

Confirmar por homologação quais fluxos estão habilitados para a conta e aplicação
de teste. Não solicitar nem registrar credenciais nesta documentação.
Definir periodicidade real dos planos e política para reversões e troca de plano;
validar o fluxo escolhido e seus webhooks em teste, sem ativar produção.

O responsável confirmou periodicidade mensal para o Pro. A migração adiciona
`QT_Meses_Periodicidade` e preenche `1` para o Pro somente quando a coluna é
criada; demais planos permanecem sem periodicidade definida. A migração ainda
precisa ser aplicada e validada em banco isolado.

## Componentes preparados

`ConsultaPagamentoMercadoPago` consulta um pagamento pelo endpoint oficial e
`ClassificadorPagamentoMercadoPago` distingue estados e detalhes, inclusive
reembolso parcial e contestação. Eles não são registrados na aplicação, não
executam chamadas por conta própria e não ativam acesso. Uma resposta aprovada
precisa ainda passar por validações de ambiente, propriedade, valor, moeda,
correlação e idempotência antes de ser utilizada financeiramente. A política
de reembolso e o reconhecimento confiável de credenciais de teste ainda não
estão implementados.
