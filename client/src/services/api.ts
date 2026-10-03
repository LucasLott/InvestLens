import axios from 'axios'
import { env } from '../config/env'
import { authSession } from '../auth/authSession'

declare module 'axios' {
  export interface AxiosRequestConfig {
    skipAuth?: boolean
    skipAuthRefresh?: boolean
  }

  export interface InternalAxiosRequestConfig {
    skipAuth?: boolean
    skipAuthRefresh?: boolean
    _authRetry?: boolean
  }
}

export const api = axios.create({ baseURL: env.apiBaseUrl, withCredentials: true })

api.interceptors.request.use((config) => {
  const token = authSession.getAccessToken()

  if (token && !config.skipAuth) config.headers.Authorization = `Bearer ${token}`

  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (!axios.isAxiosError(error) || error.response?.status !== 401 || !error.config) {
      return Promise.reject(error)
    }

    const request = error.config
    const method = request.method?.toLowerCase()
    const canRetry = method === 'get' || method === 'head' || method === 'options'

    if (request.skipAuth || request.skipAuthRefresh || request._authRetry || !canRetry) {
      return Promise.reject(error)
    }

    try {
      await authSession.refresh()
      request._authRetry = true
      return api(request)
    } catch {
      return Promise.reject(error)
    }
  },
)
