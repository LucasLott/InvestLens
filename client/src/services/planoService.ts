import { api } from './api';
import type { Plano } from '../types/plano';

export const planoService = {
  listar: async () =>
    (await api.get<Plano[]>('/planos', { skipAuth: true, skipAuthRefresh: true })).data,
};
