import { useEffect, useState } from 'react';
import { NavLink, Outlet, useLocation, useSearchParams } from 'react-router-dom';
import { uiCapabilities } from '../app/capabilities';

type Theme = 'light' | 'dark';

const themeStorageKey = 'efactura-theme';

function getInitialTheme(): Theme {
  const saved = window.localStorage.getItem(themeStorageKey);
  if (saved === 'light' || saved === 'dark') return saved;
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export function AppShell() {
  const [theme, setTheme] = useState<Theme>(getInitialTheme);
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();
  const catalogSearch = searchParams.get('q') ?? '';
  const isPos = location.pathname === '/pos';

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;
    window.localStorage.setItem(themeStorageKey, theme);
  }, [theme]);

  const updateCatalogSearch = (value: string) => {
    const next = new URLSearchParams(searchParams);
    if (value.trim()) next.set('q', value);
    else next.delete('q');
    setSearchParams(next, { replace: true });
  };

  return (
    <div className="ef-app min-h-screen">
      <header className="ef-global-topbar sticky top-0 z-40">
        <div className="ef-header-brand">
          <span className="ef-menu-glyph" aria-hidden="true">☰</span>
          <div>
            <div className="ef-header-title">eFactura Demo</div>
            <div className="ef-header-subtitle">Facturación electrónica Uruguay</div>
          </div>
        </div>

        <div className="ef-header-search-wrap">
          {isPos && (
            <label className="ef-global-search-shell">
              <span aria-hidden="true">⌕</span>
              <input
                value={catalogSearch}
                onChange={(event) => updateCatalogSearch(event.target.value)}
                placeholder="Buscar productos por nombre o código…"
                aria-label="Buscar productos"
              />
              <kbd>Ctrl + K</kbd>
            </label>
          )}
        </div>

        <div className="ef-header-actions">
          <div className="ef-cash-card">
            <div className="font-bold">Caja demo</div>
            <div><span className="ef-online-dot" /> Mock local</div>
          </div>
          <div className="ef-user-badge">A</div>
          <div className="ef-user-copy">
            <strong>Admin</strong>
            <span>Demo Uruguay</span>
          </div>
          <button
            type="button"
            onClick={() => setTheme((current) => current === 'dark' ? 'light' : 'dark')}
            className="ef-theme-toggle"
            aria-label={theme === 'dark' ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'}
            title={theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}
          >
            <span aria-hidden="true">{theme === 'dark' ? '☀' : '☾'}</span>
            <span className="hidden xl:inline">{theme === 'dark' ? 'Claro' : 'Oscuro'}</span>
          </button>
        </div>
      </header>

      <div className="ef-workspace lg:grid lg:grid-cols-[128px_minmax(0,1fr)]">
        <aside className="ef-sidebar hidden lg:flex lg:flex-col">
          <nav className="space-y-1">
            {uiCapabilities.map((item) => (
              <NavLink
                key={item.uiId}
                to={item.route}
                className={({ isActive }) => `ef-nav-link ${isActive ? 'is-active' : ''}`}
              >
                <span className="ef-nav-icon" aria-hidden="true">{item.uiId === 'UI-POS-001' ? '🛒' : '◉'}</span>
                <span>{item.uiId === 'UI-POS-001' ? 'POS' : item.label}</span>
              </NavLink>
            ))}
          </nav>

          <div className="ef-sidebar-footer">
            <span>🇺🇾</span>
            <strong>UY</strong>
          </div>
        </aside>

        <main className="min-w-0">
          <div className="ef-mobile-nav border-b px-3 py-2 lg:hidden">
            <nav className="flex gap-2 overflow-x-auto">
              {uiCapabilities.map((item) => (
                <NavLink key={item.uiId} to={item.route} className={({ isActive }) => `ef-mobile-link ${isActive ? 'is-active' : ''}`}>
                  {item.uiId === 'UI-POS-001' ? 'POS' : item.label}
                </NavLink>
              ))}
            </nav>
          </div>

          <Outlet />

          <footer className="ef-app-footer">
            <span>eFactura Demo</span>
            <span>Ambiente de demostración</span>
            <span>Fiscalidad autoritativa: servidor</span>
          </footer>
        </main>
      </div>
    </div>
  );
}
