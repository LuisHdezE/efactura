import { useEffect, useState } from 'react';
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

function WaterfrontArt() {
  return (
    <svg
      className="auth-waterfront-art"
      viewBox="0 0 1600 360"
      preserveAspectRatio="none"
      aria-hidden="true"
      focusable="false"
    >
      <path className="auth-water" d="M0 296C225 285 428 309 632 293C843 276 1069 298 1281 286C1403 279 1500 280 1600 274V360H0Z" />
      <path className="auth-shore" d="M0 291C234 278 457 297 682 284C896 271 1119 287 1329 276C1434 271 1517 270 1600 267V290C1519 295 1429 297 1329 301C1114 309 895 299 684 312C449 326 222 308 0 320Z" />
      <g className="auth-city">
        <rect x="22" y="245" width="42" height="43" rx="3" />
        <rect x="73" y="226" width="30" height="62" rx="3" />
        <rect x="112" y="240" width="53" height="48" rx="3" />
        <rect x="178" y="212" width="42" height="76" rx="3" />
        <rect x="231" y="233" width="54" height="55" rx="3" />
        <rect x="298" y="219" width="38" height="69" rx="3" />
        <rect x="349" y="243" width="52" height="45" rx="3" />
        <rect x="416" y="225" width="32" height="63" rx="3" />
        <rect x="463" y="237" width="58" height="51" rx="3" />
        <rect x="537" y="208" width="37" height="80" rx="3" />
        <rect x="589" y="231" width="49" height="57" rx="3" />
        <rect x="651" y="222" width="29" height="66" rx="3" />
        <rect x="692" y="240" width="44" height="48" rx="3" />
        <rect x="1260" y="232" width="44" height="56" rx="3" />
        <rect x="1317" y="215" width="37" height="73" rx="3" />
        <rect x="1367" y="237" width="56" height="51" rx="3" />
        <rect x="1438" y="222" width="31" height="66" rx="3" />
        <rect x="1482" y="240" width="48" height="48" rx="3" />
        <rect x="1542" y="228" width="40" height="60" rx="3" />
      </g>
      <g className="auth-palacio">
        <rect x="1022" y="177" width="73" height="111" rx="3" />
        <rect x="1031" y="153" width="55" height="28" rx="4" />
        <rect x="1040" y="124" width="37" height="32" rx="4" />
        <rect x="1048" y="88" width="21" height="40" rx="5" />
        <path d="M1058 54L1068 89H1048Z" />
        <rect x="1041" y="191" width="10" height="16" rx="2" />
        <rect x="1056" y="191" width="10" height="16" rx="2" />
        <rect x="1071" y="191" width="10" height="16" rx="2" />
        <rect x="1041" y="217" width="10" height="16" rx="2" />
        <rect x="1056" y="217" width="10" height="16" rx="2" />
        <rect x="1071" y="217" width="10" height="16" rx="2" />
      </g>
      <g className="auth-palms">
        <path d="M1149 287V223" />
        <path d="M1149 226C1134 213 1120 210 1107 214M1149 226C1162 210 1178 207 1191 211M1149 226C1140 207 1142 194 1149 184M1149 226C1158 208 1168 197 1178 194" />
        <path d="M1211 287V235" />
        <path d="M1211 237C1198 226 1187 223 1175 226M1211 237C1223 224 1235 221 1247 225M1211 237C1204 222 1206 210 1212 203M1211 237C1219 223 1228 214 1237 211" />
      </g>
      <g className="auth-window-lights">
        {Array.from({ length: 14 }).map((_, index) => (
          <circle key={index} cx={90 + index * 101} cy={263 - (index % 3) * 8} r="2.2" />
        ))}
      </g>
    </svg>
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

        <blockquote className="auth-quote">
          “Tecnología al servicio
          <br />
          de un Uruguay más simple”
        </blockquote>
      </main>

      <div className="auth-waterfront">
        <WaterfrontArt />
      </div>

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
