import { useAuth } from '../auth/useAuth'

export function AuthenticatedAreaPage() {
  const { user } = useAuth()
  return <main className="auth-initializing"><p className="eyebrow">InvestLens</p><h1>Olá, {user?.nome}.</h1><p>Sua sessão foi autenticada com segurança.</p></main>
}
