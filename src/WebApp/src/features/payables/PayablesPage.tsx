import { useMemo, useState } from 'react';
import './payables-preview.css';

type PayableStatus = 'Pendiente' | 'Vencida' | 'Parcial' | 'Pagada';
type DetailTab = 'Información' | 'Historial';

type PayableHistoryEntry = {
  at: string;
  title: string;
  detail: string;
};

type PayablePreview = {
  id: string;
  reference: string;
  supplier: string;
  taxId: string;
  issueDate: string;
  dueDate: string;
  currency: 'UYU';
  originalAmount: number;
  openBalance: number;
  status: PayableStatus;
  dueBucket: 'overdue' | 'next30' | 'later' | 'settled';
  history: PayableHistoryEntry[];
};

const payables: PayablePreview[] = [
  {
    id: 'A 0012-000345',
    reference: 'Compra OC-2026-0418',
    supplier: 'Distribuidora Sur S.A.',
    taxId: '214567890012',
    issueDate: '02/09/2026',
    dueDate: '02/10/2026',
    currency: 'UYU',
    originalAmount: 124560,
    openBalance: 124560,
    status: 'Pendiente',
    dueBucket: 'next30',
    history: [
      { at: '02/09/2026 · 09:15', title: 'Obligación de demostración registrada', detail: 'Origen visual asociado a una compra de demostración. Sin persistencia financiera.' },
      { at: '02/09/2026 · 09:16', title: 'Saldo ilustrativo abierto', detail: 'El saldo pertenece únicamente al fixture gobernado del preview.' },
    ],
  },
  {
    id: 'B 0001-000789',
    reference: 'Servicios agosto',
    supplier: 'Tecnología Global Ltda.',
    taxId: '216304520017',
    issueDate: '28/08/2026',
    dueDate: '12/09/2026',
    currency: 'UYU',
    originalAmount: 98730,
    openBalance: 98730,
    status: 'Vencida',
    dueBucket: 'overdue',
    history: [
      { at: '28/08/2026 · 14:42', title: 'Factura de demostración registrada', detail: 'La vista no representa un CFE recibido autoritativo.' },
      { at: '12/09/2026 · 23:59', title: 'Vencimiento ilustrativo alcanzado', detail: 'El aging es local y existe únicamente para la revisión visual.' },
    ],
  },
  {
    id: 'A 0003-001234',
    reference: 'Flete y logística',
    supplier: 'Servicios Logísticos UY',
    taxId: '217654320018',
    issueDate: '15/09/2026',
    dueDate: '30/09/2026',
    currency: 'UYU',
    originalAmount: 256000,
    openBalance: 256000,
    status: 'Pendiente',
    dueBucket: 'next30',
    history: [
      { at: '15/09/2026 · 11:08', title: 'Obligación de demostración registrada', detail: 'Asociación visual a proveedor y vencimiento.' },
    ],
  },
  {
    id: 'B 0002-000456',
    reference: 'Insumos administrativos',
    supplier: 'Oficina y Papel S.A.',
    taxId: '212309870014',
    issueDate: '10/08/2026',
    dueDate: '10/09/2026',
    currency: 'UYU',
    originalAmount: 52300,
    openBalance: 52300,
    status: 'Vencida',
    dueBucket: 'overdue',
    history: [
      { at: '10/08/2026 · 16:31', title: 'Obligación de demostración registrada', detail: 'Sin efecto de caja o banco.' },
      { at: '10/09/2026 · 23:59', title: 'Vencimiento ilustrativo alcanzado', detail: 'No proviene de API-AP-003.' },
    ],
  },
  {
    id: 'A 0001-000987',
    reference: 'Suministro eléctrico',
    supplier: 'Energía del Plata',
    taxId: '210998760015',
    issueDate: '05/09/2026',
    dueDate: '20/09/2026',
    currency: 'UYU',
    originalAmount: 178900,
    openBalance: 178900,
    status: 'Vencida',
    dueBucket: 'overdue',
    history: [
      { at: '05/09/2026 · 08:47', title: 'Obligación de demostración registrada', detail: 'Importe y estado pertenecen al fixture local.' },
    ],
  },
  {
    id: 'B 0001-000321',
    reference: 'Limpieza mensual',
    supplier: 'Limpieza Total S.R.L.',
    taxId: '219912340011',
    issueDate: '12/09/2026',
    dueDate: '12/10/2026',
    currency: 'UYU',
    originalAmount: 42150,
    openBalance: 42150,
    status: 'Pendiente',
    dueBucket: 'next30',
    history: [
      { at: '12/09/2026 · 13:12', title: 'Obligación de demostración registrada', detail: 'Vencimiento ilustrativo dentro de próximos 30 días.' },
    ],
  },
  {
    id: 'A 0004-000654',
    reference: 'Equipamiento local',
    supplier: 'Equipos y Soluciones',
    taxId: '214400330016',
    issueDate: '18/09/2026',
    dueDate: '18/10/2026',
    currency: 'UYU',
    originalAmount: 310450,
    openBalance: 155225,
    status: 'Parcial',
    dueBucket: 'next30',
    history: [
      { at: '18/09/2026 · 10:04', title: 'Obligación de demostración registrada', detail: 'Saldo original ilustrativo UYU 310.450.' },
      { at: '20/09/2026 · 15:20', title: 'Pago parcial de demostración', detail: 'Asignación visual UYU 155.225. No existe movimiento de caja/banco.' },
    ],
  },
  {
    id: 'A 0001-000111',
    reference: 'Servicio de vigilancia',
    supplier: 'Seguridad Total S.A.',
    taxId: '211120090013',
    issueDate: '20/08/2026',
    dueDate: '18/09/2026',
    currency: 'UYU',
    originalAmount: 89600,
    openBalance: 0,
    status: 'Pagada',
    dueBucket: 'settled',
    history: [
      { at: '20/08/2026 · 12:30', title: 'Obligación de demostración registrada', detail: 'Documento de fixture visual.' },
      { at: '17/09/2026 · 09:25', title: 'Pago completo de demostración', detail: 'El estado Pagada no representa una transacción financiera real.' },
    ],
  },
];

