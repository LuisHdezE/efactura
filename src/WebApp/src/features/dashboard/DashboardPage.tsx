import { Link } from 'react-router-dom';
import './dashboard.css';

type AlertSeverity = 'critical' | 'warning' | 'info';

interface DashboardAlert {
  id: string;
  severity: AlertSeverity;
  eyebrow: string;
  title: string;
  detail: string;
  state: string;
}

const salesTrend = [
  { label: 'Lun', value: 42 },
  { label: 'Mar', value: 58 },
  { label: 'Mié', value: 49 },
  { label: 'Jue', value: 72 },
  { label: 'Vie', value: 84 },
  { label: 'Sáb', value: 66 },
  { label: 'Dom', value: 76 }
];

const alerts: DashboardAlert[] = [
  {
    id: 'alert-cae',
    severity: 'critical',
    eyebrow: 'CAE · Demo',
    title: 'Rango de comprobantes en umbral crítico',
    detail: 'Queda 18% del rango ficticio configurado para la demostración.',
    state: 'Pendiente'
  },
  {
    id: 'alert-stock',
    severity: 'warning',
    eyebrow: 'Inventario · Demo',
    title: '3 productos requieren reposición',
    detail: 'Hay existencias demo por debajo del mínimo operativo ilustrativo.',
    state: 'Revisar'
  },
  {
    id: 'alert-fiscal',
    severity: 'info',
    eyebrow: 'Fiscal · Demo',
    title: 'Próxima obligación en 5 días',
    detail: 'Evento de calendario local de muestra, sin consulta a DGI.',
    state: 'Programado'
  }
];

const lowStock = [
  { name: 'Papel térmico 80 mm', sku: 'INS-0041', current: 4, minimum: 12, level: 'Crítico' },
  { name: 'Cable USB-C 1 m', sku: 'ACC-0184', current: 7, minimum: 15, level: 'Bajo' },
  { name: 'Adaptador 20 W', sku: 'ACC-0210', current: 9, minimum: 14, level: 'Bajo' }
];

const obligations = [
  { day: '24', month: 'SEP', title: 'Anticipo fiscal', detail: 'Evento demo · calendario local' },
  { day: '30', month: 'SEP', title: 'Cierre operativo mensual', detail: 'Recordatorio interno demo' },
  { day: '06', month: 'OCT', title: 'Revisión de CAE', detail: 'Seguimiento preventivo demo' }
];

function severityLabel(severity: AlertSeverity) {
  if (severity === 'critical') return 'Crítica';
  if (severity === 'warning') return 'Atención';
  return 'Informativa';
}

