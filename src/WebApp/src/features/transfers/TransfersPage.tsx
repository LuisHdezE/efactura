import { useMemo, useState } from 'react';
import '../workflow-preview.css';

type TransferStatus = 'Solicitada' | 'Aprobada' | 'Despachada' | 'Recibida';

type TransferLine = {
  code: string;
  item: string;
  requested: number;
  received?: number;
};

type TransferPreview = {
  id: string;
  source: string;
  destination: string;
  status: TransferStatus;
  updatedAt: string;
  reference: string;
  lines: TransferLine[];
};

const transfers: TransferPreview[] = [
  {
    id: 'TRF-000184',
    source: 'Depósito Central',
    destination: 'Sucursal Centro',
    status: 'Despachada',
    updatedAt: '20/09/2026 · 16:42',
    reference: 'Reposición semanal',
    lines: [
      { code: 'CAB-USBC-2M', item: 'Cable USB-C 2 m', requested: 12 },
      { code: 'CRG-30W', item: 'Cargador USB-C 30 W', requested: 6 },
      { code: 'FND-IP15', item: 'Funda iPhone 15', requested: 8 },
    ],
  },
  {
    id: 'TRF-000183',
    source: 'Sucursal Norte',
    destination: 'Depósito Central',
    status: 'Recibida',
    updatedAt: '20/09/2026 · 14:18',
    reference: 'Rebalanceo de stock',
    lines: [
      { code: 'BAT-A54', item: 'Batería Galaxy A54', requested: 5, received: 5 },
      { code: 'LCD-A14', item: 'Pantalla Galaxy A14', requested: 4, received: 3 },
    ],
  },
  {
    id: 'TRF-000182',
    source: 'Depósito Central',
    destination: 'Sucursal Norte',
    status: 'Aprobada',
    updatedAt: '20/09/2026 · 11:05',
    reference: 'Demanda extraordinaria',
    lines: [
      { code: 'MIC-IP13', item: 'Micrófono iPhone 13', requested: 10 },
      { code: 'SPK-IP13', item: 'Parlante iPhone 13', requested: 10 },
    ],
  },
  {
    id: 'TRF-000181',
    source: 'Sucursal Centro',
    destination: 'Sucursal Norte',
    status: 'Solicitada',
    updatedAt: '19/09/2026 · 18:27',
    reference: 'Cobertura de faltantes',
    lines: [{ code: 'GLS-S23', item: 'Vidrio Galaxy S23', requested: 15 }],
  },
];

const lifecycle: TransferStatus[] = ['Solicitada', 'Aprobada', 'Despachada', 'Recibida'];

function statusClass(status: TransferStatus) {
  return status.toLowerCase().replace('ó', 'o');
}

