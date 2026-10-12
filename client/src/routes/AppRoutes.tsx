import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from '../components/layout/AppLayout';
import { AuthenticatedHomePage } from '../pages/AuthenticatedHomePage';
import { ConfigurationPage } from '../pages/ConfigurationPage';
import { CadastroPage } from '../pages/CadastroPage';
import { LoginPage } from '../pages/LoginPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { ProtectedRoute } from './ProtectedRoute';
import { PlanosPage } from '../pages/PlanosPage';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/cadastro" element={<CadastroPage />} />
      <Route path="/planos" element={<PlanosPage />} />
      <Route path="/" element={<Navigate to="/login" replace />} />
      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/inicio" element={<AuthenticatedHomePage />} />
        <Route path="/configuracao" element={<ConfigurationPage />} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
