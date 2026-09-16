import { useEffect, useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
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

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;
    window.localStorage.setItem(themeStorageKey, theme);
  }, [theme]);

  return (
    <div className="ef-app min-h-screen lg:grid lg:grid-cols-[116px_1fr]">
      <aside className="ef-sidebar hidden min-h-screen lg:flex lg:flex-col">
        <div className="ef-brand">
          <div className="ef-brand-mark">e</div>
          <div className="min-w-0">
            <div className="truncate text-sm font-bold">eFactura</div>
            <div className="text-[10px] opacity-60">Demo UY</div>
          </div>
        </div>

        <nav className="mt-5 space-y-1.5">
          {uiCapabilities.map((item) => (
            <NavLink
              key={item.uiId}
              to={item.route}
              className={({ isActive }) => `ef-nav-link ${isActive ? 'is-active' : ''}`}
            >
              <span className="ef-nav-icon" aria-hidden="true">{item.uiId === 'UI-POS-001' ? '▦' : '◉'}</span>
              <span>{item.uiId === 'UI-POS-001' ? 'POS' : item.label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="mt-auto border-t border-current/10 pt-4 text-center text-[10px] opacity-60">
          <div className="font-semibold">🇺🇾 UY</div>
          <div className="mt-1">Modo demo</div>
        </div>
      </aside>

      <main className="min-w-0">
        <header className="ef-topbar sticky top-0 z-30 flex h-14 items-center justify-between px-3 sm:px-4 lg:px-5">
          <div className="flex min-w-0 items-center gap-3">
            <div className="lg:hidden ef-brand-mark">e</div>
            <div className="min-w-0">
              <div className="truncate text-sm font-bold">eFactura Demo</div>
              <div className="hidden text-[11px] opacity-55 sm:block">Facturación electrónica · Uruguay</div>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <div className="hidden rounded-lg border border-current/10 px-2.5 py-1.5 text-right sm:block">
              <div className="text-[11px] font-semibold">Caja demo</div>
              <div className="text-[10px] opacity-55">Mock local</div>
            </div>
            <button
              type="button"
              onClick={() => setTheme((current) => current === 'dark' ? 'light' : 'dark')}
              className="ef-theme-toggle"
              aria-label={theme === 'dark' ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'}
              title={theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}
            >
              <span aria-hidden="true">{theme === 'dark' ? '☀' : '☾'}</span>
              <span className="hidden sm:inline">{theme === 'dark' ? 'Claro' : 'Oscuro'}</span>
            </button>
            <div className="hidden text-right md:block">
              <div className="text-xs font-semibold">Carlos A.</div>
              <div className="text-[10px] opacity-55">Operador</div>
            </div>
          </div>
        </header>

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
      </main>
    </div>
  );
}
