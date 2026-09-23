import { useMemo, useState } from 'react';
import { contingencyPreviewRecords, contingencyStatusCards, syncStatusLabels } from './fixtures';
import type { PreviewRecordKind, SyncOperationStatus } from './types';
import './contingency-preview.css';

type KindFilter = 'ALL' | PreviewRecordKind;
type StatusFilter = 'ALL' | SyncOperationStatus;

function statusClass(status: SyncOperationStatus) {
  return status.toLowerCase().replaceAll('_', '-');
}

export function ContingencyPage() {
  const [search, setSearch] = useState('');
  const [kind, setKind] = useState<KindFilter>('ALL');
  const [status, setStatus] = useState<StatusFilter>('ALL');
  const [selectedId, setSelectedId] = useState(contingencyPreviewRecords[0].id);
  const [mobileFiltersOpen, setMobileFiltersOpen] = useState(false);

  const filteredRecords = useMemo(() => {
    const term = search.trim().toLowerCase();

    return contingencyPreviewRecords.filter((record) => {
      const matchesSearch = !term || [
        record.reference,
        record.source,
        record.statusLabel,
        record.summary,
      ].some((value) => value.toLowerCase().includes(term));
      const matchesKind = kind === 'ALL' || record.kind === kind;
      const matchesStatus = status === 'ALL' || record.status === status;

      return matchesSearch && matchesKind && matchesStatus;
    });
  }, [kind, search, status]);

  const selected = filteredRecords.find((record) => record.id === selectedId)
    ?? filteredRecords[0]
    ?? contingencyPreviewRecords[0];

  const clearFilters = () => {
    setSearch('');
    setKind('ALL');
    setStatus('ALL');
  };

  return (
    <div className="contingency-preview-page">
      <header className="contingency-heading">
        <div>
          <div className="contingency-breadcrumb">Fiscal <span aria-hidden="true">›</span> Contingencia / Sincronización</div>
          <h1>Contingencia / Sincronización</h1>
          <p>Supervisa visualmente contingencia formal CFC, operaciones offline, conflictos y recuperación sin presentar estado DGI/API ni acciones server-owned como reales.</p>
        </div>
        <div className="contingency-heading-actions">
          <span className="contingency-preview-badge">Preview UI · API pendiente</span>
          <button type="button" className="contingency-secondary" disabled title="La entrada a contingencia requiere autoridad HTTP y permisos reales.">
            Entrar en contingencia
          </button>
          <button type="button" className="contingency-primary" disabled title="La sincronización real no está disponible en este preview.">
            Forzar sincronización
          </button>
        </div>
      </header>

      <div className="contingency-demo-note" role="note">
        <strong>Datos de demostración.</strong>
        <span>Esta vista usa fixtures locales deterministas, no consulta DGI/proveedor ni backend, no ejecuta replay y no modifica contingencia, CFC o batches de sincronización.</span>
      </div>

      <section className="contingency-status-grid" aria-label="Estado operativo de demostración">
        {contingencyStatusCards.map((card) => (
          <article key={card.id} className={`contingency-status-card is-${card.tone}`}>
            <div className="contingency-status-card__topline">
              <span>{card.label}</span>
              <span className="contingency-local-pill">demo local</span>
            </div>
            <strong>{card.value}</strong>
            <p>{card.detail}</p>
          </article>
        ))}
      </section>

      <section className="contingency-workspace" aria-label="Supervisión local de contingencia y sincronización">
        <div className="contingency-list-panel">
          <div className="contingency-panel-heading">
            <div>
              <span className="contingency-eyebrow">Cola y documentos</span>
              <h2>Operaciones de demostración</h2>
            </div>
            <button
              type="button"
              className="contingency-filter-toggle"
              aria-expanded={mobileFiltersOpen}
              onClick={() => setMobileFiltersOpen((current) => !current)}
            >
              Filtros
            </button>
          </div>

          <div className={`contingency-filters ${mobileFiltersOpen ? 'is-open' : ''}`}>
            <label className="contingency-search">
              <span>Buscar</span>
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="ID, origen, estado o detalle…"
              />
            </label>
            <label>
              <span>Tipo</span>
              <select value={kind} onChange={(event) => setKind(event.target.value as KindFilter)}>
                <option value="ALL">Todos</option>
                <option value="CFC">Documento CFC</option>
                <option value="SYNC">Operación sync</option>
              </select>
            </label>
            <label>
              <span>Estado</span>
              <select value={status} onChange={(event) => setStatus(event.target.value as StatusFilter)}>
                <option value="ALL">Todos</option>
                {Object.entries(syncStatusLabels).map(([value, label]) => (
                  <option key={value} value={value}>{label}</option>
                ))}
              </select>
            </label>
            <button type="button" className="contingency-clear" onClick={clearFilters}>Limpiar</button>
          </div>

          <div className="contingency-table-wrap">
            <table className="contingency-table">
              <thead>
                <tr>
                  <th>Identidad</th>
                  <th>Tipo</th>
                  <th>Origen</th>
                  <th>Creado</th>
                  <th>Estado</th>
                </tr>
              </thead>
              <tbody>
                {filteredRecords.map((record) => (
                  <tr
                    key={record.id}
                    className={selected.id === record.id ? 'is-selected' : undefined}
                    tabIndex={0}
                    onClick={() => setSelectedId(record.id)}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault();
                        setSelectedId(record.id);
                      }
                    }}
                  >
                    <td><strong>{record.reference}</strong><span>{record.kind === 'CFC' ? 'Identidad CFC demo preservada' : 'ID único de operación demo'}</span></td>
                    <td>{record.kind === 'CFC' ? 'CFC' : 'Sync'}</td>
                    <td>{record.source}</td>
                    <td>{record.createdAt}</td>
                    <td><span className={`contingency-status is-${statusClass(record.status)}`}>{record.statusLabel}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="contingency-mobile-records" aria-label="Registros de demostración en móvil">
            {filteredRecords.map((record) => (
              <button
                key={record.id}
                type="button"
                className={`contingency-mobile-card ${selected.id === record.id ? 'is-selected' : ''}`}
                onClick={() => setSelectedId(record.id)}
              >
                <span className="contingency-mobile-card__topline">
                  <strong>{record.reference}</strong>
                  <span>{record.kind === 'CFC' ? 'CFC' : 'Sync'}</span>
                </span>
                <span>{record.source}</span>
                <span className={`contingency-status is-${statusClass(record.status)}`}>{record.statusLabel}</span>
              </button>
            ))}
          </div>

          {filteredRecords.length === 0 && (
            <div className="contingency-empty">
              <strong>Sin coincidencias en el fixture local</strong>
              <span>Ajusta búsqueda o filtros. No se realizó ninguna consulta HTTP.</span>
            </div>
          )}
        </div>

        <aside className="contingency-detail-panel" aria-label="Detalle de demostración">
          <div className="contingency-panel-heading contingency-detail-heading">
            <div>
              <span className="contingency-eyebrow">Detalle / revisión</span>
              <h2>{selected.reference}</h2>
            </div>
            <span className={`contingency-status is-${statusClass(selected.status)}`}>{selected.statusLabel}</span>
          </div>

          <div className="contingency-detail-callout">
            <strong>Dato de demostración</strong>
            <span>Sin consulta HTTP. Los valores siguientes no son autoridad fiscal ni estado live del servidor.</span>
          </div>

          <dl className="contingency-detail-grid">
            <div><dt>Tipo</dt><dd>{selected.kind === 'CFC' ? 'Documento CFC demo' : 'Operación sync demo'}</dd></div>
            <div><dt>Origen</dt><dd>{selected.source}</dd></div>
            <div><dt>Creado</dt><dd>{selected.createdAt}</dd></div>
            <div><dt>Estado contractual</dt><dd>{selected.statusLabel}</dd></div>
          </dl>

          <section className="contingency-detail-section">
            <h3>Resumen</h3>
            <p>{selected.summary}</p>
          </section>
          <section className="contingency-detail-section">
            <h3>Evidencia / motivo</h3>
            <p>{selected.evidence}</p>
          </section>
          <section className="contingency-detail-section">
            <h3>Resultado canónico</h3>
            <p>{selected.canonicalResult}</p>
          </section>
          <section className="contingency-detail-section">
            <h3>Recuperación</h3>
            <p>{selected.recovery}</p>
          </section>

          <section className="contingency-history">
            <h3>Historial local</h3>
            {selected.history.map((entry) => (
              <div key={`${selected.id}-${entry.at}-${entry.title}`} className="contingency-history-entry">
                <span>{entry.at}</span>
                <div><strong>{entry.title}</strong><p>{entry.detail}</p></div>
              </div>
            ))}
          </section>

          <div className="contingency-blocked-actions">
            <button type="button" disabled>Reconciliar CFC</button>
            <button type="button" disabled>Reintentar operación</button>
            <p>Acciones server-owned bloqueadas hasta disponer de contratos HTTP, sesión y permisos autoritativos.</p>
          </div>
        </aside>
      </section>
    </div>
  );
}
