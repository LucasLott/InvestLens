# Regras de desenvolvimento frontend

- Prettier é o formatador oficial do frontend.
- Utilize dois espaços de indentação, aspas simples no TypeScript e ponto e vírgula.
- Mantenha a largura preferencial de até 100 caracteres por linha.
- Nunca gere código minificado ou compacte componentes React.
- Separe declarações independentes em linhas distintas.
- Use nomes em português para páginas e funcionalidades de negócio, como `PlanosPage` e `planoService`.
- Preserve nomes técnicos e de infraestrutura já consolidados, como `api`, `Button` e `AppRoutes`.
- Siga as convenções do código existente antes de introduzir uma nova variação.
- Mantenha cada arquivo com uma responsabilidade clara e evite concentrar funcionalidades não relacionadas em arquivos extensos.
- Execute `npm run format` antes de concluir uma alteração e `npm run format:check` antes de finalizar.
