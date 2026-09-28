const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim().replace(/\/+$/, '')

if (!apiBaseUrl) {
  throw new Error('VITE_API_BASE_URL é obrigatória. Configure client/.env antes de iniciar.')
}

const url = new URL(apiBaseUrl)

if (
  !['http:', 'https:'].includes(url.protocol) ||
  url.username || url.password || url.search || url.hash ||
  !url.pathname.endsWith('/api/v1')
) {
  throw new Error('VITE_API_BASE_URL deve ser uma URL HTTP(S), sem credenciais, terminando em /api/v1.')
}

export const env = Object.freeze({ apiBaseUrl })
