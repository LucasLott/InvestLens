# InvestLens — frontend

Fundação em React, TypeScript estrito e Vite. A única rota implementada é
`/`, com uma apresentação temporária do produto. Não há chamadas à API,
autenticação, dashboard ou dados financeiros simulados.

## Executar

Utilize Node.js 24 LTS e npm. Na pasta `client/`:

```powershell
npm ci
Copy-Item .env.example .env
# Preencha VITE_API_BASE_URL no arquivo .env.
npm run dev
```

`.env.example` contém apenas `VITE_API_BASE_URL=`. A URL informada precisa
ser HTTP(S), terminar em `/api/v1` e não conter credenciais, query ou fragmento.
O valor é público e incorporado ao bundle pelo Vite. Nunca usar secrets em
variáveis `VITE_*`. Arquivos `.env*` locais estão ignorados, com exceção do exemplo.

`src/config/env.ts` é o único ponto de leitura da variável, remove espaços e
barras finais e valida a URL. A inicialização verifica a configuração mesmo sem
chamadas HTTP. Uma configuração inválida exibe uma mensagem clara em vez da Home.
Após mudar o ambiente, reinicie o servidor; em produção, gere novamente o bundle.

`src/services/api.ts` exporta uma única instância Axios com
`baseURL: env.apiBaseUrl`. Services futuros utilizarão caminhos como
`/auth/login`, sem repetir `/api/v1`. Nenhum interceptor ou configuração de
cookies foi adicionado.

**Integração futura:** as rotas atuais do backend estão documentadas como
`/api/usuarios/...`; o contrato solicitado para este frontend é `/api/v1`.
Esse prefixo deverá ser alinhado no backend ou gateway antes de conectar os
services. Não houve alteração no backend nesta etapa.

## Estrutura

```text
client/
  .env.example
  index.html
  package.json
  package-lock.json
  eslint.config.js
  tsconfig.json
  tsconfig.node.json
  vite.config.ts
  src/
    assets/investlens-logo.png
    config/env.ts
    pages/HomePage.tsx
    routes/AppRoutes.tsx
    services/api.ts
    styles/
      tokens.css
      global.css
      home.css
    App.tsx
    main.tsx
    vite-env.d.ts
```

`App.tsx` contém BrowserRouter e `AppRoutes.tsx` define somente `/`.
A marca utiliza Link do Router; o link da proposta navega para uma seção real
da mesma página. No deploy, o servidor estático deve servir `index.html` como
fallback das futuras rotas SPA.

## Base visual e acessibilidade

CSS nativo, sem framework ou fontes externas. A imagem de marca é uma cópia
inalterada de `imgs/logo.png`. Azul-marinho, verde e superfícies claras seguem
a identidade existente; nenhum gráfico de dados foi implementado.

- `tokens.css`: cores semânticas, texto, bordas, foco, sucesso/aviso/erro,
  estados de interação, espaçamentos, raios, sombra e largura de conteúdo.
- `global.css`: base semântica, container, link de salto, botão, foco visível,
  estados hover/active/disabled/busy e respeito a movimento reduzido.
- `home.css`: composição da Home. Duas colunas em telas amplas, adaptação
  intermediária em 900px e coluna única abaixo de 600px. Tipografia e margens
  fluidas; não é usado overflow hidden para esconder problemas de layout.

Hierarquia h1/h2/h3, landmarks, textos alternativos, alvos de navegação de pelo
menos 44px, foco visível e link para pular ao conteúdo. Os textos descrevem o
propósito, sem apresentar funcionalidades financeiras como já disponíveis.

## Verificação

```powershell
npm run lint
npm run typecheck
npm run build
npm run preview
```

`build` também executa TypeScript antes de gerar `dist/`. O build pode ser
gerado sem ambiente; nesse caso, a aplicação construída exibirá a falha de
configuração na inicialização. Configure o ambiente correto antes do build de
publicação. Lockfile versionado; `node_modules/` e `dist/` ignorados.

Dependências de runtime: React, React DOM, React Router DOM e Axios.
Dependências de desenvolvimento: Vite, plugin React, TypeScript, tipos e ESLint
com plugins de React Hooks/Refresh. Nenhuma biblioteca de UI, estado global ou
autenticação foi adicionada.

Referências: [Vite](https://vite.dev/guide/),
[React Router](https://reactrouter.com/start/declarative/installation),
[Axios](https://axios-http.com/docs/instance).
