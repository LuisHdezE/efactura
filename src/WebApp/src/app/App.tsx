import { Navigate, Route, Routes } from 'react-router-dom';
import { AuthPage } from '../features/auth/AuthPage';
import { AppShell } from '../layout/AppShell';
import { defaultShellRoute, shellRoutes } from './routes';

export function App() {
  return (
    <Routes>
      <Route path="/acceso" element={<AuthPage />} />

      <Route element={<AppShell />}>
        <Route index element={<Navigate to={defaultShellRoute} replace />} />
        {shellRoutes.map(({ capability, element }) => (
          <Route key={capability.uiId} path={capability.route} element={element} />
        ))}
        <Route path="*" element={<Navigate to={defaultShellRoute} replace />} />
      </Route>
    </Routes>
  );
}