const uyu = new Intl.NumberFormat('es-UY', {
  style: 'currency',
  currency: 'UYU',
  maximumFractionDigits: 0,
});

function formatUyu(value: number) {
  return uyu.format(value).replace('UYU', '').trim();
}

function statusClass(status: PayableStatus) {
  return status.toLowerCase().replace(' ', '-');
}

export function PayablesPage() {
  const [selectedId, setSelectedId] = useState(payables[0].id);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<'Todos' | PayableStatus>('Todos');
  const [due, setDue] = useState<'Todos' | 'Vencidas' | 'Próximos 30 días'>('Todos');
  const [detailTab, setDetailTab] = useState<DetailTab>('Información');

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();

    return payables.filter((payable) => {
      const matchesTerm = !term || [
        payable.id,
        payable.reference,
        payable.supplier,
        payable.taxId,
        payable.status,
      ].some((value) => value.toLowerCase().includes(term));
      const matchesStatus = status === 'Todos' || payable.status === status;
      const matchesDue = due === 'Todos'
        || (due === 'Vencidas' && payable.dueBucket === 'overdue')
        || (due === 'Próximos 30 días' && payable.dueBucket === 'next30');

      return matchesTerm && matchesStatus && matchesDue;
    });
  }, [search, status, due]);

  const selected = payables.find((item) => item.id === selectedId) ?? filtered[0] ?? payables[0];

  const summary = useMemo(() => {
    const open = payables.filter((item) => item.status !== 'Pagada');
    return {
      totalOpen: open.reduce((total, item) => total + item.openBalance, 0),
      pendingCount: open.length,
      overdueCount: payables.filter((item) => item.dueBucket === 'overdue' && item.openBalance > 0).length,
      next30: payables.filter((item) => item.dueBucket === 'next30').reduce((total, item) => total + item.openBalance, 0),
    };
  }, []);

  const clearFilters = () => {
    setSearch('');
    setStatus('Todos');
    setDue('Todos');
  };

  return (
    <div className="payables-preview-page">
      <header className="payables-heading">
        <div>
          <div className="payables-breadcrumb">Finanzas <span aria-hidden="true">›</span> Cuentas por pagar</div>
          <h1>Cuentas por pagar</h1>
          <p>Gestiona visualmente obligaciones con proveedores, vencimientos y asignaciones sin presentar operaciones financieras como ejecutables.</p>
        </div>
        <div className="payables-heading-actions">
          <span className="payables-preview-badge">Preview UI · API pendiente</span>
          <button type="button" className="payables-primary" disabled title="El alta autoritativa de obligaciones no está disponible en este preview.">
            Registrar factura <span>DEMO</span>
          </button>
        </div>
      </header>

      <div className="payables-demo-note" role="note">
        <strong>Datos de demostración.</strong> Importes, estados, aging y asignaciones son fixtures locales. Pagos, ajustes, reversas y efectos de caja/banco permanecen bloqueados.
      </div>

      <section className="payables-summary" aria-label="Resumen visual de cuentas por pagar">
        <article className="payables-summary-card is-open">
          <div className="payables-summary-icon" aria-hidden="true">◆</div>
          <div><span>Total por pagar</span><strong>{formatUyu(summary.totalOpen)} <small>UYU</small></strong><em>Muestra visual consolidada</em></div>
        </article>
        <article className="payables-summary-card is-pending">
          <div className="payables-summary-icon" aria-hidden="true">▤</div>
          <div><span>Facturas pendientes</span><strong>{summary.pendingCount}</strong><em>Obligaciones con saldo · demo</em></div>
        </article>
        <article className="payables-summary-card is-overdue">
          <div className="payables-summary-icon" aria-hidden="true">!</div>
          <div><span>Vencidas</span><strong>{summary.overdueCount}</strong><em>Aging local de demostración</em></div>
        </article>
        <article className="payables-summary-card is-next">
          <div className="payables-summary-icon" aria-hidden="true">▣</div>
          <div><span>Próximos 30 días</span><strong>{formatUyu(summary.next30)} <small>UYU</small></strong><em>Fixture local</em></div>
        </article>
      </section>

      <div className="payables-workspace">
        <section className="payables-panel payables-list-panel" aria-label="Cuentas por pagar de demostración">
          <div className="payables-filters">
            <label className="payables-search">
              <span aria-hidden="true">⌕</span>
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar proveedor, factura o referencia…" />
            </label>
            <label className="payables-filter-field">
              <span>Estado</span>
              <select value={status} onChange={(event) => setStatus(event.target.value as 'Todos' | PayableStatus)}>
                <option>Todos</option>
                <option>Pendiente</option>
                <option>Vencida</option>
                <option>Parcial</option>
                <option>Pagada</option>
              </select>
            </label>
            <label className="payables-filter-field">
              <span>Vencimiento</span>
              <select value={due} onChange={(event) => setDue(event.target.value as 'Todos' | 'Vencidas' | 'Próximos 30 días')}>
                <option>Todos</option>
                <option>Vencidas</option>
                <option>Próximos 30 días</option>
              </select>
            </label>
            <button type="button" className="payables-clear" onClick={clearFilters}>Limpiar</button>
          </div>

          <div className="payables-table-wrap">
            <table className="payables-table">
              <thead>
                <tr>
                  <th>Nº factura</th>
                  <th>Proveedor</th>
                  <th>Fecha</th>
                  <th>Vencimiento</th>
                  <th>Importe</th>
                  <th>Saldo</th>
                  <th>Estado</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((payable) => (
                  <tr
                    key={payable.id}
                    className={payable.id === selected.id ? 'is-selected' : undefined}
                    tabIndex={0}
                    onClick={() => setSelectedId(payable.id)}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault();
                        setSelectedId(payable.id);
                      }
                    }}
                  >
                    <td><strong>{payable.id}</strong><span>{payable.reference}</span></td>
                    <td>{payable.supplier}</td>
                    <td>{payable.issueDate}</td>
                    <td className={payable.status === 'Vencida' ? 'is-overdue-date' : undefined}>{payable.dueDate}</td>
                    <td><strong>{formatUyu(payable.originalAmount)}</strong></td>
                    <td>{formatUyu(payable.openBalance)}</td>
                    <td><span className={`payables-status is-${statusClass(payable.status)}`}>{payable.status}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="payables-mobile-list">
            {filtered.map((payable) => (
              <button
                type="button"
                key={payable.id}
                className={`payables-mobile-card ${payable.id === selected.id ? 'is-selected' : ''}`}
                onClick={() => setSelectedId(payable.id)}
              >
                <div className="payables-mobile-head">
                  <span><strong>{payable.supplier}</strong><small>{payable.id}</small></span>
                  <strong>{formatUyu(payable.openBalance)}</strong>
                </div>
                <div className="payables-mobile-values">
                  <span>Vence <strong>{payable.dueDate}</strong></span>
                  <span className={`payables-status is-${statusClass(payable.status)}`}>{payable.status}</span>
                </div>
              </button>
            ))}
          </div>

          <div className="payables-list-footer">
            <span>Mostrando {filtered.length} de {payables.length} obligaciones de demostración</span>
            <div className="payables-pagination" aria-label="Paginación visual">
              <button type="button" disabled>‹</button>
              <button type="button" className="is-current" disabled>1</button>
              <button type="button" disabled>2</button>
              <button type="button" disabled>3</button>
              <button type="button" disabled>›</button>
            </div>
          </div>
        </section>

        <aside className="payables-panel payables-detail" aria-label="Detalle de obligación seleccionada">
          <div className="payables-detail-head">
            <div>
              <span>Detalle de factura</span>
              <h2>{selected.id}</h2>
              <p>{selected.supplier}</p>
            </div>
            <span className={`payables-status is-${statusClass(selected.status)}`}>{selected.status}</span>
          </div>

          <div className="payables-tabs" role="tablist" aria-label="Detalle de cuenta por pagar">
            {(['Información', 'Historial'] as DetailTab[]).map((tab) => (
              <button
                key={tab}
                type="button"
                role="tab"
                aria-selected={detailTab === tab}
                className={detailTab === tab ? 'is-active' : undefined}
                onClick={() => setDetailTab(tab)}
              >
                {tab}
              </button>
            ))}
          </div>

          {detailTab === 'Información' ? (
            <>
              <dl className="payables-detail-grid">
                <div><dt>RUT proveedor</dt><dd>{selected.taxId}</dd></div>
                <div><dt>Fecha de emisión</dt><dd>{selected.issueDate}</dd></div>
                <div><dt>Fecha de vencimiento</dt><dd>{selected.dueDate}</dd></div>
                <div><dt>Importe total</dt><dd>{formatUyu(selected.originalAmount)} UYU</dd></div>
                <div><dt>Saldo pendiente</dt><dd>{formatUyu(selected.openBalance)} UYU</dd></div>
                <div><dt>Referencia</dt><dd>{selected.reference}</dd></div>
              </dl>
              <div className="payables-info-box">
                <strong>Obligación de demostración</strong>
                <span>No representa un saldo autoritativo ni evidencia fiscal recibida. El backend de cuentas por pagar continúa pendiente.</span>
              </div>
            </>
          ) : (
            <div className="payables-history">
              {selected.history.map((entry) => (
                <div className="payables-history-entry" key={`${entry.at}-${entry.title}`}>
                  <span className="payables-history-dot" aria-hidden="true" />
                  <div>
                    <time>{entry.at}</time>
                    <strong>{entry.title}</strong>
                    <p>{entry.detail}</p>
                  </div>
                </div>
              ))}
            </div>
          )}

          <div className="payables-payment-preview">
            <div className="payables-payment-title">
              <div><span>Proveedor</span><strong>{selected.supplier}</strong></div>
              <span className="payables-demo-pill">DEMO</span>
            </div>
            <label>
              <span>Importe de pago</span>
              <div className="payables-money-input"><input value={selected.openBalance ? formatUyu(selected.openBalance) : '0'} disabled readOnly /><b>UYU</b></div>
            </label>
            <label>
              <span>Medio de pago</span>
              <select disabled><option>Seleccionar…</option></select>
            </label>
            <label>
              <span>Referencia externa</span>
              <input disabled placeholder="Nº de operación, cheque, etc." />
            </label>
            <div className="payables-policy-box">
              <strong>Sin política de excedente inventada</strong>
              <span>Un pago superior al saldo requiere una política explícita para el importe no aplicado. Este preview no lo calcula ni lo trunca.</span>
            </div>
            <button type="button" className="payables-submit" disabled title="Requiere API-PAY-001 y reconciliación financiera.">Registrar pago <span>DEMO</span></button>
          </div>

          <div className="payables-detail-actions">
            <button type="button" disabled>Ver documento</button>
            <button type="button" disabled>Más acciones</button>
          </div>
        </aside>
      </div>

      <div className="payables-bottom-note" role="note">
        <strong>Vista preliminar.</strong>
        <span>La gestión visual de cuentas por pagar está disponible. Pagos, ajustes, reversas y conciliación se habilitarán únicamente cuando existan contratos HTTP implementados y reconciliados.</span>
      </div>
    </div>
  );
}