export function TransfersPage() {
  const [selectedId, setSelectedId] = useState(transfers[0].id);
  const [search, setSearch] = useState('');

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return transfers;
    return transfers.filter((transfer) =>
      [transfer.id, transfer.source, transfer.destination, transfer.reference, transfer.status]
        .some((value) => value.toLowerCase().includes(term))
    );
  }, [search]);

  const selected = transfers.find((transfer) => transfer.id === selectedId) ?? filtered[0] ?? transfers[0];
  const discrepancy = selected.lines.some((line) => line.received !== undefined && line.received !== line.requested);
  const lifecycleIndex = lifecycle.indexOf(selected.status);

  return (
    <div className="workflow-preview-page">
      <header className="workflow-preview-heading">
        <div>
          <div className="workflow-preview-breadcrumb">Inventario y Compras <span aria-hidden="true">›</span> Transferencias</div>
          <h1>Transferencias</h1>
          <p>Seguimiento operativo entre ubicaciones, desde solicitud hasta recepción y diferencias.</p>
        </div>
        <div className="workflow-preview-heading-actions">
          <span className="workflow-preview-mode">Preview UI · API pendiente</span>
          <button type="button" className="workflow-preview-primary" disabled title="Se habilitará cuando API-TRF-001..007 estén disponibles.">
            Nueva transferencia
          </button>
        </div>
      </header>

      <div className="workflow-preview-note" role="note">
        Datos de demostración. La navegación, selección y detalle son funcionales; crear, aprobar, despachar, recibir y reconciliar requieren la API de transferencias.
      </div>

      <div className="workflow-preview-layout">
        <section className="workflow-preview-panel" aria-label="Listado de transferencias">
          <div className="workflow-preview-toolbar">
            <label className="workflow-preview-search">
              <span aria-hidden="true">⌕</span>
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar transferencia, ubicación o estado…" />
            </label>
            <span className="workflow-preview-counter">{filtered.length} transferencias</span>
          </div>

          <div className="workflow-preview-table-wrap">
            <table className="workflow-preview-table">
              <thead>
                <tr><th>Transferencia</th><th>Origen → destino</th><th>Estado</th><th>Actualización</th></tr>
              </thead>
              <tbody>
                {filtered.map((transfer) => (
                  <tr
                    key={transfer.id}
                    className={transfer.id === selected.id ? 'is-selected' : undefined}
                    tabIndex={0}
                    onClick={() => setSelectedId(transfer.id)}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault();
                        setSelectedId(transfer.id);
                      }
                    }}
                  >
                    <td><strong>{transfer.id}</strong><span>{transfer.reference}</span></td>
                    <td><strong>{transfer.source}</strong><span>→ {transfer.destination}</span></td>
                    <td><span className={`workflow-preview-status is-${statusClass(transfer.status)}`}>{transfer.status}</span></td>
                    <td>{transfer.updatedAt}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <aside className="workflow-preview-panel workflow-preview-detail" aria-label={`Detalle ${selected.id}`}>
          <div className="workflow-preview-detail-head">
            <div><span>Transferencia</span><h2>{selected.id}</h2><p>{selected.reference}</p></div>
            <span className={`workflow-preview-status is-${statusClass(selected.status)}`}>{selected.status}</span>
          </div>

          <div className="workflow-preview-route-card">
            <div><span>Origen</span><strong>{selected.source}</strong></div>
            <span className="workflow-preview-arrow" aria-hidden="true">→</span>
            <div><span>Destino</span><strong>{selected.destination}</strong></div>
          </div>

          <div className="workflow-preview-lifecycle" aria-label="Ciclo de vida de la transferencia">
            {lifecycle.map((step, index) => (
              <div key={step} className={index <= lifecycleIndex ? 'is-complete' : undefined}>
                <span>{index + 1}</span><strong>{step}</strong>
              </div>
            ))}
          </div>

          {discrepancy ? (
            <div className="workflow-preview-warning"><strong>Diferencia pendiente de conciliación</strong><span>La cantidad recibida no coincide con la solicitada en una línea. El servidor deberá resolver la conciliación.</span></div>
          ) : null}

          <div className="workflow-preview-lines">
            <div className="workflow-preview-section-title">Artículos</div>
            {selected.lines.map((line) => {
              const difference = line.received === undefined ? null : line.received - line.requested;
              return (
                <div className="workflow-preview-line" key={line.code}>
                  <div><strong>{line.item}</strong><span>{line.code}</span></div>
                  <div><span>Solicitado</span><strong>{line.requested}</strong></div>
                  <div><span>Recibido</span><strong>{line.received ?? '—'}</strong></div>
                  <div><span>Diferencia</span><strong className={difference && difference !== 0 ? 'is-difference' : undefined}>{difference === null ? '—' : difference}</strong></div>
                </div>
              );
            })}
          </div>

          <div className="workflow-preview-actions">
            <button type="button" disabled>Aprobar</button>
            <button type="button" disabled>Despachar</button>
            <button type="button" disabled>Recibir</button>
            <button type="button" disabled>Reconciliar</button>
          </div>
        </aside>
      </div>
    </div>
  );
}
