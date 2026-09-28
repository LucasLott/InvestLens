import type { AuthenticatedUser } from '../types/auth'

interface JwtPayload {
  IdUsuario?: unknown
  Codigo?: unknown
  Nome?: unknown
}

export function getAuthenticatedUser(accessToken: string, email: string): AuthenticatedUser {
  const encodedPayload = accessToken.split('.')[1]
  if (!encodedPayload) throw new Error('Access Token inv\u00e1lido.')

  const payload = JSON.parse(decodeBase64Url(encodedPayload)) as JwtPayload
  const idUsuario = typeof payload.IdUsuario === 'string' ? Number(payload.IdUsuario) : NaN

  if (!Number.isSafeInteger(idUsuario) || typeof payload.Codigo !== 'string' || typeof payload.Nome !== 'string') {
    throw new Error('Claims obrigat\u00f3rias ausentes no Access Token.')
  }

  return { idUsuario, codigo: payload.Codigo, nome: payload.Nome, email }
}

function decodeBase64Url(value: string): string {
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/')
  const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')
  const bytes = Uint8Array.from(atob(padded), (character) => character.charCodeAt(0))
  return new TextDecoder().decode(bytes)
}
