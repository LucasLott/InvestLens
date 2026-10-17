import assert from 'node:assert/strict';
import { after, before, test } from 'node:test';
import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { createServer } from 'vite';

let server;
let ListaFuncionalidades;
const itens = [
  {
    idFuncionalidade: 1,
    nome: 'CONFIGURACAO_CRITERIOS',
    descricao: 'Configuração',
    exclusivaPro: false,
    disponivel: true,
  },
  {
    idFuncionalidade: 2,
    nome: 'PRECO_MAXIMO',
    descricao: 'Preço',
    exclusivaPro: true,
    disponivel: false,
  },
];

before(async () => {
  process.env.VITE_API_BASE_URL = 'http://localhost:8080/api/v1';
  server = await createServer({
    server: { middlewareMode: true },
    appType: 'custom',
    logLevel: 'silent',
  });
  ({ ListaFuncionalidades } = await server.ssrLoadModule('/src/components/Funcionalidades.tsx'));
});

after(async () => {
  await server?.close();
});

function render(modo, dados = itens) {
  return renderToStaticMarkup(React.createElement(ListaFuncionalidades, { modo, itens: dados }));
}

test('comparação usa inclusão vinda da API e identifica recursos planejados', () => {
  const html = render('comparacao');
  assert.match(html, /Configuração de critérios/);
  assert.match(html, /Cálculo do preço máximo/);
  assert.match(html, /Em desenvolvimento/);
  assert.equal((html.match(/comparison-yes/g) ?? []).length, 3);
  assert.equal((html.match(/comparison-no/g) ?? []).length, 1);
  assert.match(html, /Indisponível/);
});

test('Free, Pro e minha assinatura exibem apenas os itens correspondentes', () => {
  assert.doesNotMatch(render('free'), /preço máximo/i);
  assert.match(render('pro'), /preço máximo/i);
  assert.doesNotMatch(render('minhas'), /preço máximo/i);
  assert.match(
    render(
      'minhas',
      itens.map((item) => ({ ...item, disponivel: true })),
    ),
    /preço máximo/i,
  );
});

test('catálogo vazio não inventa funcionalidades', () => {
  assert.match(render('comparacao', []), /Nenhuma funcionalidade/);
});
