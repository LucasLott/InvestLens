import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';
import { Button } from '../components/ui/Button';
import { planoService } from '../services/planoService';
import '../styles/planos.css';
import type { Plano } from '../types/plano';

const formatadorMoeda = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
});

const recursosPorPlano: Record<string, string[]> = {
  Free: ['Busca de ativos', 'Informações básicas', 'Cotação atual', 'Indicadores atuais'],
  Pro: [
    'Tudo do Free',
    'Histórico de proventos',
    'Históricos avançados',
    'Screener e filtros',
    'Preço máximo',
  ],
};

export function PlanosPage() {
  const { isAuthenticated } = useAuth();
  const [planos, setPlanos] = useState<Plano[] | null>(null);
  const [erro, setErro] = useState(false);
  const [avisoPro, setAvisoPro] = useState(false);

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

  return (
    <main className="plans-page">
      <header className="plans-header container">
        <Link className="wordmark" to={isAuthenticated ? '/inicio' : '/login'}>
          Invest<span>Lens</span>
        </Link>
        <Link className="ui-button ui-button-ghost" to={isAuthenticated ? '/inicio' : '/login'}>
          {isAuthenticated ? 'Voltar ao início' : 'Entrar'}
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
                {plano.nome === 'Pro' && <span>/ mês</span>}
              </p>
              <ul>
                {(recursosPorPlano[plano.nome] ?? ['Detalhes do plano disponíveis em breve.']).map(
                  (recurso) => (
                    <li key={recurso}>✓ {recurso}</li>
                  ),
                )}
              </ul>
              {plano.nome === 'Free' ? (
                <Link
                  className="ui-button ui-button-secondary"
                  to={isAuthenticated ? '/inicio' : '/cadastro'}
                >
                  {isAuthenticated ? 'Continuar' : 'Criar conta gratuita'}
                </Link>
              ) : (
                <Button onClick={() => setAvisoPro(true)}>Conhecer o Pro</Button>
              )}
            </article>
          ))}
        </section>
      )}

      {avisoPro && (
        <section className="plans-message container" role="status">
          <strong>O InvestLens Pro estará disponível em breve.</strong>
          <p>A contratação não está ativa e nenhum acesso foi alterado.</p>
        </section>
      )}
    </main>
  );
}
