import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../../auth/useAuth'
import { Button } from '../ui/Button'
import '../../styles/app-layout.css'

export function AppLayout() {
  const { user, logout } = useAuth(); const navigate = useNavigate(); const [open, setOpen] = useState(false)
  const leave = async () => { await logout(); navigate('/login', { replace: true }) }
  return <div className="app-shell"><a className="skip-link" href="#app-content">Pular para o conteúdo</a><aside className={`app-sidebar ${open ? 'is-open' : ''}`}><NavLink className="app-logo" to="/" onClick={() => setOpen(false)}>Invest<span>Lens</span><i /></NavLink><nav aria-label="Navegação principal"><NavLink to="/" end onClick={() => setOpen(false)}>Início</NavLink><NavLink to="/configuracao" onClick={() => setOpen(false)}>Configuração</NavLink></nav><div className="app-user"><span aria-hidden="true">{user?.nome.slice(0, 1)}</span><div><strong>{user?.nome}</strong><small>Código {user?.codigo}</small></div><Button variant="ghost" onClick={leave}>Sair</Button></div></aside><div className="app-workspace"><header className="app-topbar"><button className="menu-toggle" type="button" onClick={() => setOpen((value) => !value)} aria-expanded={open} aria-controls="app-navigation">Menu</button><span>{user?.nome}</span></header><main id="app-content" className="app-content"><Outlet /></main></div></div>
}
