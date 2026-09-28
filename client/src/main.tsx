import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from './App'
import './styles/tokens.css'
import './styles/global.css'
import './styles/forms.css'
import './styles/configuration.css'

const container = document.getElementById('root')
if (!container) throw new Error('Elemento de inicialização não encontrado.')
const root = createRoot(container)

// Validar mesmo quando a página ainda não faz chamadas HTTP.
void import('./config/env').then(
  () => root.render(<StrictMode><App /></StrictMode>),
  () => root.render(
    <main className="startup-error">
      <p className="eyebrow">InvestLens</p>
      <h1>Configuração inicial necessária.</h1>
      <p>Configure VITE_API_BASE_URL com a URL da API terminando em /api/v1 e reinicie a aplicação.</p>
    </main>,
  ),
)
