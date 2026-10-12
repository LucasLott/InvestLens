# Planos no frontend

A rota pública `/planos` apresenta Free (R$ 0,00) e Pro (R$ 49,90/mês) como
referência comercial. Como não há endpoint público de planos, os dados são uma matriz
de apresentação centralizada em `PlanosPage`, não uma decisão de autorização.

Free apresenta busca, detalhes, cotação e indicadores atuais. Pro apresenta somente
recursos documentados como avançados: históricos, screener/filtros e preço máximo.
O botão Pro é informativo e não envia plano, não altera banco e não ativa acesso.

O avatar não recebe badge Pro nesta etapa: `IdPlano` não comprova assinatura efetiva.
A Etapa 9 fornecerá esse estado; a Etapa 10 substituirá a matriz visual por catálogo
e permissões persistidas. O frontend deve tratar `403 FEATURE_NOT_AVAILABLE` com uma
mensagem premium, mas a autorização definitiva continua no backend.
