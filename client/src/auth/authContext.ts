import { createContext } from 'react'
import type { LoginRequest, AuthenticatedUser } from '../types/auth'

export interface AuthContextValue {
  user: AuthenticatedUser | null
  isAuthenticated: boolean
  isInitializing: boolean
  login: (request: LoginRequest) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