export function DashboardPage() {
  return (
    <div className="dashboard-page">
      <header className="dashboard-heading-row">
        <div>
          <div className="dashboard-kicker">Vista operativa</div>
          <h1>Dashboard</h1>
          <p>Resumen ejecutivo de demostración para ventas, fiscalidad, inventario y flujo financiero.</p>
        </div>
        <div className="dashboard-demo-badge" role="status" aria-label="Datos del dashboard en modo demostración local">
          <span className="dashboard-demo-dot" aria-hidden="true" />
          Demo · Mock local
        </div>
      </header>

      <section className="dashboard-kpis" aria-label="Indicadores principales de demostración">
        <article className="dashboard-kpi-card">
          <div className="dashboard-kpi-topline">
            <span>Ventas del período</span>
            <span className="dashboard-kpi-icon" aria-hidden="true">↗</span>
          </div>
          <strong>$ 1.284.560</strong>
          <div className="dashboard-kpi-foot">
            <span className="is-positive">+8,4% demo</span>
            <span>UYU · 7 días</span>
          </div>
        </article>

        <article className="dashboard-kpi-card">
          <div className="dashboard-kpi-topline">
            <span>Operaciones</span>
            <span className="dashboard-kpi-icon" aria-hidden="true">▥</span>
          </div>
          <strong>184</strong>
          <div className="dashboard-kpi-foot">
            <span>26 promedio/día</span>
            <span>Fixture local</span>
          </div>
        </article>

        <article className="dashboard-kpi-card is-alert">
          <div className="dashboard-kpi-topline">
            <span>Alertas operativas</span>
            <span className="dashboard-kpi-icon" aria-hidden="true">!</span>
          </div>
          <strong>6</strong>
          <div className="dashboard-kpi-foot">
            <span className="is-danger">1 crítica</span>
            <span>3 categorías</span>
          </div>
        </article>

        <article className="dashboard-kpi-card">
          <div className="dashboard-kpi-topline">
            <span>Flujo proyectado</span>
            <span className="dashboard-kpi-icon" aria-hidden="true">≈</span>
          </div>
          <strong>+$ 284.900</strong>
          <div className="dashboard-kpi-foot">
            <span className="is-positive">Positivo demo</span>
            <span>Próx. 30 días</span>
          </div>
        </article>
      </section>

      <div className="dashboard-primary-grid">
        <section className="dashboard-panel dashboard-sales-panel" aria-labelledby="dashboard-sales-title">
          <div className="dashboard-panel-head">
            <div>
              <span className="dashboard-panel-eyebrow">Comercial · Demo</span>
              <h2 id="dashboard-sales-title">Tendencia de ventas</h2>
            </div>
            <span className="dashboard-panel-period">Últimos 7 días</span>
          </div>

          <div className="dashboard-chart-summary">
            <div>
              <span>Total ilustrativo</span>
              <strong>$ 1,28 M UYU</strong>
            </div>
            <p>La gráfica usa fixtures locales. No consume todavía un reporte autoritativo del backend.</p>
          </div>

          <div
            className="dashboard-bars"
            role="img"
            aria-label="Tendencia demo de ventas: crecimiento irregular durante siete días, con máximo el viernes"
          >
            {salesTrend.map((point) => (
              <div key={point.label} className="dashboard-bar-column">
                <div className="dashboard-bar-track" aria-hidden="true">
                  <span className="dashboard-bar" style={{ height: `${point.value}%` }} />
                </div>
                <span>{point.label}</span>
              </div>
            ))}
          </div>

          <div className="dashboard-quick-links" aria-label="Accesos rápidos a vistas implementadas">
            <Link to="/pos" className="dashboard-quick-link">
              <span className="dashboard-quick-icon" aria-hidden="true">🛒</span>
              <span><strong>Punto de Venta</strong><small>Abrir venta demo</small></span>
              <span aria-hidden="true">→</span>
            </Link>
            <Link to="/clientes" className="dashboard-quick-link">
              <span className="dashboard-quick-icon" aria-hidden="true">◉</span>
              <span><strong>Clientes</strong><small>Consultar maestro Party</small></span>
              <span aria-hidden="true">→</span>
            </Link>
          </div>
        </section>

        <section className="dashboard-panel dashboard-fiscal-panel" aria-labelledby="dashboard-fiscal-title">
          <div className="dashboard-panel-head">
            <div>
              <span className="dashboard-panel-eyebrow">Fiscal · Demo</span>
              <h2 id="dashboard-fiscal-title">CAE y operación fiscal</h2>
            </div>
            <span className="dashboard-status-pill is-warning">Atención</span>
          </div>

          <div className="dashboard-cae-meter">
            <div className="dashboard-cae-ring" aria-label="Consumo demo de CAE: 82 por ciento">
              <span>82%</span>
              <small>consumido</small>
            </div>
            <div className="dashboard-cae-copy">
              <span>Rango demo A-2026</span>
              <strong>18% disponible</strong>
              <p>Vencimiento ilustrativo: 18 oct 2026</p>
            </div>
          </div>

          <dl className="dashboard-fiscal-stats">
            <div>
              <dt>Documentos demo hoy</dt>
              <dd>37</dd>
            </div>
            <div>
              <dt>Pendientes conceptuales</dt>
              <dd>2</dd>
            </div>
          </dl>

          <div className="dashboard-authority-note">
            <strong>Sin autoridad DGI en vivo</strong>
            <span>Este bloque no consulta estado de proveedor, certificados ni sincronización real.</span>
          </div>
        </section>
      </div>

      <div className="dashboard-secondary-grid">
        <section className="dashboard-panel" aria-labelledby="dashboard-alerts-title">
          <div className="dashboard-panel-head">
            <div>
              <span className="dashboard-panel-eyebrow">Atención requerida</span>
              <h2 id="dashboard-alerts-title">Alertas operativas</h2>
            </div>
            <span className="dashboard-panel-count">{alerts.length}</span>
          </div>

          <div className="dashboard-alert-list">
            {alerts.map((alert) => (
              <article key={alert.id} className={`dashboard-alert is-${alert.severity}`}>
                <div className="dashboard-alert-marker" aria-hidden="true" />
                <div className="dashboard-alert-copy">
                  <div className="dashboard-alert-meta">
                    <span>{alert.eyebrow}</span>
                    <span>{severityLabel(alert.severity)}</span>
                  </div>
                  <strong>{alert.title}</strong>
                  <p>{alert.detail}</p>
                </div>
                <span className="dashboard-alert-state">{alert.state}</span>
              </article>
            ))}
          </div>
        </section>

        <section className="dashboard-panel" aria-labelledby="dashboard-stock-title">
          <div className="dashboard-panel-head">
            <div>
              <span className="dashboard-panel-eyebrow">Inventario · Demo</span>
              <h2 id="dashboard-stock-title">Reposición sugerida</h2>
            </div>
            <span className="dashboard-panel-count">3</span>
          </div>

          <div className="dashboard-stock-list">
            {lowStock.map((item) => (
              <div key={item.sku} className="dashboard-stock-row">
                <div className="dashboard-stock-product">
                  <span className="dashboard-stock-icon" aria-hidden="true">□</span>
                  <div>
                    <strong>{item.name}</strong>
                    <span>{item.sku}</span>
                  </div>
                </div>
                <div className="dashboard-stock-values">
                  <strong>{item.current}</strong>
                  <span>mín. {item.minimum}</span>
                </div>
                <span className={`dashboard-stock-state ${item.level === 'Crítico' ? 'is-critical' : ''}`}>{item.level}</span>
              </div>
            ))}
          </div>

          <p className="dashboard-disabled-note">La vista de Inventario aún no está implementada, por eso este panel no genera enlaces muertos.</p>
        </section>
      </div>

      <div className="dashboard-bottom-grid">
        <section className="dashboard-panel" aria-labelledby="dashboard-obligations-title">
          <div className="dashboard-panel-head">
            <div>
              <span className="dashboard-panel-eyebrow">Calendario · Demo</span>
              <h2 id="dashboard-obligations-title">Próximos eventos y obligaciones</h2>
            </div>
          </div>
          <div className="dashboard-obligation-list">
            {obligations.map((item) => (
              <div key={`${item.day}-${item.month}-${item.title}`} className="dashboard-obligation-row">
                <div className="dashboard-date-tile" aria-hidden="true">
                  <strong>{item.day}</strong>
                  <span>{item.month}</span>
                </div>
                <div>
                  <strong>{item.title}</strong>
                  <span>{item.detail}</span>
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="dashboard-panel dashboard-finance-panel" aria-labelledby="dashboard-finance-title">
          <div className="dashboard-panel-head">
            <div>
              <span className="dashboard-panel-eyebrow">Finanzas · Demo</span>
              <h2 id="dashboard-finance-title">Flujo proyectado · 30 días</h2>
            </div>
          </div>

          <div className="dashboard-finance-summary">
            <div className="dashboard-finance-row">
              <span>Entradas proyectadas</span>
              <strong className="is-positive">+$ 742.400</strong>
            </div>
            <div className="dashboard-finance-row">
              <span>Salidas proyectadas</span>
              <strong>−$ 457.500</strong>
            </div>
            <div className="dashboard-finance-row is-net">
              <span>Flujo neto demo</span>
              <strong>+$ 284.900</strong>
            </div>
          </div>

          <div className="dashboard-finance-track" aria-hidden="true">
            <span className="dashboard-finance-in" />
            <span className="dashboard-finance-out" />
          </div>
          <p className="dashboard-disabled-note">Proyección puramente ilustrativa. El cálculo autoritativo deberá provenir del backend cuando exista el contrato ejecutable correspondiente.</p>
        </section>
      </div>
    </div>
  );
}
