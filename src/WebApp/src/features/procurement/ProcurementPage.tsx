import { useMemo, useState } from 'react';
import '../workflow-preview.css';

type PurchaseStatus = 'Borrador' | 'Aprobada' | 'Recepción parcial' | 'Recibida';

type PurchaseLine = {
  code: string;
  item: string;
  ordered: number;
  received: number;
  unitCost: number;
};

type PurchaseOrderPreview = {
  id: string;
  supplier: string;
  status: PurchaseStatus;
  expected: string;
  currency: 'UYU' | 'USD';
  reference: string;
  receipt?: string;
  lines: PurchaseLine[];
};

const orders: PurchaseOrderPreview[] = [
  {
    id: 'OC-000347', supplier: 'TecnoParts Uruguay', status: 'Recepción parcial', expected: '23/09/2026', currency: 'USD', reference: 'Reposición accesorios', receipt: 'REC-000091',
    lines: [
      { code: 'CRG-30W', item: 'Cargador USB-C 30 W', ordered: 20, received: 12, unitCost: 11.8 },
      { code: 'CAB-USBC-2M', item: 'Cable USB-C 2 m', ordered: 40, received: 40, unitCost: 3.2 },
    ],
  },
  {
    id: 'OC-000346', supplier: 'Mobile Supply SA', status: 'Aprobada', expected: '25/09/2026', currency: 'USD', reference: 'Componentes reparación',
    lines: [
      { code: 'LCD-A14', item: 'Pantalla Galaxy A14', ordered: 12, received: 0, unitCost: 31.5 },
      { code: 'BAT-A54', item: 'Batería Galaxy A54', ordered: 18, received: 0, unitCost: 9.75 },
    ],
  },
  {
    id: 'OC-000345', supplier: 'Distribuidora del Plata', status: 'Recibida', expected: '19/09/2026', currency: 'UYU', reference: 'Consumibles taller', receipt: 'REC-000090',
    lines: [
      { code: 'ALC-IPA', item: 'Alcohol isopropílico', ordered: 24, received: 24, unitCost: 395 },
      { code: 'FLX-UNIV', item: 'Flux universal', ordered: 10, received: 10, unitCost: 520 },
    ],
  },
  {
    id: 'OC-000344', supplier: 'Importadora Sur', status: 'Borrador', expected: '30/09/2026', currency: 'USD', reference: 'Stock preventivo',
    lines: [{ code: 'GLS-S23', item: 'Vidrio Galaxy S23', ordered: 30, received: 0, unitCost: 6.4 }],
  },
];

function purchaseStatusClass(status: PurchaseStatus) {
  return status.toLowerCase().replaceAll(' ', '-').normalize('NFD').replace(/[\u0300-\u036f]/g, '');
}

function money(value: number, currency: string) {
  return new Intl.NumberFormat('es-UY', { style: 'currency', currency }).format(value);
}

