import { useEffect, useState } from 'react';
import './auth-scenery.css';
import './auth.css';

type Theme = 'light' | 'dark';

const themeStorageKey = 'efactura-theme';

function getInitialTheme(): Theme {
  const saved = window.localStorage.getItem(themeStorageKey);
  if (saved === 'light' || saved === 'dark') return saved;
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function BrandMark() {
  return (
    <span className="auth-brand-mark" aria-hidden="true">
      <span className="auth-brand-fold" />
      <span className="auth-brand-line auth-brand-line-one" />
      <span className="auth-brand-line auth-brand-line-two" />
      <span className="auth-brand-line auth-brand-line-three" />
    </span>
  );
}

export function AuthPage() {
  const [theme, setTheme] = useState<Theme>(getInitialTheme);
  const [demoNoticeVisible, setDemoNoticeVisible] = useState(false);

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;
    window.localStorage.setItem(themeStorageKey, theme);
  }, [theme]);

  return (
    <div className="auth-page">
      <header className="auth-header">
        <div className="auth-brand">
          <BrandMark />
          <div>
            <div className="auth-brand-title">eFactura</div>
            <div className="auth-brand-subtitle">Facturación electrónica Uruguay</div>
          </div>
        </div>

        <div className="auth-header-controls">
          <span className="auth-language" aria-label="Idioma español">
            <span aria-hidden="true">◎</span>
            ES
          </span>
          <button
            type="button"
            className="auth-theme-button"
            onClick={() => setTheme((current) => current === 'dark' ? 'light' : 'dark')}
            aria-label={theme === 'dark' ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'}
            title={theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}
          >
            <span aria-hidden="true">{theme === 'dark' ? '☀' : '☾'}</span>
            <span>{theme === 'dark' ? 'Claro' : 'Oscuro'}</span>
          </button>
        </div>
      </header>

      <main className="auth-main">
        <section className="auth-copy" aria-labelledby="auth-title">
          <div className="auth-kicker">
            <span aria-hidden="true" />
            Bienvenido a eFactura
          </div>

          <h1 id="auth-title" className="auth-title">
            Tu facturación,
            <strong>más simple</strong>
          </h1>

          <p className="auth-lead">
            Gestiona tus comprobantes, clientes y operaciones de forma segura,
            moderna y en conformidad con la normativa de Uruguay.
          </p>

          <div className="auth-benefits" aria-label="Beneficios">
            <div className="auth-benefit">
              <span className="auth-benefit-icon" aria-hidden="true">▤</span>
              <div>
                <strong>Operación simple</strong>
                <span>Todo en un solo lugar</span>
              </div>
            </div>

            <div className="auth-benefit">
              <span className="auth-benefit-icon" aria-hidden="true">◇</span>
              <div>
                <strong>Seguro y confiable</strong>
                <span>Protegemos tu información</span>
              </div>
            </div>

            <div className="auth-benefit">
              <span className="auth-benefit-icon" aria-hidden="true">▥</span>
              <div>
                <strong>Siempre actualizado</strong>
                <span>En alineación con DGI Uruguay</span>
              </div>
            </div>
          </div>
        </section>

        <section className="auth-access-card" aria-labelledby="auth-access-title">
          <div className="auth-demo-label">Demo visual · identidad pendiente de integración</div>

          <h2 id="auth-access-title">Acceso seguro</h2>
          <p className="auth-access-copy">
            Continúa con tu proveedor de identidad para ingresar a eFactura.
          </p>

          <button
            type="button"
            className="auth-primary-button"
            onClick={() => setDemoNoticeVisible(true)}
          >
            <span className="auth-login-glyph" aria-hidden="true">↪</span>
            Continuar al acceso seguro
          </button>

          <div className="auth-divider" />

          <p className="auth-redirect-copy">
            Serás redirigido a un proveedor de identidad seguro para autenticarte.
          </p>

          {demoNoticeVisible && (
            <div className="auth-demo-notice" role="status" aria-live="polite">
              <strong>Modo demostración</strong>
              <span>
                El proveedor de identidad todavía no está conectado. Esta acción
                no solicita ni almacena credenciales.
              </span>
            </div>
          )}

          <div className="auth-protection">
            <span className="auth-protection-icon" aria-hidden="true">◆</span>
            <div>
              <strong>Tu información está protegida</strong>
              <span>
                El acceso real se delegará en un proveedor de identidad confiable
                y seguirá los estándares de seguridad definidos para el producto.
              </span>
            </div>
          </div>

          <div className="auth-card-footer">
            <strong>eFactura</strong>
            <span>·</span>
            <span>Facturación electrónica Uruguay</span>
          </div>
        </section>
      </main>

      <div className="auth-waterfront" aria-hidden="true" />

      <footer className="auth-footer">
        <div className="auth-footer-brand">
          <strong>eFactura</strong>
          <span>Facturación electrónica Uruguay</span>
        </div>
        <div className="auth-footer-links" aria-label="Información">
          <span>Ayuda</span>
          <span>Privacidad</span>
          <span>Términos</span>
        </div>
        <div className="auth-footer-country">
          <span aria-hidden="true">🇺🇾</span>
          <strong>UY</strong>
        </div>
      </footer>
    </div>
  );
}
