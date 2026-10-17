import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';
import { Button } from '../components/ui/Button';
import { planoService } from '../services/planoService';
import '../styles/planos.css';
import type { Plano } from '../types/plano';

const formatadorMoeda = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
});

export function PlanosPage() {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [planos, setPlanos] = useState<Plano[] | null>(null);
  const [erro, setErro] = useState(false);

  const carregarPlanos = () => {
    setErro(false);
    planoService
      .listar()
      .then(setPlanos)
      .catch(() => setErro(true));
  };

  useEffect(() => {
    planoService
      .listar()
      .then(setPlanos)
      .catch(() => setErro(true));
  }, []);

  const selecionarPlano = (plano: Plano) => {
    if (isAuthenticated) {
      navigate(plano.valor > 0 ? '/finalizar-contratacao' : '/inicio', {
        state: { idPlano: plano.idPlano },
      });
      return;
    }

    navigate('/cadastro', {
      state: {
        planoSelecionado: {
          idPlano: plano.idPlano,
          nome: plano.nome,
        },
      },
    });
  };

  return (
    <main className="plans-page">
      <header className="plans-header container">
        <Link className="wordmark" to="/">
          Invest<span>Lens</span>
        </Link>
        <Link className="ui-button ui-button-ghost" to="/">
          Voltar ao início
        </Link>
      </header>

      <section className="plans-hero container">
        <p className="eyebrow">Planos InvestLens</p>
        <h1>
          Escolha como quer <span>enxergar seus ativos.</span>
        </h1>
      </section>

      {planos === null && !erro ? (
        <p className="container">Carregando planos…</p>
      ) : erro ? (
        <section className="plans-message container">
          <strong>Não foi possível carregar os planos.</strong>
          <Button onClick={carregarPlanos}>Tentar novamente</Button>
        </section>
      ) : planos?.length === 0 ? (
        <p className="container">Nenhum plano está disponível no momento.</p>
      ) : (
        <section className="plans-grid container">
          {planos?.map((plano) => (
            <article
              className={`plan-card ${plano.nome === 'Pro' ? 'plan-card-pro' : ''}`}
              key={plano.idPlano}
            >
              {plano.nome === 'Pro' && <p className="plan-highlight">Recursos avançados</p>}
              <h2>{plano.nome}</h2>
              <p className="plan-price">
                <strong>{formatadorMoeda.format(plano.valor)}</strong>
              </p>
              <Button
                variant={plano.nome === 'Pro' ? 'primary' : 'secondary'}
                onClick={() => selecionarPlano(plano)}
              >
                Escolher {plano.nome}
              </Button>
            </article>
          ))}
        </section>
      )}
    </main>
  );
}
