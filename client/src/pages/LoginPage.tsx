import { useState } from 'react'
import { isAxiosError } from 'axios'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import logo from '../assets/investlens-logo.png'
import '../styles/login.css'

interface LocationState { from?: string }
interface FormErrors { email?: string; senha?: string }

export function LoginPage() {
  const { isAuthenticated, login } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [errors, setErrors] = useState<FormErrors>({})
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  if (isAuthenticated) return <Navigate to="/" replace />
  const validate = () => {
    const next: FormErrors = {}
    if (!email.trim()) next.email = 'Informe seu e-mail.'
    else if (!/^\S+@\S+\.\S+$/.test(email.trim())) next.email = 'Informe um e-mail válido.'
    if (!senha) next.senha = 'Informe sua senha.'
    setErrors(next)
    return Object.keys(next).length === 0
  }
  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault(); setSubmitError(null)
    if (!validate()) return
    setIsSubmitting(true)
    try {
      await login({ email: email.trim(), senha }); setSenha('')
      const from = (location.state as LocationState | null)?.from
      navigate(from && from !== '/login' ? from : '/', { replace: true })
    } catch (error: unknown) { setSubmitError(getLoginErrorMessage(error)) } finally { setIsSubmitting(false) }
  }
  return <main className="login-page">
    <section className="login-intro" aria-labelledby="login-intro-title">
      <Link className="login-brand" to="/" aria-label="InvestLens — página inicial"><img src={logo} alt="" width="1254" height="1254" /><span>Invest<span>Lens</span><i aria-hidden="true" /></span></Link>
      <div className="login-intro-copy"><p className="eyebrow">Análise com intenção</p><h1 id="login-intro-title">Decisões melhores começam com uma visão mais clara.</h1><p>Organize sua análise, acompanhe seus critérios e enxergue oportunidades com mais contexto.</p></div>
      <div className="login-signals" aria-label="Diferenciais da InvestLens">
        <article><span className="signal-icon signal-icon-data" aria-hidden="true" /><div><h2>Informação com contexto</h2><p>Dados para apoiar uma análise consciente.</p></div></article>
        <article><span className="signal-icon signal-icon-filter" aria-hidden="true" /><div><h2>Critérios que fazem sentido</h2><p>Uma leitura alinhada à sua estratégia.</p></div></article>
        <article><span className="signal-icon signal-icon-chart" aria-hidden="true" /><div><h2>Clareza para decidir</h2><p>Menos ruído, mais perspectiva.</p></div></article>
      </div><div className="login-orbit login-orbit-one" aria-hidden="true" /><div className="login-orbit login-orbit-two" aria-hidden="true" />
    </section>
    <section className="login-access" aria-labelledby="login-title"><div className="login-card">
      <div className="login-card-brand" aria-hidden="true"><img src={logo} alt="" width="1254" height="1254" /></div>
      <header><p className="eyebrow">Acesso seguro</p><h2 id="login-title">Bem-vindo de volta.</h2><p>Acesse sua conta para continuar sua análise.</p></header>
      <form noValidate onSubmit={submit} aria-describedby={submitError ? 'login-submit-error' : undefined}>
        <div className="login-field"><label htmlFor="email">E-mail</label><input id="email" name="email" type="email" autoComplete="email" inputMode="email" value={email} onChange={(event) => setEmail(event.target.value)} onBlur={validate} disabled={isSubmitting} aria-invalid={Boolean(errors.email)} aria-describedby={errors.email ? 'email-error' : undefined} placeholder="voce@exemplo.com" />{errors.email && <p className="field-error" id="email-error" role="alert">{errors.email}</p>}</div>
        <div className="login-field"><label htmlFor="senha">Senha</label><div className="password-control"><input id="senha" name="senha" type={showPassword ? 'text' : 'password'} autoComplete="current-password" value={senha} onChange={(event) => setSenha(event.target.value)} onBlur={validate} disabled={isSubmitting} aria-invalid={Boolean(errors.senha)} aria-describedby={errors.senha ? 'senha-error' : undefined} placeholder="Digite sua senha" /><button type="button" className="password-toggle" onClick={() => setShowPassword((current) => !current)} disabled={isSubmitting} aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'} aria-pressed={showPassword}>{showPassword ? 'Ocultar' : 'Mostrar'}</button></div>{errors.senha && <p className="field-error" id="senha-error" role="alert">{errors.senha}</p>}</div>
        {submitError && <p className="login-submit-error" id="login-submit-error" role="alert">{submitError}</p>}
        <button className="login-submit" type="submit" disabled={isSubmitting} aria-busy={isSubmitting}><span>{isSubmitting ? 'Entrando com segurança' : 'Entrar'}</span><span aria-hidden="true">→</span></button>
      </form><footer><span aria-hidden="true">●</span> Seus dados são protegidos durante o acesso.</footer>
    </div></section>
  </main>
}

function getLoginErrorMessage(error: unknown) {
  if (!isAxiosError(error)) return 'Não foi possível concluir o acesso. Tente novamente.'
  if (!error.response) return 'Não foi possível conectar à plataforma. Verifique sua conexão e tente novamente.'
  if (error.response.status === 401) return 'Credenciais inválidas.'
  if (error.response.status === 400) return 'Verifique os dados informados e tente novamente.'
  return 'Ocorreu um erro ao acessar sua conta. Tente novamente em instantes.'
}
