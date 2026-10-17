import { useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { PageHeader } from '../components/ui/PageHeader';
import { planoService } from '../services/planoService';
import type { Plano } from '../types/plano';

export function FinalizarContratacaoPage() {
  const { state } = useLocation();
  const idPlano: unknown = state?.idPlano;
  const [planos, setPlanos] = useState<Plano[] | null>(null);
  const [erro, setErro] = useState(false);

  useEffect(() => {
    planoService
      .listar()
      .then(setPlanos)
      .catch(() => setErro(true));
  }, []);

  const plano = planos?.find((item) => item.idPlano === idPlano && item.valor > 0);
  return (
    <>
      <PageHeader
        title="Finalizar contratação"
        description="Confira sua intenção de contratação."
      />
      {erro ? (
        <p role="alert">Não foi possível consultar os planos. Tente novamente mais tarde.</p>
      ) : planos === null ? (
        <p>Carregando plano…</p>
      ) : plano ? (
        <section className="empty-state">
          <h2>{plano.nome}</h2>
          <p>
            {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(
              plano.valor,
            )}
          </p>
          <p>A contratação online estará disponível em breve.</p>
          <p>As funcionalidades do plano estão indisponíveis para consulta no momento.</p>
          <p>Esta escolha não altera seu plano efetivo.</p>
        </section>
      ) : (
        <p role="status">O plano selecionado não está disponível. Escolha um plano novamente.</p>
      )}
      <Link className="ui-button ui-button-primary" to="/inicio">
        Continuar com o plano Free
      </Link>{' '}
      <Link to="/planos">Consultar planos</Link>
    </>
  );
}
