export interface LoginRequest {
  email: string
  senha: string
}

export interface TokenResponse {
  accessToken: string
  expiration: string
}

export interface LoginResponse {
  nome: string
  email: string
  token: TokenResponse
}

export interface AuthenticatedUser {
  idUsuario: number
  codigo: string
  nome: string
  email: string
}
