# Revisão do módulo de Configuração

## Escopo e execução

Revisão dos endpoints GET, POST e PUT em `/api/usuarios/{idUsuario}/configuracao`,
JWT, SameUser, service, validator, repository, tratamento de erros e objetos SQL.
O código de produção e o fluxo de Refresh Token não foram alterados.

Na raiz do repositório:

```powershell
dotnet restore server/InvestLens.sln
dotnet build server/InvestLens.sln --no-restore
dotnet test server/InvestLens.sln --no-build --no-restore
```

## Cobertura

- `ConfiguracaoSecurityTests`: JWT ausente, malformado, expirado e assinado com
  outra chave nos três endpoints; 401 sem execução do service/repository.
- SameUser: outro usuário, claim ausente, duplicada ou inválida; 403 antes da
  lógica de negócio tanto para recurso existente quanto ausente, sem dados no erro.
- IDOR/BOLA e mass assignment: IDs em body, query e headers não substituem a rota.
  Propriedades JSON extras de identificadores e datas não são utilizadas na escrita.
  O teste verifica o estado do outro usuário e os IDs recebidos pelo repository.
- Validação em POST e PUT: negativos, nulos, ausentes, excesso de precisão/escala,
  texto em campo decimal e RetornoPreco zero; 400 antes de acessar a persistência.
- Decimais e percentuais: 6 permanece 6.0000 no contrato com o repository, sem divisão
  por 100; valores decimais com quatro casas são aceitos.
- GET não solicita gravação; POST duplicado retorna 409 sem fallback para UPDATE;
  PUT ausente retorna 404 sem fallback para INSERT.
- `ConfiguracaoServiceTests`: validação, cancelamento, propagação de exceções sem
  upsert, ausência, leitura sem gravação, preservação de critérios e CancellationToken.
- `ConfiguracaoEndpointTests`: testes existentes continuam passando pelo service e
  validator registrados na DI. O double passou a exigir preparação explícita dos
  dados, não cria durante GET e não permite PUT de registro inexistente.
- `ExceptionHandlingTests`: respostas e logs não expõem marcadores de Access Token,
  Refresh Token, JWT secret, senha ou hash Argon2id, inclusive em exceções 500.
- A cobertura existente de `SameUserAuthorizationTests` já inclui parsing numérico,
  faixa de INT, IDs não positivos, identidade não autenticada e claims duplicadas.
- Os testes existentes de Refresh Token verificam geração, hash enviado ao repository,
  cookie seguro, rotação, replay, revogação e proteção CSRF. Usam persistência simulada.

## Revisão de código

Não foi identificada vulnerabilidade nos caminhos de Configuração analisados.
Isso se limita ao código revisado e aos cenários testados; não representa uma auditoria
completa da aplicação ou de sua infraestrutura.

- JWT valida assinatura HS256, emissor, audiência e expiração com ClockSkew zero.
- A policy SameUser exige Bearer e compara somente a claim IdUsuario com a rota.
- ConfiguracaoRequest expõe apenas os seis critérios; não aceita IDs ou datas como
  propriedades do DTO. JSON desconhecido permanece com o comportamento padrão da API.
- ConfiguracaoRepository utiliza SQL constante e DynamicParameters para todos os
  valores externos. Não há concatenação/interpolação de input na consulta.
- `fn_DadosConfiguracao` filtra por ID_Usuario e executa somente SELECT.
- `st_ConfiguracaoUpd` contém `WHERE ID_Usuario = @ID_Usuario`; preserva PK,
  ID_Usuario e DH_Inclusao. A inclusão e a alteração são operações distintas.
- `st_ConfiguracaoAdd` usa UPDLOCK/HOLDLOCK dentro da transação. A UNIQUE de
  ID_Usuario permanece em `001_CreateTables.sql`. O mapeamento de erro considera
  número/procedure/state, incluindo 2601/2627, sem analisar mensagens do SQL.
