import { useMemo, useState } from 'react';
import './cash-preview.css';

type MovementKind = 'Ingreso' | 'Egreso' | 'Ajuste';
type MovementStatus = 'Confirmado' | 'Pendiente';
type CashTab = 'Movimientos' | 'Conciliación' | 'Turno';

type CashMovement = {
  id: string;
  occurredAt: string;
  concept: string;
  kind: MovementKind;
  medium: string;
  reference: string;
  amount: number;
  status: MovementStatus;
  actor: string;
  note: string;
};

const movements: CashMovement[] = [
  { id: 'MOV-DEMO-001', occurredAt: '21/09/2026 · 10:24', concept: 'Cobro factura A 0003-001234', kind: 'Ingreso', medium: 'Transferencia', reference: 'FAC-1234', amount: 256000, status: 'Confirmado', actor: 'Caja demo', note: 'Movimiento ilustrativo. No proviene de una cobranza autoritativa.' },
  { id: 'MOV-DEMO-002', occurredAt: '20/09/2026 · 16:48', concept: 'Pago proveedor Tecnología Global', kind: 'Egreso', medium: 'Transferencia', reference: 'PAG-5678', amount: 98730, status: 'Confirmado', actor: 'Tesorería demo', note: 'Egreso local de demostración. No existe efecto financiero real.' },
  { id: 'MOV-DEMO-003', occurredAt: '19/09/2026 · 14:13', concept: 'Cobro contado · Punto de Venta', kind: 'Ingreso', medium: 'Efectivo', reference: 'PV-0456', amount: 123500, status: 'Confirmado', actor: 'Caja demo', note: 'Fixture visual asociado al turno de caja de demostración.' },
  { id: 'MOV-DEMO-004', occurredAt: '18/09/2026 · 11:02', concept: 'Retiro operativo ilustrativo', kind: 'Egreso', medium: 'Efectivo', reference: 'RET-0012', amount: 50000, status: 'Confirmado', actor: 'Caja demo', note: 'No representa un retiro persistido ni autorizado.' },
  { id: 'MOV-DEMO-005', occurredAt: '17/09/2026 · 09:51', concept: 'Pago proveedor Distribuidora Sur', kind: 'Egreso', medium: 'Transferencia', reference: 'PAG-9012', amount: 124560, status: 'Confirmado', actor: 'Tesorería demo', note: 'Sin vínculo ejecutable con cuentas por pagar.' },
  { id: 'MOV-DEMO-006', occurredAt: '16/09/2026 · 15:07', concept: 'Cobro factura A 0001-000345', kind: 'Ingreso', medium: 'Efectivo', reference: 'FAC-0345', amount: 178900, status: 'Confirmado', actor: 'Caja demo', note: 'Movimiento de demostración sin persistencia.' },
  { id: 'MOV-DEMO-007', occurredAt: '15/09/2026 · 18:20', concept: 'Diferencia de arqueo ilustrativa', kind: 'Ajuste', medium: 'Efectivo', reference: 'ARQ-0001', amount: 2150, status: 'Pendiente', actor: 'Supervisor demo', note: 'La diferencia no está conciliada. Ninguna tolerancia o aprobación se calcula localmente.' },
];

const uyu = new Intl.NumberFormat('es-UY', { style: 'currency', currency: 'UYU', maximumFractionDigits: 0 });
const formatUyu = (value: number) => uyu.format(value).replace('UYU', '').trim();

function kindClass(kind: MovementKind) {
  return kind.toLowerCase();
}

