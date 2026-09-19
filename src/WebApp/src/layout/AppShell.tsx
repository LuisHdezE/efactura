import { useEffect, useState } from 'react';
import { Outlet, useLocation, useSearchParams } from 'react-router-dom';
import { shellRoutes } from '../app/routes';
import { BottomBar } from './BottomBar';
import { MobileNavigation } from './MobileNavigation';
import { Sidebar } from './Sidebar';
import { Topbar } from './Topbar';

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
      <Topbar
        theme={theme}
        onToggleTheme={() => setTheme((current) => current === 'dark' ? 'light' : 'dark')}
        search={isPos ? {
          value: catalogSearch,
          onChange: updateCatalogSearch,
          placeholder: 'Buscar productos por nombre o código…'
        } : undefined}
      />

      <div className="ef-workspace lg:grid lg:grid-cols-[128px_minmax(0,1fr)]">
        <Sidebar routes={shellRoutes} />

        <main className="min-w-0">
          <MobileNavigation routes={shellRoutes} />
          <Outlet />
          <BottomBar />
        </main>
      </div>
    </div>
  );
}
