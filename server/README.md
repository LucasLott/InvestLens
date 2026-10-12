# InvestLens API

Backend do InvestLens em .NET 10, ASP.NET Core, SQL Server/Azure SQL, Dapper,
FluentValidation, NLog, JWT e refresh-token rotation.

## Estrutura

```text
src/InvestLens.Api/             HTTP, configuração e hosted services
src/InvestLens.Application/     casos de uso, contratos e validações
src/InvestLens.Domain/          regras independentes
src/InvestLens.Infrastructure/  SQL/Dapper, segurança e bolsai
tests/                          testes unitários e de integração
```

## Pré-requisitos

- .NET SDK 10
- SQL Server ou Azure SQL
- API Key da bolsai para importar dados financeiros

## Configuração local

`src/InvestLens.Api/appsettings.json` possui apenas configurações não sensíveis.
Use User Secrets para todos os segredos; nunca publique valores reais de conexão,
JWT ou API Key.

Na raiz do repositório:

```powershell
$jwtSecret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))

dotnet user-secrets set "Authentication:Jwt:Secret" $jwtSecret --project server/src/InvestLens.Api
dotnet user-secrets set "ConnectionStrings:InvestLens" "Server=SEU_SERVIDOR;Database=InvestLens;Trusted_Connection=True;TrustServerCertificate=True" --project server/src/InvestLens.Api
dotnet user-secrets set "Bolsai:ApiKey" "SUA_CHAVE_BOLSAI" --project server/src/InvestLens.Api
```

Para SQL Authentication, use uma string equivalente a:

```text
Server=SEU_SERVIDOR;Database=InvestLens;User Id=SEU_USUARIO;Password=SUA_SENHA;TrustServerCertificate=True
```

`Authentication:Jwt:Secret` deve ser Base64 de exatamente 32 bytes. `Bolsai:ApiKey`
é enviada somente à bolsai como `X-API-Key`; não deve aparecer no banco, JWT, logs,
frontend ou documentação. Confira nomes sem compartilhar a saída:

```powershell
dotnet user-secrets list --project server/src/InvestLens.Api
```

| Seção | Finalidade |
| --- | --- |
| `Authentication:Jwt:Issuer`, `Audience`, `ExpirationInMinutes` | Metadados e validade do JWT. O `Secret` é obrigatório em User Secrets. |
| `ConnectionStrings:InvestLens` | Conexão SQL; mantenha em User Secrets/Key Vault. |
| `Bolsai:BaseUrl`, `TimeoutSeconds` | URL e timeout da integração. A chave fica em User Secrets. |
| `Swagger` | Metadados e ativação no Release. |
| `Cors:AllowedOrigins` | Origens permitidas para o cliente. |
| `FinancialUpdate` | Worker interno, desabilitado por padrão. |

`FinancialUpdate` aceita `Enabled`, `RunOnStartup`, `IntervalMinutes`, `BatchSize`,
`MaxRequestsPerExecution` e `MinimumIntervalBetweenRequestsSeconds`. Mantenha
`Enabled=false` até existir autorização operacional e cota contratada confirmada.

## Banco de dados

Execute scripts com suporte a `GO` apenas em ambiente autorizado:

1. `dbObjects/tables/001_CreateTables.sql`
2. arquivos em `dbObjects/functions/`
3. arquivos em `dbObjects/procedures/`, incluindo `st_ImportacaoFinanceiraUpsert.sql`

O script de tabelas é idempotente e registra objetos em `ILCAD000`. Não execute
`002_DropTables.sql` fora de um ambiente descartável.

## Executar

```powershell
dotnet restore server/InvestLens.sln
dotnet build server/InvestLens.sln --no-restore
dotnet test server/InvestLens.sln --no-build
dotnet run --project server/src/InvestLens.Api --launch-profile http
```

- Health: `http://localhost:5124/health`
- Swagger em DEBUG: `http://localhost:5124/swagger`
- OpenAPI: `http://localhost:5124/swagger/v1/swagger.json`

Em Release, Swagger só é exposto com `Swagger:EnableSwaggerProd=true`.

## Autenticação

Login emite JWT HS256 e cookie de refresh HttpOnly. As claims são `IdUsuario`,
`Codigo`, `Nome` e `IdPlano`. O refresh token não é retornado em JSON; é rotacionado
e sua reutilização revoga a sessão. Operações de refresh exigem
`X-InvestLens-CSRF: 1` e HTTPS.

## Dados financeiros

A importação manual persiste empresas, ativos, indicadores e cotações. Não há endpoint
público de importação. O worker opcional seleciona ativos existentes no SQL e os
processa sequencialmente. Consulte:

- [Integração bolsai](../docs/bolsai-integration.md)
- [Banco financeiro](../docs/financial-database.md)
- [Importação](../docs/financial-import.md)
- [Atualização automática](../docs/financial-update.md)

## Logs e segurança

NLog grava em `Log/InvestLens-yyyyMMdd.log`, com retenção inicial de 30 dias. Nunca
versione API Keys, connection strings, senhas, JWTs ou refresh tokens. Em produção,
prefira Managed Identity para Azure SQL e Azure Key Vault para os segredos restantes.
