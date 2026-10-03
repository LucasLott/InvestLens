import { api } from './api'
import type { CadastroUsuarioRequest } from '../types/usuario'

export const usuarioService = {
  cadastrar: async (request: CadastroUsuarioRequest) => {
    await api.post('/usuario', request, { skipAuth: true, skipAuthRefresh: true })
  },
}
