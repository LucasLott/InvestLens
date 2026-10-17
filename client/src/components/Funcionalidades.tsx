import { useEffect, useState } from 'react';
import { Check, X } from 'lucide-react';
import { funcionalidadeService, type Funcionalidade } from '../services/funcionalidadeService';
import { apresentacaoFuncionalidade } from '../config/funcionalidades';

export function Funcionalidades({ modo }: { modo: 'comparacao' | 'pro' | 'free' | 'minhas' }) {
  const [itens, setItens] = useState<Funcionalidade[] | null>(null);
  const [erro, setErro] = useState(false);
  useEffect(() => {
    let ativo = true;
    const consulta =
      modo === 'minhas' ? funcionalidadeService.minhas() : funcionalidadeService.listar();
    consulta
      .then((dados) => {
        if (ativo) setItens(dados);
      })
      .catch(() => {
        if (ativo) setErro(true);
      });
    return () => {
      ativo = false;
    };
  }, [modo]);
  if (erro) return <p role="alert">Não foi possível consultar as funcionalidades.</p>;
  if (itens === null) return <p>Carregando funcionalidades…</p>;
  return <ListaFuncionalidades itens={itens} modo={modo} />;
}

export function ListaFuncionalidades({
  itens,
  modo,
}: {
  itens: Funcionalidade[];
  modo: 'comparacao' | 'pro' | 'free' | 'minhas';
}) {
  const visiveis = itens.filter((item) =>
    modo === 'minhas' ? item.disponivel : modo === 'free' ? !item.exclusivaPro : true,
  );
  if (!visiveis.length) return <p>Nenhuma funcionalidade disponível para exibição.</p>;
  if (modo !== 'comparacao')
    return (
      <ul>
        {visiveis.map((item) => (
          <li key={item.idFuncionalidade}>
            {apresentacaoFuncionalidade(item.nome, item.descricao).descricao}
            {apresentacaoFuncionalidade(item.nome, item.descricao).emDesenvolvimento &&
              ' — Em desenvolvimento'}
          </li>
        ))}
      </ul>
    );
  return (
    <section className="plans-comparison container" aria-labelledby="comparacao-titulo">
      <h2 id="comparacao-titulo">Compare as funcionalidades</h2>
      <p>
        Os ícones indicam os recursos previstos em cada plano. Os itens em desenvolvimento ainda não
        estão disponíveis para uso.
      </p>
      <div className="comparison-row comparison-heading">
        <span>Funcionalidade</span>
        <span>Free</span>
        <span>Pro</span>
      </div>
      {visiveis.map((item) => (
        <div className="comparison-row" key={item.idFuncionalidade}>
          <span>
            {apresentacaoFuncionalidade(item.nome, item.descricao).descricao}
            {apresentacaoFuncionalidade(item.nome, item.descricao).emDesenvolvimento && (
              <small className="feature-planned">Em desenvolvimento</small>
            )}
          </span>
          <span className={item.exclusivaPro ? 'comparison-no' : 'comparison-yes'}>
            {item.exclusivaPro ? <X aria-hidden="true" /> : <Check aria-hidden="true" />}
            <span className="sr-only">{item.exclusivaPro ? 'Indisponível' : 'Disponível'}</span>
          </span>
          <span className="comparison-yes">
            <Check aria-hidden="true" />
            <span className="sr-only">Disponível</span>
          </span>
        </div>
      ))}
    </section>
  );
}