export function CashPage() {
  const [tab, setTab] = useState<CashTab>('Movimientos');
  const [selectedId, setSelectedId] = useState(movements[0].id);
  const [search, setSearch] = useState('');
  const [kind, setKind] = useState<'Todos' | MovementKind>('Todos');
  const [medium, setMedium] = useState<'Todos' | 'Efectivo' | 'Transferencia'>('Todos');

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return movements.filter((item) => {
      const matchesTerm = !term || [item.concept, item.reference, item.medium, item.actor].some((value) => value.toLowerCase().includes(term));
      const matchesKind = kind === 'Todos' || item.kind === kind;
      const matchesMedium = medium === 'Todos' || item.medium === medium;
      return matchesTerm && matchesKind && matchesMedium;
    });
  }, [search, kind, medium]);

  const selected = movements.find((item) => item.id === selectedId) ?? filtered[0] ?? movements[0];

  const summary = useMemo(() => {
    const income = movements.filter((item) => item.kind === 'Ingreso').reduce((sum, item) => sum + item.amount, 0);
    const expense = movements.filter((item) => item.kind === 'Egreso').reduce((sum, item) => sum + item.amount, 0);
    return {
      expected: 1482360,
      income,
      expense,
      variance: 2150,
    };
  }, []);

  return (
    <div className="cash-preview-page">
      <header className="cash-heading">
        <div>
          <div className="cash-breadcrumb">Finanzas <span aria-hidden="true">›</span> Caja y conciliación</div>
          <h1>Caja y conciliación</h1>
          <p>Inspecciona un turno de caja de demostración, sus movimientos y el arqueo sin ejecutar operaciones financieras reales.</p>
        </div>
        <div className="cash-heading-actions">
          <span className="cash-preview-badge">Preview UI · API pendiente</span>
          <button type="button" className="cash-primary" disabled title="API-CSH-002 no está disponible.">Abrir turno <span>DEMO</span></button>
        </div>
      </header>

      <div className="cash-demo-note" role="note">
        <strong>Datos de demostración.</strong> El turno, importes, movimientos, conteos y diferencias son fixtures locales. Abrir, mover, cerrar o reconciliar caja permanece bloqueado.
      </div>

      <section className="cash-summary" aria-label="Resumen visual de caja">
        <article className="cash-summary-card is-balance"><div className="cash-summary-icon">▣</div><div><span>Esperado en turno</span><strong>{formatUyu(summary.expected)} <small>UYU</small></strong><em>Fixture local</em></div></article>
        <article className="cash-summary-card is-income"><div className="cash-summary-icon">↑</div><div><span>Ingresos demo</span><strong>{formatUyu(summary.income)}</strong><em>Sin efecto financiero real</em></div></article>
        <article className="cash-summary-card is-expense"><div className="cash-summary-icon">↓</div><div><span>Egresos demo</span><strong>{formatUyu(summary.expense)}</strong><em>Sin efecto financiero real</em></div></article>
        <article className="cash-summary-card is-variance"><div className="cash-summary-icon">!</div><div><span>Diferencia ilustrativa</span><strong>{formatUyu(summary.variance)}</strong><em>Pendiente de autoridad servidor</em></div></article>
      </section>

      <div className="cash-tabs" role="tablist" aria-label="Secciones de caja">
        {(['Movimientos', 'Conciliación', 'Turno'] as CashTab[]).map((item) => (
          <button key={item} type="button" className={tab === item ? 'is-active' : undefined} onClick={() => setTab(item)}>{item}</button>
        ))}
      </div>

      {tab === 'Movimientos' && (
        <div className="cash-workspace">
          <section className="cash-panel cash-list-panel" aria-label="Movimientos de caja de demostración">
            <div className="cash-filters">
              <label className="cash-search"><span aria-hidden="true">⌕</span><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar concepto, referencia o medio…" /></label>
              <label className="cash-filter-field"><span>Tipo</span><select value={kind} onChange={(event) => setKind(event.target.value as 'Todos' | MovementKind)}><option>Todos</option><option>Ingreso</option><option>Egreso</option><option>Ajuste</option></select></label>
              <label className="cash-filter-field"><span>Medio</span><select value={medium} onChange={(event) => setMedium(event.target.value as 'Todos' | 'Efectivo' | 'Transferencia')}><option>Todos</option><option>Efectivo</option><option>Transferencia</option></select></label>
              <button type="button" className="cash-clear" onClick={() => { setSearch(''); setKind('Todos'); setMedium('Todos'); }}>Limpiar</button>
            </div>

            <div className="cash-table-wrap">
              <table className="cash-table">
                <thead><tr><th>Fecha</th><th>Concepto</th><th>Tipo</th><th>Medio</th><th>Referencia</th><th>Importe</th><th>Estado</th></tr></thead>
                <tbody>{filtered.map((item) => (
                  <tr key={item.id} className={selected.id === item.id ? 'is-selected' : undefined} tabIndex={0} onClick={() => setSelectedId(item.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setSelectedId(item.id); } }}>
                    <td>{item.occurredAt.split(' · ')[0]}</td><td><strong>{item.concept}</strong><span>{item.actor}</span></td><td><span className={`cash-kind is-${kindClass(item.kind)}`}>{item.kind}</span></td><td>{item.medium}</td><td>{item.reference}</td><td><strong>{formatUyu(item.amount)}</strong></td><td><span className={`cash-status is-${item.status.toLowerCase()}`}>{item.status}</span></td>
                  </tr>
                ))}</tbody>
              </table>
            </div>

            <div className="cash-mobile-list">{filtered.map((item) => (
              <button type="button" key={item.id} className={`cash-mobile-card ${selected.id === item.id ? 'is-selected' : ''}`} onClick={() => setSelectedId(item.id)}>
                <div className="cash-mobile-head"><span><strong>{item.concept}</strong><small>{item.occurredAt} · {item.medium}</small></span><strong>{formatUyu(item.amount)}</strong></div>
                <div className="cash-mobile-values"><span className={`cash-kind is-${kindClass(item.kind)}`}>{item.kind}</span><span className={`cash-status is-${item.status.toLowerCase()}`}>{item.status}</span></div>
              </button>
            ))}</div>

            <div className="cash-list-footer"><span>Mostrando {filtered.length} de {movements.length} movimientos locales</span><button type="button" disabled>+ Movimiento manual · DEMO</button></div>
          </section>

          <aside className="cash-panel cash-detail" aria-label="Detalle del movimiento seleccionado">
            <div className="cash-detail-head"><div><span>Detalle del movimiento</span><h2>{selected.reference}</h2><p>{selected.id}</p></div><span className={`cash-status is-${selected.status.toLowerCase()}`}>{selected.status}</span></div>
            <dl className="cash-detail-grid">
              <div><dt>Concepto</dt><dd>{selected.concept}</dd></div><div><dt>Fecha</dt><dd>{selected.occurredAt}</dd></div><div><dt>Tipo</dt><dd>{selected.kind}</dd></div><div><dt>Medio</dt><dd>{selected.medium}</dd></div><div><dt>Importe</dt><dd>{formatUyu(selected.amount)} UYU</dd></div><div><dt>Actor</dt><dd>{selected.actor}</dd></div>
            </dl>
            <div className="cash-info-box"><strong>Representación local</strong><span>{selected.note}</span></div>
            <div className="cash-history"><div className="cash-history-entry"><span className="cash-history-dot"/><div><time>{selected.occurredAt}</time><strong>Movimiento de demostración creado</strong><p>No se invoca `API-CSH-005` ni se persiste un asiento de caja.</p></div></div><div className="cash-history-entry"><span className="cash-history-dot"/><div><time>Estado actual</time><strong>{selected.status}</strong><p>El estado existe únicamente para revisión visual y de interacción.</p></div></div></div>
            <div className="cash-detail-actions"><button type="button" disabled>Editar</button><button type="button" disabled>Revertir</button></div>
          </aside>
        </div>
      )}

      {tab === 'Conciliación' && (
        <section className="cash-panel cash-reconciliation" aria-label="Conciliación de demostración">
          <div className="cash-section-heading"><div><span>Arqueo del turno</span><h2>Conteo y diferencia</h2></div><span className="cash-preview-badge">No ejecutable</span></div>
          <div className="cash-reconcile-grid">
            <article><span>Esperado</span><strong>{formatUyu(summary.expected)} UYU</strong><small>Cálculo local ilustrativo</small></article>
            <article><span>Contado</span><strong>{formatUyu(summary.expected - summary.variance)} UYU</strong><small>Fixture de conteo</small></article>
            <article className="is-alert"><span>Diferencia</span><strong>- {formatUyu(summary.variance)} UYU</strong><small>Sin tolerancia evaluada</small></article>
          </div>
          <label className="cash-disabled-field"><span>Explicación de diferencia</span><textarea disabled value="La explicación real se habilitará cuando exista API-CSH-007." readOnly /></label>
          <div className="cash-policy-box"><strong>Autoridad pendiente</strong><span>La vista no decide tolerancias, aprobación de supervisor ni resultado de conciliación. Esas reglas pertenecen al servidor.</span></div>
          <div className="cash-command-row"><button type="button" disabled>Cerrar turno · DEMO</button><button type="button" disabled>Conciliar diferencia · DEMO</button></div>
        </section>
      )}

      {tab === 'Turno' && (
        <section className="cash-panel cash-shift" aria-label="Turno de caja de demostración">
          <div className="cash-section-heading"><div><span>Turno actual · DEMO</span><h2>Terminal POS-01 · Casa Central</h2></div><span className="cash-status is-confirmado">Abierto · local</span></div>
          <dl className="cash-detail-grid cash-shift-grid"><div><dt>Operador</dt><dd>Admin Demo Uruguay</dd></div><div><dt>Apertura</dt><dd>21/09/2026 · 08:00</dd></div><div><dt>Fondo inicial</dt><dd>$ 12.000 UYU</dd></div><div><dt>Terminal</dt><dd>POS-01</dd></div><div><dt>Sucursal</dt><dd>Casa Central</dd></div><div><dt>Origen</dt><dd>Fixture local</dd></div></dl>
          <div className="cash-info-box"><strong>No es estado canónico</strong><span>`API-CSH-001` y `API-CSH-003` siguen ausentes. Este “turno abierto” existe solo para inspeccionar la composición aprobada.</span></div>
          <div className="cash-command-row"><button type="button" disabled>Movimiento manual · DEMO</button><button type="button" disabled>Cerrar turno · DEMO</button></div>
        </section>
      )}

      <div className="cash-bottom-note"><strong>Vista preliminar</strong><span>`UI-CASH-001` usa fixtures locales y `operations: []`. Ninguna acción de caja afecta datos reales.</span></div>
    </div>
  );
}
