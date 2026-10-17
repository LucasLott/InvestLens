import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { PageHeader } from '../components/ui/PageHeader';
import { api } from '../services/api';
import { planoService } from '../services/planoService';
import type { Plano } from '../types/plano';
import { Button } from '../components/ui/Button';
import { Funcionalidades } from '../components/Funcionalidades';

interface Assinatura {
  planoCadastrado: string;
  planoEfetivo: string;
  statusAssinatura: string | null;
  inicioVigencia: string | null;
  fimVigencia: string | null;
}

interface Solicitacao {
  idSolicitacao: number;
  tipo: string;
  status: string;
  planoOrigem: string;
  planoDestino: string | null;
  dataSolicitacao: string;
}

export function MinhaAssinaturaPage() {
  const [assinatura, setAssinatura] = useState<Assinatura | null>(null);
  const [erro, setErro] = useState(false);
  const [planos, setPlanos] = useState<Plano[]>([]);
  const [solicitacoes, setSolicitacoes] = useState<Solicitacao[]>([]);
  const [destino, setDestino] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [mensagem, setMensagem] = useState('');
  useEffect(() => {
    Promise.all([
      api.get<Assinatura>('/minha-assinatura'),
      api.get<Solicitacao[]>('/minha-assinatura/solicitacoes'),
      planoService.listar(),
    ])
      .then(([atual, pedidos, disponiveis]) => {
        setAssinatura(atual.data);
        setSolicitacoes(pedidos.data);
        setPlanos(disponiveis);
      })
      .catch(() => setErro(true));
  }, []);
  const registrar = async (tipo: 'alteracoes' | 'cancelamento') => {
    const plano = planos.find((item) => item.idPlano === Number(destino));
    const pergunta =
      tipo === 'alteracoes'
        ? `Solicitar alteração de ${assinatura?.planoEfetivo} para ${plano?.nome}? A alteração depende da futura integração de pagamento.`
        : `Solicitar cancelamento de ${assinatura?.planoEfetivo}? A cobrança não será cancelada nesta etapa e a vigência atual será preservada.`;
    if ((tipo === 'alteracoes' && !plano) || !window.confirm(pergunta)) return;
    setEnviando(true);
    setMensagem('');
    try {
      const response = await api.post<Solicitacao>(
        `/minha-assinatura/${tipo}`,
        tipo === 'alteracoes' ? { idPlanoDestino: plano?.idPlano } : undefined,
      );
      setSolicitacoes((anteriores) => [
        response.data,
        ...anteriores.filter((item) => item.idSolicitacao !== response.data.idSolicitacao),
      ]);
      setMensagem(
        tipo === 'alteracoes'
          ? 'Sua solicitação foi registrada. A alteração dependerá da disponibilização do pagamento.'
          : 'Sua solicitação de cancelamento foi registrada. O cancelamento da cobrança ainda não foi efetivado.',
      );
    } catch {
      setMensagem(
        'Não foi possível registrar a solicitação. Verifique se há uma solicitação conflitante ou sessão expirada.',
      );
    } finally {
      setEnviando(false);
    }
  };
  const data = (valor: string | null) =>
    valor ? new Date(valor).toLocaleDateString('pt-BR') : 'Não informada';
  return (
    <>
      <PageHeader title="Minha assinatura" description="Consulte seu plano e sua vigência." />
      {erro ? (
        <p role="alert">Não foi possível consultar sua assinatura.</p>
      ) : assinatura ? (
        <section className="empty-state">
          <p>Plano cadastrado: {assinatura.planoCadastrado}</p>
          <p>Plano efetivo: {assinatura.planoEfetivo}</p>
          <p>Situação: {assinatura.statusAssinatura ?? 'Sem assinatura paga'}</p>
          <p>Início: {data(assinatura.inicioVigencia)}</p>
          <p>Término: {data(assinatura.fimVigencia)}</p>
          <Funcionalidades modo="minhas" />
          <h2>Solicitações pendentes</h2>
          {solicitacoes.filter((item) => item.status === 'PEN').length === 0 ? (
            <p>Nenhuma solicitação pendente.</p>
          ) : (
            solicitacoes
              .filter((item) => item.status === 'PEN')
              .map((item) => (
                <p key={item.idSolicitacao}>
                  {item.tipo === 'ALT'
                    ? `Alteração de ${item.planoOrigem} para ${item.planoDestino}`
                    : `Cancelamento de ${item.planoOrigem}`}{' '}
                  — pendente desde {data(item.dataSolicitacao)}
                </p>
              ))
          )}
          <label htmlFor="plano-destino">Plano desejado</label>
          <select
            id="plano-destino"
            value={destino}
            onChange={(event) => setDestino(event.target.value)}
            disabled={enviando}
          >
            <option value="">Selecione</option>
            {planos
              .filter((item) => item.nome !== assinatura.planoEfetivo)
              .map((item) => (
                <option key={item.idPlano} value={item.idPlano}>
                  {item.nome} —{' '}
                  {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(
                    item.valor,
                  )}
                </option>
              ))}
          </select>
          <Button disabled={enviando || !destino} onClick={() => void registrar('alteracoes')}>
            Trocar plano
          </Button>
          {assinatura.planoEfetivo !== 'Free' && (
            <Button disabled={enviando} onClick={() => void registrar('cancelamento')}>
              Cancelar assinatura
            </Button>
          )}
          {mensagem && <p role="status">{mensagem}</p>}
          <Link className="ui-button ui-button-primary" to="/planos">
            Consultar planos
          </Link>
        </section>
      ) : (
        <p>Carregando assinatura…</p>
      )}
    </>
  );
}
