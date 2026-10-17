import { api } from './api';

export interface Funcionalidade {
  idFuncionalidade: number;
  nome: string;
  descricao: string;
  exclusivaPro: boolean;
  disponivel: boolean | null;
}

export const funcionalidadeService = {
  listar: async () =>
    (await api.get<Funcionalidade[]>('/funcionalidades', { skipAuth: true, skipAuthRefresh: true }))
      .data,
  minhas: async () => (await api.get<Funcionalidade[]>('/minhas-funcionalidades')).data,
};
