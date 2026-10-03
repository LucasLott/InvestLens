import { useEffect, useState } from 'react'
import { isAxiosError } from 'axios'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { Eye, EyeOff } from 'lucide-react'
import { useAuth } from '../auth/useAuth'
import { Alert, FormField } from '../components/ui/FormField'
import { Input } from '../components/ui/Input'
import { Button } from '../components/ui/Button'
import { usuarioService } from '../services/usuarioService'
import type { CadastroUsuarioRequest } from '../types/usuario'
import { formatCpf, isValidCpf, sanitizeCpf } from '../utils/cpf'
import logoSimple from '../../../imgs/logo_simples.png'
import '../styles/cadastro.css'

type FormValues = CadastroUsuarioRequest & { aceitouTermos: boolean }
type FormErrors = Partial<Record<keyof FormValues, string>>

const initialValues: FormValues = { nome: '', cpf: '', email: '', senha: '', confirmacaoSenha: '', aceitouTermos: false }

export function CadastroPage() {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const [values, setValues] = useState<FormValues>(initialValues)
  const [errors, setErrors] = useState<FormErrors>({})
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [showPassword, setShowPassword] = useState(false)

  useEffect(() => {
    if (!success) return
    const timeout = window.setTimeout(() => navigate('/login', { replace: true }), 1400)
    return () => window.clearTimeout(timeout)
  }, [navigate, success])

  if (isAuthenticated) return <Navigate to="/" replace />

  const update = <K extends keyof FormValues>(key: K, value: FormValues[K]) => {
    setValues((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const { aceitouTermos, ...registration } = values
    const request = { ...registration, cpf: sanitizeCpf(values.cpf) }
    const nextErrors = validate({ ...request, aceitouTermos })
    setValues((current) => ({ ...current, cpf: request.cpf }))
    setErrors(nextErrors)
    setError(null)
    setSuccess(false)
    if (Object.keys(nextErrors).length) return

    setIsSubmitting(true)
    try {
      await usuarioService.cadastrar(request)
      setValues(initialValues)
      setSuccess(true)
    } catch (exception: unknown) {
      setError(messageFor(exception))
    } finally {
      setIsSubmitting(false)
    }
  }

  return <main className="cadastro-page">
    <a className="skip-link" href="#cadastro-form">Ir para o cadastro</a>
    <section className="cadastro-panel">
      <Link className="cadastro-brand" to="/login" aria-label="InvestLens, ir para login">
        <img src={logoSimple} alt="" />
        <b>Invest<span>Lens</span></b>
      </Link>
      <div className="cadastro-copy">
        <p className="eyebrow">Comece agora</p>
        <h1>Crie sua conta InvestLens</h1>
        <p>Organize seus critérios e encontre oportunidades com uma análise mais clara.</p>
      </div>
      <p className="cadastro-security">Seus dados são tratados com segurança. Você poderá configurar sua estratégia após entrar.</p>
    </section>
    <section className="cadastro-access">
      <div className="cadastro-card">
        <header><h2>Cadastro</h2><p>Preencha seus dados para criar sua conta.</p></header>
        <form id="cadastro-form" onSubmit={submit} noValidate>
          <FormField label="Nome" htmlFor="nome" error={errors.nome}>
            <Input id="nome" type="text" autoComplete="name" value={values.nome} disabled={isSubmitting} maxLength={80} aria-invalid={Boolean(errors.nome)} aria-describedby={errors.nome ? 'nome-error' : undefined} onChange={(event) => update('nome', event.target.value)} placeholder="Como podemos chamar você?" />
          </FormField>
          <FormField label="CPF" htmlFor="cpf" error={errors.cpf}>
            <Input id="cpf" type="text" inputMode="numeric" autoComplete="off" value={formatCpf(values.cpf)} disabled={isSubmitting} aria-invalid={Boolean(errors.cpf)} aria-describedby={errors.cpf ? 'cpf-error' : undefined} onChange={(event) => update('cpf', sanitizeCpf(event.target.value))} placeholder="000.000.000-00" />
          </FormField>
          <FormField label="E-mail" htmlFor="email" error={errors.email}>
            <Input id="email" type="email" autoComplete="email" value={values.email} disabled={isSubmitting} maxLength={255} aria-invalid={Boolean(errors.email)} aria-describedby={errors.email ? 'email-error' : undefined} onChange={(event) => update('email', event.target.value)} placeholder="voce@exemplo.com" />
          </FormField>
          <FormField label="Senha" htmlFor="senha" error={errors.senha}>
            <div className="cadastro-password-control"><Input id="senha" type={showPassword ? 'text' : 'password'} autoComplete="new-password" value={values.senha} disabled={isSubmitting} aria-invalid={Boolean(errors.senha)} aria-describedby={errors.senha ? 'senha-error' : undefined} onChange={(event) => update('senha', event.target.value)} placeholder="No mínimo 6 caracteres" /><button type="button" onClick={() => setShowPassword((current) => !current)} aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'}>{showPassword ? <EyeOff /> : <Eye />}</button></div>
          </FormField>
          <FormField label="Confirmar senha" htmlFor="confirmacaoSenha" error={errors.confirmacaoSenha}>
            <Input id="confirmacaoSenha" type={showPassword ? 'text' : 'password'} autoComplete="new-password" value={values.confirmacaoSenha} disabled={isSubmitting} aria-invalid={Boolean(errors.confirmacaoSenha)} aria-describedby={errors.confirmacaoSenha ? 'confirmacaoSenha-error' : undefined} onChange={(event) => update('confirmacaoSenha', event.target.value)} placeholder="Digite a senha novamente" />
          </FormField>
          <div className="cadastro-terms">
            <input id="aceitouTermos" type="checkbox" checked={values.aceitouTermos} disabled={isSubmitting} aria-invalid={Boolean(errors.aceitouTermos)} aria-describedby={errors.aceitouTermos ? 'aceitouTermos-error' : undefined} onChange={(event) => update('aceitouTermos', event.target.checked)} />
            <label htmlFor="aceitouTermos">Li e concordo com os <a href="/documentos/termos-de-uso.pdf" target="_blank" rel="noreferrer">Termos de Uso e Políticas</a>.</label>
            {errors.aceitouTermos && <p className="form-field-error" id="aceitouTermos-error" role="alert">{errors.aceitouTermos}</p>}
          </div>
          {error && <Alert>{error}</Alert>}
          {success && <p className="cadastro-success" role="status">Cadastro realizado com sucesso. Redirecionando para o login…</p>}
          <Button type="submit" loading={isSubmitting} className="cadastro-submit">{isSubmitting ? 'Criando conta…' : 'Criar conta'}</Button>
        </form>
        <p className="cadastro-login">Já possui uma conta? <Link to="/login">Entrar</Link></p>
      </div>
    </section>
  </main>
}

function validate(values: FormValues): FormErrors {
  const errors: FormErrors = {}
  if (!values.nome.trim()) errors.nome = 'Informe seu nome.'
  else if (values.nome.length > 80) errors.nome = 'O nome não pode exceder 80 caracteres.'
  if (!isValidCpf(values.cpf)) errors.cpf = 'Informe um CPF válido.'
  if (!values.email.trim()) errors.email = 'Informe seu e-mail.'
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email)) errors.email = 'Informe um e-mail válido.'
  if (values.senha.length < 6) errors.senha = 'A senha deve ter no mínimo 6 caracteres.'
  if (!values.confirmacaoSenha) errors.confirmacaoSenha = 'Confirme sua senha.'
  else if (values.confirmacaoSenha !== values.senha) errors.confirmacaoSenha = 'As senhas não coincidem.'
  if (!values.aceitouTermos) errors.aceitouTermos = 'Você precisa concordar com os Termos de Uso e Políticas.'
  return errors
}

function messageFor(error: unknown) {
  if (isAxiosError(error) && error.response?.status === 400) return 'Revise os dados informados e tente novamente.'
  return 'Não foi possível concluir o cadastro. Tente novamente.'
}
