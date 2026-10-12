# InvestLens

Plataforma para análise de ativos brasileiros, com API .NET, interface React e base financeira alimentada pela bolsai.

## Stack

- .NET 10, ASP.NET Core Web API, Dapper, FluentValidation e NLog
- SQL Server / Azure SQL
- JWT HS256 e Refresh Token Rotation
- React 19, TypeScript, Vite e Axios
- Integração HTTP com a bolsai

## Estrutura

```text
client/      Aplicação React
server/      API, camadas de aplicação e testes
dbObjects/   Estrutura SQL, funções e stored procedures
docs/        Decisões e documentação técnica
imgs/        Referências visuais
```

## Funcionalidades atuais

- Cadastro, login, JWT, refresh token rotativo e logout.
- Configuração de critérios de análise por usuário.
- Empresas, ativos, indicadores, cotações e planos Free/Pro.
- Importação financeira interna idempotente.
- Worker de atualização financeira configurável e desabilitado por padrão.

## Início rápido

Pré-requisitos: .NET SDK 10, Node.js 22.12+, npm e SQL Server/Azure SQL.

```powershell
# API
dotnet restore server/InvestLens.sln
dotnet build server/InvestLens.sln --no-restore

# Frontend
cd client
npm ci
npm run dev
```

Antes de executar a API, configure Connection String, segredo JWT e chave da bolsai com User Secrets. O guia completo, com scripts SQL e exemplos seguros, está no [README da API](server/README.md).

## Configuração e segurança

Nunca versione valores reais de `ConnectionStrings:InvestLens`, `Authentication:Jwt:Secret` ou `Bolsai:ApiKey`. Use User Secrets no desenvolvimento e Key Vault/Managed Identity em produção. O [`.gitignore`](.gitignore) protege artefatos, logs, arquivos `.env`, configurações locais e IDEs.

## Documentação

- [Regras de banco](docs/database-rules.md)
- [Regras da API](docs/api-rules.md)
- [Integração bolsai](docs/bolsai-integration.md)
- [Banco financeiro](docs/financial-database.md)
- [Importação financeira](docs/financial-import.md)
- [Atualização automática](docs/financial-update.md)

## Testes

```powershell
dotnet test server/InvestLens.sln --no-build
```

Chamadas reais à bolsai são opt-in e não devem ser executadas no CI sem uma chave autorizada e orçamento de consumo confirmado.
