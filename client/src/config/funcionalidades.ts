const descricoes: Record<string, string> = {
  BUSCA_ATIVOS: 'Busca de ativos',
  CONSULTA_ATIVOS: 'Consulta de ativos',
  COTACAO_ATUAL: 'Cotação atual',
  DADOS_BASICOS_EMPRESA: 'Dados básicos da empresa',
  INDICADORES_BASICOS: 'Indicadores fundamentalistas básicos',
  CONFIGURACAO_CRITERIOS: 'Configuração de critérios',
  INDICADORES_AVANCADOS: 'Indicadores fundamentalistas avançados',
  FILTROS_AVANCADOS: 'Filtros fundamentalistas avançados',
  HISTORICO_DIVIDENDOS: 'Histórico de dividendos',
  HISTORICO_PRECOS: 'Histórico de preços',
  DADOS_FINANCEIROS: 'Dados financeiros detalhados',
  DEMONSTRACOES_FINANCEIRAS: 'Demonstrações financeiras',
  PRECO_MAXIMO: 'Cálculo do preço máximo',
  ANALISE_FUNDAMENTALISTA: 'Análise fundamentalista',
  COMPARACAO_ATIVOS: 'Comparação de ativos',
};

// Status de apresentação, não determina autorização nem altera o catálogo.
const operacionais = new Set(['CONFIGURACAO_CRITERIOS']);

export function apresentacaoFuncionalidade(nome: string, descricao: string) {
  return { descricao: descricoes[nome] ?? descricao, emDesenvolvimento: !operacionais.has(nome) };
}
