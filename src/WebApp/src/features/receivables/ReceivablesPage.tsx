import { useMemo, useState } from 'react';
import './receivables-preview.css';

type ReceivableStatus = 'Por vencer' | 'Vencido' | 'Parcial' | 'Cobrado';

type ReceivableHistoryEntry = {
  at: string;
  title: string;
  detail: string;
};

type ReceivablePreview = {
  id: string;
  reference: string;
  customer: string;
  taxId: string;
  issueDate: string;
  dueDate: string;
  currency: 'UYU';
  originalAmount: number;
  openBalance: number;
  status: ReceivableStatus;
  source: string;
  history: ReceivableHistoryEntry[];
};

const receivables: ReceivablePreview[] = [
  {
    id: 'FC-001234',
    reference: 'VENTA mostrador',
    customer: 'Comercial del Plata S.A.',
    taxId: '210123450017',
    issueDate: '02/09/2026',
    dueDate: '01/10/2026',
    currency: 'UYU',
    originalAmount: 123450,
    openBalance: 123450,
    status: 'Por vencer',
    source: 'Venta mostrador',
    history: [
      { at: '02/09/2026 · 10:24', title: 'Documento emitido', detail: 'Origen de demostración registrado para la vista.' },
      { at: '02/09/2026 · 10:24', title: 'Cuenta registrada', detail: 'Saldo ilustrativo disponible para el preview visual.' },
    ],
  },
  {
    id: 'FC-001233',
    reference: 'Pedido #4587',
    customer: 'Distribuidora Norte Ltda.',
    taxId: '214455660013',
    issueDate: '28/08/2026',
    dueDate: '12/09/2026',
    currency: 'UYU',
    originalAmount: 98760,
    openBalance: 98760,
    status: 'Vencido',
    source: 'Pedido #4587',
    history: [
      { at: '28/08/2026 · 15:10', title: 'Documento emitido', detail: 'Cuenta de demostración creada para el preview.' },
      { at: '12/09/2026 · 23:59', title: 'Vencimiento alcanzado', detail: 'El estado visual no representa cálculo servidor.' },
    ],
  },
  {
    id: 'FC-001221',
    reference: 'Remito #1045',
    customer: 'Supermercados Centro',
    taxId: '216677880019',
    issueDate: '15/08/2026',
    dueDate: '15/09/2026',
    currency: 'UYU',
    originalAmount: 250000,
    openBalance: 100000,
    status: 'Parcial',
    source: 'Remito #1045',
    history: [
      { at: '15/08/2026 · 09:42', title: 'Documento emitido', detail: 'Obligación visual por UYU 250.000.' },
      { at: '05/09/2026 · 11:18', title: 'Cobranza parcial de demostración', detail: 'Asignación ilustrativa por UYU 150.000. Sin persistencia.' },
    ],
  },
  {
    id: 'FC-001210',
    reference: 'Venta online',
    customer: 'Hogar & Estilo S.A.',
    taxId: '212233440015',
    issueDate: '10/08/2026',
    dueDate: '10/09/2026',
    currency: 'UYU',
    originalAmount: 89500,
    openBalance: 0,
    status: 'Cobrado',
    source: 'Venta online',
    history: [
      { at: '10/08/2026 · 14:05', title: 'Documento emitido', detail: 'Cuenta visual creada.' },
      { at: '07/09/2026 · 16:32', title: 'Cobranza completa de demostración', detail: 'Saldo ilustrativo llevado a cero solo dentro del fixture.' },
    ],
  },
  {
    id: 'FC-001208',
    reference: 'Pedido #4501',
    customer: 'Constructora Sur S.A.',
    taxId: '219988770014',
    issueDate: '08/08/2026',
    dueDate: '22/08/2026',
    currency: 'UYU',
    originalAmount: 320000,
    openBalance: 320000,
    status: 'Vencido',
    source: 'Pedido #4501',
    history: [
      { at: '08/08/2026 · 08:55', title: 'Documento emitido', detail: 'Cuenta visual sin cobranza asociada.' },
      { at: '22/08/2026 · 23:59', title: 'Vencimiento alcanzado', detail: 'Indicador visual de aging pendiente de API.' },
    ],
  },
  {
    id: 'NC-000045',
    reference: 'Ajuste de precio',
    customer: 'Comercial del Plata S.A.',
    taxId: '210123450017',
    issueDate: '05/08/2026',
    dueDate: '05/08/2026',
    currency: 'UYU',
    originalAmount: -15000,
    openBalance: 0,
    status: 'Cobrado',
    source: 'Ajuste de precio',
    history: [
      { at: '05/08/2026 · 13:20', title: 'Ajuste ilustrativo', detail: 'La representación no autoriza ajustes reales desde el frontend.' },
    ],
  },
  {
    id: 'FC-001198',
    reference: 'Pedido #4390',
    customer: 'AgroCampo S.R.L.',
    taxId: '215566770012',
    issueDate: '01/08/2026',
    dueDate: '01/09/2026',
    currency: 'UYU',
    originalAmount: 76300,
    openBalance: 76300,
    status: 'Vencido',
    source: 'Pedido #4390',
    history: [
      { at: '01/08/2026 · 12:08', title: 'Documento emitido', detail: 'Registro de demostración.' },
      { at: '01/09/2026 · 23:59', title: 'Vencimiento alcanzado', detail: 'Aging visual pendiente de proyección autoritativa.' },
    ],
  },
  {
    id: 'FC-001185',
    reference: 'Remito #998',
    customer: 'Farmacia Integral',
    taxId: '211144330018',
    issueDate: '25/07/2026',
    dueDate: '25/08/2026',
    currency: 'UYU',
    originalAmount: 54900,
    openBalance: 27450,
    status: 'Parcial',
    source: 'Remito #998',
    history: [
      { at: '25/07/2026 · 10:12', title: 'Documento emitido', detail: 'Cuenta de demostración.' },
      { at: '18/08/2026 · 17:04', title: 'Cobranza parcial de demostración', detail: 'Asignación ilustrativa por UYU 27.450.' },
    ],
  },
];

