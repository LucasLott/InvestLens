import { Link } from 'react-router-dom'
import logo from '../assets/investlens-logo.png'
import '../styles/home.css'

export function HomePage() {
  return (
    <div className="home">
      <a className="skip-link" href="#conteudo">Pular para o conteúdo</a>
      <header className="site-header container">
        <Link className="wordmark" to="/" aria-label="InvestLens — página inicial">
          Invest<span>Lens</span><span className="brand-dot" aria-hidden="true" />
        </Link>
        <span className="stage-label"><span aria-hidden="true" />Em desenvolvimento</span>
      </header>
      <main id="conteudo" tabIndex={-1}>
        <section className="hero container" aria-labelledby="titulo">
          <div className="hero-copy">
            <p className="eyebrow">Uma nova perspectiva para investir</p>
            <h1 id="titulo">Enxergue além<br />dos <span>números.</span></h1>
            <p className="hero-description">Informação, critério e clareza para olhar seus investimentos por uma nova lente.</p>
            <a className="button button-primary" href="#proposta">Conheça a proposta <span aria-hidden="true">↗</span></a>
            <p className="hero-note">Uma experiência em construção. Um propósito claro.</p>
          </div>
          <figure className="brand-panel">
            <div className="panel-label"><span aria-hidden="true" />Perspectiva InvestLens</div>
            <img src={logo} alt="InvestLens: uma lupa envolvendo o símbolo de crescimento" width="1254" height="1254" fetchPriority="high" />
            <figcaption>Mais contexto. <span>Mais clareza.</span></figcaption>
          </figure>
        </section>
        <section className="principles container" id="proposta" aria-labelledby="proposta-titulo" tabIndex={-1}>
          <div className="principles-intro">
            <p className="eyebrow">Nosso ponto de partida</p>
            <h2 id="proposta-titulo">Boas análises começam<br />com uma visão clara.</h2>
          </div>
          <div className="principle">
            <span className="principle-number" aria-hidden="true">01 /</span>
            <h3>Informação com contexto</h3>
            <p>Dar significado aos números para ampliar sua perspectiva.</p>
          </div>
          <div className="principle">
            <span className="principle-number" aria-hidden="true">02 /</span>
            <h3>Critério na análise</h3>
            <p>Um olhar organizado, orientado pelo que faz sentido para você.</p>
          </div>
        </section>
      </main>
      <footer className="site-footer container">
        <span>InvestLens</span><p>Uma lente mais clara para seus investimentos.</p>
      </footer>
    </div>
  )
}
