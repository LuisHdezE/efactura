import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { App } from './app/App';
import './styles.css';
import './visual-parity.css';
import './navigation-compact.css';
import './runtime-product-images';
import './features/dashboard/dashboard-density.css';
import './features/dashboard/dashboard-icons.css';
import './features/suppliers/supplier-runtime-polish.css';
import './features/catalog/catalog-runtime-polish.css';
import './features/contingency/contingency-runtime-polish.css';

const routerBasename = import.meta.env.BASE_URL === '/'
  ? '/'
  : import.meta.env.BASE_URL.replace(/\/+$/, '');

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter basename={routerBasename}>
      <App />
    </BrowserRouter>
  </StrictMode>,
);