const demoSummary = {
  openBalance: 1248320,
  current: 720450,
  overdue: 527870,
  customersWithDebt: 24,
  documents: 94,
};

const uyu = new Intl.NumberFormat('es-UY', {
  style: 'currency',
  currency: 'UYU',
  maximumFractionDigits: 0,
});

function formatUyu(value: number) {
  return uyu.format(value).replace('UYU', '').trim();
}

function statusClass(status: ReceivableStatus) {
  return status.toLowerCase().replace(' ', '-');
}

export function ReceivablesPage() {
  const [selectedId, setSelectedId] = useState(receivables[0].id);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<'Todos' | ReceivableStatus>('Todos');

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return receivables.filter((receivable) => {
      const matchesTerm = !term || [
        receivable.id,
        receivable.reference,
        receivable.customer,
        receivable.taxId,
        receivable.status,
      ].some((value) => value.toLowerCase().includes(term));
      const matchesStatus = status === 'Todos' || receivable.status === status;
      return matchesTerm && matchesStatus;
    });
  }, [search, status]);

  const selected = receivables.find((item) => item.id === selectedId) ?? filtered[0] ?? receivables[0];

  const clearFilters = () => {
    setSearch('');
    setStatus('Todos');
  };

  const selectReceivable = (id: string) => setSelectedId(id);

  return (
    <div className="receivables-preview-page">
      <header className="receivables-heading">
        <div>
          <div className="receivables-breadcrumb">Finanzas <span aria-hidden="true">›</span> Cuentas por cobrar</div>
          <h1>Cuentas por cobrar</h1>
          <p>Gestiona visualmente saldos, vencimientos y cobranzas sin presentar operaciones financieras como ejecutables.</p>
        </div>
        <div className="receivables-heading-actions">
          <span className="receivables-preview-badge">Preview UI · API pendiente</span>
          <button
            type="button"
            className="receivables-primary"
            disabled
            title="Se habilitará cuando API-COL-001 esté disponible y reconciliada."
          >
            Registrar cobro <span>DEMO</span>
          </button>
        </div>
      </header>

      <div className="receivables-demo-note" role="note">
        <strong>Datos de demostración.</strong> Los importes, estados y aging de esta vista no son saldos reales ni cálculos del servidor. Cobros, ajustes y reversas permanecen bloqueados.
      </div>

      <section className="receivables-summary" aria-label="Resumen visual de cuentas por cobrar">
        <article className="receivables-summary-card is-open">
          <div className="receivables-summary-icon" aria-hidden="true">◈</div>
          <div><span>Saldo abierto total</span><strong>{formatUyu(demoSummary.openBalance)} <small>UYU</small></strong><em>Muestra visual consolidada</em></div>
        </article>
        <article className="receivables-summary-card is-current">
          <div className="receivables-summary-icon" aria-hidden="true">▣</div>
          <div><span>Por vencer</span><strong>{formatUyu(demoSummary.current)} <small>UYU</small></strong><em>58 documentos · demo</em></div>
        </article>
        <article className="receivables-summary-card is-overdue">
          <div className="receivables-summary-icon" aria-hidden="true">!</div>
          <div><span>Vencido</span><strong>{formatUyu(demoSummary.overdue)} <small>UYU</small></strong><em>36 documentos · demo</em></div>
        </article>
        <article className="receivables-summary-card is-customers">
          <div className="receivables-summary-icon" aria-hidden="true">◎</div>
          <div><span>Clientes con deuda</span><strong>{demoSummary.customersWithDebt}</strong><em>de 86 clientes · demo</em></div>
        </article>
      </section>

      <div className="receivables-workspace">
        <section className="receivables-panel receivables-list-panel" aria-label="Cuentas por cobrar de demostración">
          <div className="receivables-filters">
            <label className="receivables-search">
              <span aria-hidden="true">⌕</span>
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Buscar por cliente o documento…"
              />
            </label>
            <label className="receivables-filter-field">
              <span>Estado</span>
              <select value={status} onChange={(event) => setStatus(event.target.value as 'Todos' | ReceivableStatus)}>
                <option>Todos</option>
                <option>Por vencer</option>
                <option>Vencido</option>
                <option>Parcial</option>
                <option>Cobrado</option>
              </select>
            </label>
            <label className="receivables-filter-field">
              <span>Moneda</span>
              <select value="Todas" disabled title="El fixture gobernado usa UYU solamente.">
                <option>Todas</option>
              </select>
            </label>
            <button type="button" className="receivables-clear" onClick={clearFilters}>Limpiar</button>
          </div>

          <div className="receivables-table-wrap">
            <table className="receivables-table">
              <thead>
                <tr>
                  <th>Documento / Referencia</th>
                  <th>Cliente</th>
                  <th>Emisión</th>
                  <th>Vencimiento</th>
                  <th>Moneda</th>
                  <th>Monto original</th>
                  <th>Saldo abierto</th>
                  <th>Estado</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((receivable) => (
                  <tr
                    key={receivable.id}
                    className={receivable.id === selected.id ? 'is-selected' : undefined}
                    tabIndex={0}
                    onClick={() => selectReceivable(receivable.id)}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault();
                        selectReceivable(receivable.id);
                      }
                    }}
                  >
                    <td><strong>{receivable.id}</strong><span>{receivable.reference}</span></td>
                    <td>{receivable.customer}</td>
                    <td>{receivable.issueDate}</td>
                    <td className={receivable.status === 'Vencido' ? 'is-overdue-date' : undefined}>{receivable.dueDate}</td>
                    <td>{receivable.currency}</td>
                    <td>{formatUyu(receivable.originalAmount)}</td>
                    <td><strong>{formatUyu(receivable.openBalance)}</strong></td>
                    <td><span className={`receivables-status is-${statusClass(receivable.status)}`}>{receivable.status}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="receivables-mobile-list">
            {filtered.map((receivable) => (
              <button
                type="button"
                key={receivable.id}
                className={`receivables-mobile-card ${receivable.id === selected.id ? 'is-selected' : ''}`}
                onClick={() => selectReceivable(receivable.id)}
              >
                <div className="receivables-mobile-head">
                  <span><strong>{receivable.id}</strong><small>{receivable.customer}</small></span>
                  <span className={`receivables-status is-${statusClass(receivable.status)}`}>{receivable.status}</span>
                </div>
                <div className="receivables-mobile-values">
                  <span>Vence <strong>{receivable.dueDate}</strong></span>
                  <span>Saldo <strong>{formatUyu(receivable.openBalance)} UYU</strong></span>
                </div>
              </button>
            ))}
          </div>

          <footer className="receivables-list-footer">
            <span>Mostrando {filtered.length} de {demoSummary.documents} documentos de demostración</span>
            <div className="receivables-pagination" aria-label="Paginación ilustrativa">
              <button type="button" disabled>‹</button><button type="button" className="is-current">1</button><button type="button" disabled>2</button><button type="button" disabled>3</button><button type="button" disabled>›</button>
            </div>
          </footer>
        </section>

        <aside className="receivables-panel receivables-detail" aria-label={`Detalle de ${selected.id}`}>
          <div className="receivables-detail-head">
            <div>
              <span>Detalle de cuenta</span>
              <h2>{selected.id}</h2>
              <p>{selected.source}</p>
            </div>
            <span className={`receivables-status is-${statusClass(selected.status)}`}>{selected.status}</span>
          </div>

          <dl className="receivables-detail-grid">
            <div><dt>Cliente</dt><dd>{selected.customer}</dd></div>
            <div><dt>RUT</dt><dd>{selected.taxId}</dd></div>
            <div><dt>Emisión</dt><dd>{selected.issueDate}</dd></div>
            <div><dt>Vencimiento</dt><dd>{selected.dueDate}</dd></div>
            <div><dt>Moneda</dt><dd>{selected.currency}</dd></div>
            <div><dt>Monto original</dt><dd>{formatUyu(selected.originalAmount)}</dd></div>
            <div><dt>Saldo abierto</dt><dd><strong>{formatUyu(selected.openBalance)}</strong></dd></div>
          </dl>

          <div className="receivables-tabs" aria-label="Secciones de detalle">
            <span className="is-active">Historial</span><span>Ajustes</span><span>Cobranzas vinculadas</span>
          </div>

          <div className="receivables-history">
            {selected.history.map((entry) => (
              <div className="receivables-history-entry" key={`${entry.at}-${entry.title}`}>
                <span className="receivables-history-dot" aria-hidden="true" />
                <div><time>{entry.at}</time><strong>{entry.title}</strong><p>{entry.detail}</p></div>
              </div>
            ))}
          </div>

          <div className="receivables-info-box">
            <strong>{selected.openBalance > 0 ? 'Sin operación ejecutable' : 'Cuenta visual sin saldo abierto'}</strong>
            <span>La historia es demostrativa y no se puede ajustar ni revertir desde este preview.</span>
          </div>

          <div className="receivables-detail-actions">
            <button type="button" disabled>Ajustar cuenta</button>
            <button type="button" disabled>Revertir cobranza</button>
          </div>
        </aside>

        <aside className="receivables-panel receivables-collection" aria-label="Registrar cobro de demostración">
          <div className="receivables-collection-head">
            <div><span>Cobranza</span><h2>Registrar cobro</h2></div>
            <span className="receivables-demo-pill">DEMO</span>
          </div>

          <div className="receivables-collection-note">
            <strong>Vista de demostración</strong>
            <span>La funcionalidad de cobranzas aún no está habilitada. Ningún control modifica saldos.</span>
          </div>

          <div className="receivables-form-stack">
            <label><span>Cliente</span><select disabled value={selected.customer}><option>{selected.customer}</option></select></label>
            <label><span>Importe cobrado</span><div className="receivables-money-input"><input disabled value={formatUyu(selected.openBalance)} readOnly /><b>UYU</b></div></label>
            <label><span>Medio de pago</span><select disabled value=""><option value="">Seleccionar…</option></select></label>
            <label><span>Referencia externa</span><input disabled placeholder="Ej. Nº de operación, cheque, etc." /></label>
          </div>

          <div className="receivables-allocation-box">
            <span className="receivables-section-label">Asignar a cuentas</span>
            <div className="receivables-allocation-line">
              <span className="receivables-checkbox" aria-hidden="true">✓</span>
              <div><strong>{selected.id}</strong><small>Vence {selected.dueDate}</small></div>
              <b>{formatUyu(selected.openBalance)}</b>
            </div>
            <button type="button" disabled>＋ Agregar otra cuenta</button>
          </div>

          <div className="receivables-policy-box">
            <span>Excedente / Resultado de política</span>
            <strong>{formatUyu(0)}</strong>
            <p>Si el importe supera el saldo asignado, el resultado dependerá de la política que devuelva el backend. Este preview no inventa crédito, anticipo ni truncamiento.</p>
          </div>

          <button type="button" className="receivables-submit" disabled>Registrar cobro <span>DEMO</span></button>
          <button type="button" className="receivables-cancel" disabled>Cancelar</button>
        </aside>
      </div>

      <div className="receivables-preview-footer" role="note">
        <strong>Vista preliminar</strong>
        <span>Representación visual gobernada de Cuentas por cobrar. `API-AR-001..004` y `API-COL-001..003` siguen sin superficie HTTP ejecutable.</span>
        <code>UI-RECEIVABLE-001</code>
      </div>
    </div>
  );
}
