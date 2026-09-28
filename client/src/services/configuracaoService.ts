import { api } from './api'
import type { ConfiguracaoRequest, ConfiguracaoResponse } from '../types/configuracao'
const resource = (idUsuario: number) => `/usuarios/${idUsuario}/configuracao`
export const configuracaoService = {
  obter: async (idUsuario: number) => (await api.get<ConfiguracaoResponse>(resource(idUsuario))).data,
  adicionar: async (idUsuario: number, request: ConfiguracaoRequest) => (await api.post<ConfiguracaoResponse>(resource(idUsuario), request)).data,
  alterar: async (idUsuario: number, request: ConfiguracaoRequest) => (await api.put<ConfiguracaoResponse>(resource(idUsuario), request)).data,
}