- Os seis parâmetros SQL são Decimal(8,4), sem conversão de percentual.
- Controller/service/repository de Configuração não adicionam logs de input.
  HttpErrorLogger registra template da rota e tipo da exceção, sem body, headers,
  cookies ou mensagem da exceção. A autenticação desabilita detalhes de erro/token.

## Limites: SQL real e concorrência

Os testes HTTP usam o pipeline ASP.NET Core/JWT real, mas substituem o repository.
Nos novos testes de segurança, um contador envolve o ConfiguracaoService real para
verificar que a autorização impede sua execução. Nenhum banco é acessado.

A suíte .NET atual não possui fixture de SQL Server isolado para Configuração.
Não foram inventadas connection strings/credenciais e não foi usado banco de produção.
Concorrência entre duas chamadas HTTP com Dapper/SQL real **não foi executada nesta
revisão**. Um teste concorrente com Dictionary/lock não comprovaria a UNIQUE ou os
bloqueios do SQL Server e, por isso, não é apresentado como essa garantia.

Para completar essa cobertura, a infraestrutura futura deve provisionar um banco de
teste isolado, aplicar os scripts reais e usar ConfiguracaoRepository real. Executar
dois POSTs simultâneos para o mesmo usuário; exigir um 201, um 409 e COUNT(*) = 1.
Verificar também duas escritas SQL diretas concorrentes contra a UNIQUE e que os
dados de outro usuário permaneçam intactos. Esse ambiente deve confirmar a persistência
real de 6.0000, datas/IDs preservados e os efeitos de rollback.

O script existente `dbObjects/tests/Configuracao.sql` cobre regras/constraints e
transações em banco de teste com nome `InvestLens_Config_Test_*`, mas não é executado
automaticamente por `dotnet test` e não foi reexecutado nesta revisão.

## Pendência anterior à revisão

Há uma divergência de Swagger: os testes e `docs/api-rules.md` exigem habilitação
incondicional em DEBUG, enquanto `UseApiDocumentation` retorna quando
`EnableSwaggerProd=false`. Isso afeta dois casos de `SwaggerConfigurationTests`.
Esse código não foi alterado pela revisão de Configuração.

## Resultado da execução desta revisão

- Restore concluído; pacotes atualizados.
- Build Debug concluído com zero avisos e zero erros.
- UnitTests: 87 aprovados, zero falhas.
- IntegrationTests: 205 aprovados e duas falhas anteriores de Swagger, total 207.
- Todos os 142 novos casos passaram: 131 em ConfiguracaoSecurityTests e 11 em
  ConfiguracaoServiceTests. Também passaram os testes existentes de Configuração,
  SameUser, JWT, Refresh Token e os testes de logging ampliados.
- O comando completo de testes retorna código 1 pelas duas falhas de Swagger;
  a suíte completa não está verde. Não houve alteração em código de produção.

| Cenário | Esperado | Obtido |
| --- | --- | --- |
| GET próprio | 200 | 200 |
| GET próprio sem configuração | 404 | 404 |
| GET outro usuário | 403 | 403, service/repository não executados |
| POST próprio | 201 | 201 |
| POST duplicado | 409 | 409, sem sobrescrita |
| POST outro usuário | 403 | 403, service/repository não executados |
| PUT próprio | 200 | 200 |
| PUT inexistente | 404 | 404, sem inclusão |
| PUT outro usuário | 403 | 403, service/repository não executados |
| JWT ausente | 401 | 401 nos três endpoints |
| JWT inválido | 401 | 401 nos três endpoints |
| JWT expirado | 401 | 401 nos três endpoints |
| Validação inválida | 400 | 400 em POST/PUT, repository não executado |
| Concorrência SQL real | 201 + 409, um registro | Não executada; requer fixture SQL isolada |

Os resultados HTTP acima usam persistência simulada, conforme os limites descritos.
