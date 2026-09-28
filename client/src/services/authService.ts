import { api } from './api'
import type { LoginRequest, LoginResponse } from '../types/auth'

const browserRequest = {
  withCredentials: true,
  headers: { 'X-InvestLens-CSRF': '1' },
  skipAuth: true,
  skipAuthRefresh: true,
}

export const authService = {
  login: async (request: LoginRequest) =>
    (await api.post<LoginResponse>('/auth/login', request, browserRequest)).data,
  refresh: async () =>
    (await api.post<LoginResponse>('/auth/refresh', undefined, browserRequest)).data,
  logout: async () => {
    await api.post('/auth/revoke', undefined, browserRequest)
  },
}
