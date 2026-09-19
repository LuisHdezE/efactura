type Theme = 'light' | 'dark';

interface TopbarSearch {
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
}

interface TopbarProps {
  theme: Theme;
  onToggleTheme: () => void;
  search?: TopbarSearch;
}

export function Topbar({ theme, onToggleTheme, search }: TopbarProps) {
  return (
    <header className="ef-global-topbar sticky top-0 z-40">
      <div className="ef-header-brand">
        <span className="ef-menu-glyph" aria-hidden="true">☰</span>
        <div>
          <div className="ef-header-title">eFactura Demo</div>
          <div className="ef-header-subtitle">Facturación electrónica Uruguay</div>
        </div>
      </div>

      <div className="ef-header-search-wrap">
        {search && (
          <label className="ef-global-search-shell">
            <span aria-hidden="true">⌕</span>
            <input
              value={search.value}
              onChange={(event) => search.onChange(event.target.value)}
              placeholder={search.placeholder}
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
          onClick={onToggleTheme}
          className="ef-theme-toggle"
          aria-label={theme === 'dark' ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'}
          title={theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}
        >
          <span aria-hidden="true">{theme === 'dark' ? '☀' : '☾'}</span>
          <span className="hidden xl:inline">{theme === 'dark' ? 'Claro' : 'Oscuro'}</span>
        </button>
      </div>
    </header>
  );
}
