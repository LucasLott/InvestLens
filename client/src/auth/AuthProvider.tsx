import { useCallback, useEffect, useMemo, useState } from 'react'
import type { PropsWithChildren } from 'react'
import { authSession } from './authSession'
import { AuthContext } from './authContext'
import { getAuthenticatedUser } from './jwt'
import { authService } from '../services/authService'
import type { AuthenticatedUser, LoginRequest, LoginResponse } from '../types/auth'
import type { AuthContextValue } from './authContext'

export function AuthProvider({ children }: PropsWithChildren) {
  const [user, setUser] = useState<AuthenticatedUser | null>(null)
  const [isInitializing, setIsInitializing] = useState(true)

  const applySession = useCallback((response: LoginResponse) => {
    const { accessToken } = response.token
    const authenticatedUser = getAuthenticatedUser(accessToken, response.email)
    authSession.setAccessToken(accessToken)
    setUser(authenticatedUser)
  }, [])

  const clearSession = useCallback(() => {
    authSession.setAccessToken(null)
    setUser(null)
  }, [])

  const restoreSession = useCallback(async () => {
    try {
      applySession(await authService.refresh())
    } catch {
      clearSession()
      throw new Error('N\u00e3o foi poss\u00edvel restaurar a sess\u00e3o.')
    }
  }, [applySession, clearSession])

  useEffect(() => {
    authSession.setRefreshAccessToken(restoreSession)
    void authSession.refresh().catch(() => undefined).finally(() => setIsInitializing(false))

    return () => authSession.setRefreshAccessToken(null)
  }, [restoreSession])

  const login = useCallback(async (request: LoginRequest) => {
    applySession(await authService.login(request))
  }, [applySession])

  const logout = useCallback(async () => {
    try {
      await authService.logout()
    } finally {
      clearSession()
    }
  }, [clearSession])

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: user !== null,
    isInitializing,
    login,
    logout,
  }), [isInitializing, login, logout, user])

  return <AuthContext value={value}>{isInitializing ? <AuthInitializing /> : children}</AuthContext>
}

function AuthInitializing() {
  return <main className="auth-initializing" aria-busy="true" aria-live="polite"><p className="eyebrow">InvestLens</p><h1>Preparando sua experi\u00eancia.</h1><p>Verificando sua sess\u00e3o com seguran\u00e7a.</p></main>
}
