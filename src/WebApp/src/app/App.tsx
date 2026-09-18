import { Navigate, Route, Routes } from 'react-router-dom';
import { AppShell } from '../layout/AppShell';
import { AuthPage } from '../features/auth/AuthPage';
import { PosPage } from '../features/pos/PosPage';
import { CustomersPage } from '../features/customers/CustomersPage';

export function App() {
  return (
    <Routes>
      <Route path="/acceso" element={<AuthPage />} />

      <Route element={<AppShell />}>
        <Route index element={<Navigate to="/pos" replace />} />
        <Route path="/pos" element={<PosPage />} />
        <Route path="/clientes" element={<CustomersPage />} />
        <Route path="*" element={<Navigate to="/pos" replace />} />
      </Route>
    </Routes>
  );
}
