# InvestLens API

Backend ASP.NET Core .NET 10 com Controllers, organizado em `src/` e `tests/`.

## Dependências

- Domain: independente.
- Application → Domain. FluentValidation e seu registro por assembly.
- Infrastructure → Application e Domain. Dapper, Microsoft.Data.SqlClient e configuração por DI.
- Api → Application e Infrastructure para composição. NLog.Web.AspNetCore e Swashbuckle.AspNetCore.
- UnitTests → Infrastructure (contrato de saída de procedures).
- IntegrationTests → Api (startup, health, documentação e tratamento de erros).

Os testes usam xUnit, Microsoft.NET.Test.Sdk e Microsoft.AspNetCore.Mvc.Testing. Os diretórios reservados para futuras funcionalidades permanecem vazios, sem arquivos marcadores.

## Execução local

```powershell
dotnet restore server/InvestLens.sln
dotnet build server/InvestLens.sln --no-restore
dotnet test server/InvestLens.sln --no-build
dotnet run --project server/src/InvestLens.Api --launch-profile http
```

- Disponibilidade: `http://localhost:5124/health` (somente processo, sem consultar banco).
- Swagger UI: `http://localhost:5124/swagger`.
- OpenAPI: `http://localhost:5124/swagger/v1/swagger.json`, título InvestLens API, versão v1.

Swagger utiliza `SwaggerSettings` vinculada à seção `Swagger` pelo Options Pattern. `Title`, `Version` e `Description` configuram o documento e a rota usa a versão configurada. Em DEBUG, Swagger está sempre habilitado; em RELEASE, somente com `EnableSwaggerProd=true` (padrão atual: false), independentemente do ambiente de execução. Quando desabilitado, a UI e o documento não são expostos. A geração inclui os futuros Controllers. Neste estágio o documento não contém endpoints de negócio.

Somente `appsettings.json` contém configurações versionáveis e não sensíveis. O projeto Api possui UserSecretsId. O desenvolvedor poderá cadastrar `ConnectionStrings:InvestLens` por .NET User Secrets no desenvolvimento. Nenhum valor é fornecido ou necessário no startup. A factory retorna uma conexão fechada, com configuração lida sob demanda; o futuro repository deve abrir e descartar a conexão, propagando CancellationToken nas operações assíncronas.

## Erros e logs

`IExceptionHandler` produz ProblemDetails com `traceId`. FluentValidation e BusinessException correspondem a 400. Erros HTTP explícitos do ASP.NET Core (`BadHttpRequestException`) preservam 400/401/403/404/409. Exceptions desconhecidas e falhas técnicas correspondem a 500. Não se presume que exceções de acesso a arquivos ou de dicionários sejam erros de autenticação ou recursos HTTP inexistentes. Novas exceptions de domínio devem ser acrescentadas quando os casos de uso exigirem.

O contrato `ReturnCode/ErrMsg` distingue sucesso (0), regra de negócio (1) e falha técnica (2 ou contrato inválido). Não interpreta o valor SQL RETURN nem converte SqlException indiscriminadamente. Mensagens do banco nunca são publicadas ou registradas.

NLog recebe eventos via `ILogger<T>` e escreve em `${basedir}/Log/InvestLens-yyyyMMdd.log`, com troca diária por nome dinâmico e `maxArchiveDays=30`. A limpeza acontece quando o NLog escreve/abre os arquivos, não por um serviço independente enquanto a API está desligada. Não se combina nome dinâmico com `archiveEvery`.

Startup é Information; 400/401/403/404/409 são Warning; 5xx são Error. Um marcador por request evita duplicação entre o handler e o registro de respostas sem exception. Diagnósticos automáticos de exceptions tratadas são suprimidos no .NET 10, e somente as categorias da aplicação são encaminhadas ao arquivo.

O diagnóstico contém status, método, template da rota (ou `[unmatched]`), traceId, tipo da exception e métodos da stack para 5xx. Não copia mensagens arbitrárias de exceptions, valores da URL, headers, corpos ou dados de exceptions. Isso evita que credenciais apareçam mesmo em falhas técnicas.

Referências: [IExceptionHandler no .NET 10](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0), [NLog File target e retenção](https://github.com/NLog/NLog/wiki/File-target), [Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore).