export function ProcurementPage() {
  const [selectedId, setSelectedId] = useState(orders[0].id);
  const [search, setSearch] = useState('');

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return orders;
    return orders.filter((order) =>
      [order.id, order.supplier, order.status, order.reference]
        .some((value) => value.toLowerCase().includes(term))
    );
  }, [search]);

  const selected = orders.find((order) => order.id === selectedId) ?? filtered[0] ?? orders[0];
  const total = selected.lines.reduce((sum, line) => sum + line.ordered * line.unitCost, 0);
  const hasDiscrepancy = selected.lines.some((line) => line.received > 0 && line.received !== line.ordered);

  return (
    <div className="workflow-preview-page">
      <header className="workflow-preview-heading">
        <div>
          <div className="workflow-preview-breadcrumb">Inventario y Compras <span aria-hidden="true">›</span> Compras</div>
          <h1>Órdenes de compra y Recepciones</h1>
          <p>Control de órdenes, recepción de mercadería y diferencias contra lo solicitado.</p>
        </div>
        <div className="workflow-preview-heading-actions">
          <span className="workflow-preview-mode">Preview UI · API pendiente</span>
          <button type="button" className="workflow-preview-primary" disabled title="Se habilitará cuando API-PRC-* y API-GRC-* estén disponibles.">
            Nueva orden de compra
          </button>
        </div>
      </header>

      <div className="workflow-preview-note" role="note">
        Datos de demostración. La consulta y selección funcionan en el navegador; crear, editar, aprobar, recibir y contabilizar dependen del backend de compras.
      </div>

      <div className="workflow-preview-layout">
        <section className="workflow-preview-panel" aria-label="Órdenes de compra">
          <div className="workflow-preview-toolbar">
            <label className="workflow-preview-search">
              <span aria-hidden="true">⌕</span>
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar orden, proveedor o estado…" />
            </label>
            <span className="workflow-preview-counter">{filtered.length} órdenes</span>
          </div>

          <div className="workflow-preview-table-wrap">
            <table className="workflow-preview-table">
              <thead><tr><th>Orden</th><th>Proveedor</th><th>Estado</th><th>Fecha esperada</th></tr></thead>
              <tbody>
                {filtered.map((order) => (
                  <tr
                    key={order.id}
                    className={order.id === selected.id ? 'is-selected' : undefined}
                    tabIndex={0}
                    onClick={() => setSelectedId(order.id)}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault();
                        setSelectedId(order.id);
                      }
                    }}
                  >
                    <td><strong>{order.id}</strong><span>{order.reference}</span></td>
                    <td><strong>{order.supplier}</strong><span>{order.currency}</span></td>
                    <td><span className={`workflow-preview-status is-${purchaseStatusClass(order.status)}`}>{order.status}</span></td>
                    <td>{order.expected}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <aside className="workflow-preview-panel workflow-preview-detail" aria-label={`Detalle ${selected.id}`}>
          <div className="workflow-preview-detail-head">
            <div><span>Orden de compra</span><h2>{selected.id}</h2><p>{selected.supplier}</p></div>
            <span className={`workflow-preview-status is-${purchaseStatusClass(selected.status)}`}>{selected.status}</span>
          </div>

          <div className="workflow-preview-summary-grid">
            <div><span>Fecha esperada</span><strong>{selected.expected}</strong></div>
            <div><span>Moneda</span><strong>{selected.currency}</strong></div>
            <div><span>Recepción</span><strong>{selected.receipt ?? 'Pendiente'}</strong></div>
            <div><span>Total ilustrativo</span><strong>{money(total, selected.currency)}</strong></div>
          </div>

          {hasDiscrepancy ? (
            <div className="workflow-preview-warning"><strong>Recepción parcial</strong><span>Hay cantidades recibidas distintas de las ordenadas. La diferencia permanece visible hasta la resolución autoritativa del servidor.</span></div>
          ) : null}

          <div className="workflow-preview-lines">
            <div className="workflow-preview-section-title">Detalle de artículos</div>
            {selected.lines.map((line) => {
              const difference = line.received - line.ordered;
              return (
                <div className="workflow-preview-line" key={line.code}>
                  <div><strong>{line.item}</strong><span>{line.code}</span></div>
                  <div><span>Ordenado</span><strong>{line.ordered}</strong></div>
                  <div><span>Recibido</span><strong>{line.received || '—'}</strong></div>
                  <div><span>Costo unit.</span><strong>{money(line.unitCost, selected.currency)}</strong></div>
                  <div><span>Diferencia</span><strong className={difference !== 0 && line.received > 0 ? 'is-difference' : undefined}>{line.received > 0 ? difference : '—'}</strong></div>
                </div>
              );
            })}
          </div>

          <div className="workflow-preview-actions">
            <button type="button" disabled>Editar</button>
            <button type="button" disabled>Aprobar</button>
            <button type="button" disabled>Registrar recepción</button>
            <button type="button" disabled>Publicar recepción</button>
          </div>
        </aside>
      </div>
    </div>
  );
}
