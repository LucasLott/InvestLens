import { Link } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { PageHeader } from '../components/ui/PageHeader'
export function AuthenticatedHomePage() { const { user } = useAuth(); return <><PageHeader title={`Olá, ${user?.nome}.`} description="Sua área de análise está pronta para receber os critérios que orientam suas decisões." /><section className="empty-state"><span aria-hidden="true">↗</span><h2>Comece pela sua configuração.</h2><p>Defina os critérios que fazem sentido para sua estratégia antes de explorar oportunidades.</p><Link className="ui-button ui-button-primary" to="/configuracao">Configurar critérios</Link></section></> }
