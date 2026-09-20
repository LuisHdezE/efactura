import { useEffect, useMemo, useState } from 'react';
import type { CommercialItemDto } from '../../contracts/api';
import type { InventoryPositionDto, StockMovementDto } from '../../contracts/inventory';
import { dataMode, gateways } from '../../services';
import './inventory.css';

type DetailTab = 'information' | 'movements';

function compactId(value: string) {
  return value.length <= 18 ? value : `${value.slice(0, 8)}…${value.slice(-6)}`;
}

function formatQuantity(value: number) {
  return new Intl.NumberFormat('es-UY', { maximumFractionDigits: 3 }).format(value);
}

function formatMovementDate(value: string) {
  return new Intl.DateTimeFormat('es-UY', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value));
}

function movementLabel(kind: string) {
  return kind === 'ADJUSTMENT' ? 'Ajuste' : kind;
}

export function InventoryPage() {
  const [positions, setPositions] = useState<InventoryPositionDto[]>([]);
  const [items, setItems] = useState<CommercialItemDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [detail, setDetail] = useState<InventoryPositionDto | null>(null);
  const [movements, setMovements] = useState<StockMovementDto[]>([]);
  const [search, setSearch] = useState('');
  const [locationId, setLocationId] = useState('ALL');
  const [activeTab, setActiveTab] = useState<DetailTab>('information');
  const [mobileDetailOpen, setMobileDetailOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [detailError, setDetailError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    gateways.inventory.listPositions()
      .then((page) => {
        if (!mounted) return;
        setPositions(page.items);
        setSelectedId(page.items[0]?.id ?? null);
        setMobileDetailOpen(Boolean(page.items[0]));
        setError(null);
      })
      .catch(() => {
        if (mounted) setError('No pudimos cargar las posiciones de inventario de demostración.');
      })
      .finally(() => {
        if (mounted) setLoading(false);
      });

    gateways.catalog.listItems()
      .then((page) => {
        if (mounted) setItems(page.items);
      })
      .catch(() => {
        if (mounted) setItems([]);
      });

    return () => {
      mounted = false;
    };
  }, []);

  const itemById = useMemo(() => new Map(items.map((item) => [item.id, item])), [items]);
  const locations = useMemo(
    () => Array.from(new Set(positions.map((position) => position.locationId))).sort(),
    [positions]
  );

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();

    return positions.filter((position) => {
      if (locationId !== 'ALL' && position.locationId !== locationId) return false;
      if (!term) return true;

      const item = itemById.get(position.itemId);
      return position.itemId.toLowerCase().includes(term)
        || position.locationId.toLowerCase().includes(term)
        || Boolean(item?.code.toLowerCase().includes(term))
        || Boolean(item?.name.toLowerCase().includes(term));
    });
  }, [positions, search, locationId, itemById]);

  const selectedSummary = filtered.find((position) => position.id === selectedId) ?? filtered[0] ?? null;

  useEffect(() => {
    if (!selectedSummary) {
      setDetail(null);
      setMovements([]);
      return;
    }

    let mounted = true;
    setDetailLoading(true);
    setDetailError(null);

    Promise.all([
      gateways.inventory.getPosition(selectedSummary.id),
      gateways.inventory.listMovements({ positionId: selectedSummary.id }),
    ])
      .then(([position, movementPage]) => {
        if (!mounted) return;
        setDetail(position);
        setMovements(movementPage.items);
      })
      .catch(() => {
        if (!mounted) return;
        setDetail(selectedSummary);
        setMovements([]);
        setDetailError('No pudimos cargar el detalle completo de esta posición.');
      })
      .finally(() => {
        if (mounted) setDetailLoading(false);
      });

    return () => {
      mounted = false;
    };
  }, [selectedSummary?.id]);

  const selected = detail?.id === selectedSummary?.id ? detail : selectedSummary;
  const selectedItem = selected ? itemById.get(selected.itemId) ?? null : null;

  const selectPosition = (positionId: string) => {
    setSelectedId(positionId);
    setActiveTab('information');
    setMobileDetailOpen(true);
  };

  return (
    <div className="inventory-page">
      <header className="inventory-heading-row">
        <div>
          <div className="inventory-breadcrumb">Inventario y Compras <span aria-hidden="true">›</span> Inventario</div>
          <h1>Inventario</h1>
          <p>Consulta de posiciones y movimientos por artículo y ubicación.</p>
        </div>

        <div className="inventory-heading-meta">
          <span className="inventory-mode-pill">{dataMode === 'mock' ? 'Mock local' : 'API'}</span>
          <button
            type="button"
            className="inventory-adjust-action"
            disabled
            title="El ajuste manual se habilitará cuando la integración de escritura y permisos estén gobernados en el WebApp."
          >
            Ajustar stock
          </button>
        </div>
      </header>

      <div className="inventory-layout">
        <section className="inventory-panel inventory-browser" aria-label="Posiciones de inventario">
          <div className="inventory-filters">
            <label className="inventory-search">
              <span aria-hidden="true">⌕</span>
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Buscar artículo, ID o ubicación…"
                aria-label="Buscar posiciones de inventario"
              />
            </label>

            <label className="inventory-select-wrap">
              <span className="sr-only">Ubicación</span>
              <select value={locationId} onChange={(event) => setLocationId(event.target.value)}>
                <option value="ALL">Todas las ubicaciones</option>
                {locations.map((location) => (
                  <option key={location} value={location}>{location}</option>
                ))}
              </select>
            </label>
          </div>

          <div className="inventory-table-wrap">
            <table className="inventory-table">
              <thead>
                <tr>
                  <th>Artículo</th>
                  <th>Ubicación</th>
                  <th>Cantidad</th>
                  <th>Versión</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={4}><div className="inventory-state">Cargando posiciones…</div></td></tr>
                ) : error ? (
                  <tr><td colSpan={4}><div className="inventory-state is-error">{error}</div></td></tr>
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={4}><div className="inventory-state"><strong>Sin coincidencias</strong><span>Probá otra búsqueda o ubicación.</span></div></td></tr>
                ) : filtered.map((position) => {
                  const item = itemById.get(position.itemId);
                  const isSelected = selectedSummary?.id === position.id;

                  return (
                    <tr
                      key={position.id}
                      className={isSelected ? 'is-selected' : undefined}
                      tabIndex={0}
                      aria-selected={isSelected}
                      onClick={() => selectPosition(position.id)}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' || event.key === ' ') {
                          event.preventDefault();
                          selectPosition(position.id);
                        }
                      }}
                    >
                      <td>
                        <span className="inventory-item-cell">
                          <strong>{item?.code ?? compactId(position.itemId)}</strong>
                          <span>{item?.name ?? 'Artículo sin enriquecimiento de catálogo'}</span>
                          <small>{compactId(position.itemId)}</small>
                        </span>
                      </td>
                      <td><span className="inventory-location">{position.locationId}</span></td>
                      <td><span className="inventory-quantity">{formatQuantity(position.quantity)} {item?.unit ?? ''}</span></td>
                      <td><span className="inventory-version">v{position.version}</span></td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <div className="inventory-mobile-list" aria-label="Posiciones de inventario en formato móvil">
            {filtered.map((position) => {
              const item = itemById.get(position.itemId);
              const isSelected = selectedSummary?.id === position.id;
              return (
                <button
                  key={position.id}
                  type="button"
                  className={`inventory-mobile-card ${isSelected ? 'is-selected' : ''}`}
                  onClick={() => selectPosition(position.id)}
                >
                  <span className="inventory-mobile-card-main">
                    <strong>{item?.code ?? compactId(position.itemId)}</strong>
                    <span>{item?.name ?? 'Artículo sin enriquecimiento'}</span>
                    <small>{position.locationId} · v{position.version}</small>
                  </span>
                  <span className="inventory-mobile-card-value">
                    <strong>{formatQuantity(position.quantity)}</strong>
                    <small>{item?.unit ?? 'cantidad'}</small>
                    <span aria-hidden="true">›</span>
                  </span>
                </button>
              );
            })}
          </div>

          <footer className="inventory-table-footer">
            <span><strong>{filtered.length}</strong> {filtered.length === 1 ? 'posición visible' : 'posiciones visibles'}</span>
            <span>Datos demo · sin totales agregados</span>
          </footer>
        </section>

        <aside
          className={`inventory-panel inventory-detail ${mobileDetailOpen ? 'is-mobile-open' : ''}`}
          aria-label="Detalle de posición seleccionada"
        >
          {!selected ? (
            <div className="inventory-empty-detail"><strong>Seleccioná una posición</strong><span>Su detalle aparecerá aquí.</span></div>
          ) : (
            <>
              <div className="inventory-detail-head">
                <div>
                  <div className="inventory-detail-kicker">{selectedItem?.code ?? compactId(selected.itemId)}</div>
                  <h2>{selectedItem?.name ?? 'Posición de inventario'}</h2>
                  <div className="inventory-detail-tags">
                    <span>{selected.locationId}</span>
                    {selectedItem?.trackInventory && <span className="is-positive">Controla stock</span>}
                  </div>
                </div>

                <button
                  type="button"
                  className="inventory-mobile-close"
                  aria-label="Cerrar detalle"
                  onClick={() => setMobileDetailOpen(false)}
                >
                  ×
                </button>
              </div>

              <div className="inventory-tabs" role="tablist" aria-label="Secciones de inventario">
                <button
                  type="button"
                  role="tab"
                  aria-selected={activeTab === 'information'}
                  className={activeTab === 'information' ? 'is-active' : ''}
                  onClick={() => setActiveTab('information')}
                >
                  Información
                </button>
                <button
                  type="button"
                  role="tab"
                  aria-selected={activeTab === 'movements'}
                  className={activeTab === 'movements' ? 'is-active' : ''}
                  onClick={() => setActiveTab('movements')}
                >
                  Movimientos
                </button>
              </div>

              {detailLoading && <div className="inventory-detail-loading">Actualizando detalle…</div>}
              {detailError && <div className="inventory-detail-warning">{detailError}</div>}

              {activeTab === 'information' && (
                <div className="inventory-detail-body">
                  <dl className="inventory-summary-grid">
                    <div><dt>Cantidad actual</dt><dd>{formatQuantity(selected.quantity)} {selectedItem?.unit ?? ''}</dd></div>
                    <div><dt>Versión</dt><dd>{selected.version}</dd></div>
                    <div><dt>Ubicación</dt><dd>{selected.locationId}</dd></div>
                    <div><dt>Position ID</dt><dd title={selected.id}>{compactId(selected.id)}</dd></div>
                  </dl>

                  <section className="inventory-contract-card">
                    <h3>Artículo autoritativo</h3>
                    <strong>{selectedItem ? `${selectedItem.code} · ${selectedItem.name}` : compactId(selected.itemId)}</strong>
                    <span>{selectedItem ? `Item ID ${compactId(selected.itemId)}` : selected.itemId}</span>
                    <small>El código y nombre provienen de Catálogo solo como enriquecimiento visual; la posición conserva itemId como referencia autoritativa.</small>
                  </section>

                  <section className="inventory-contract-card">
                    <h3>Semántica de cantidad</h3>
                    <span>Esta vista muestra únicamente `quantity` de la posición. No fabrica reservado, disponible para venta, mínimo, EOQ ni valorización.</span>
                  </section>
                </div>
              )}

              {activeTab === 'movements' && (
                <div className="inventory-detail-body inventory-movement-body">
                  {movements.length === 0 ? (
                    <div className="inventory-empty-movements">
                      <strong>Sin movimientos proyectados</strong>
                      <span>El historial HTTP actual solo admite tipos que esta versión pueda proyectar de forma segura.</span>
                    </div>
                  ) : movements.map((movement) => (
                    <article key={movement.id} className="inventory-movement-card">
                      <div className="inventory-movement-head">
                        <div>
                          <strong>{movementLabel(movement.kind)}</strong>
                          <span>{formatMovementDate(movement.occurredAtUtc)}</span>
                        </div>
                        <span className={`inventory-delta ${movement.quantityDelta >= 0 ? 'is-positive' : 'is-negative'}`}>
                          {movement.quantityDelta > 0 ? '+' : ''}{formatQuantity(movement.quantityDelta)}
                        </span>
                      </div>
                      <div className="inventory-movement-equation">
                        <span>{formatQuantity(movement.quantityBefore)}</span>
                        <span aria-hidden="true">→</span>
                        <strong>{formatQuantity(movement.quantityAfter)}</strong>
                      </div>
                      <div className="inventory-movement-reason">
                        <strong>{movement.reasonCode}</strong>
                        {movement.explanation && <span>{movement.explanation}</span>}
                      </div>
                    </article>
                  ))}
                </div>
              )}

              <div className="inventory-detail-actions">
                <button
                  type="button"
                  disabled
                  title="La escritura permanece deshabilitada hasta habilitar la integración gobernada y evaluar inventory.adjust."
                >
                  Ajustar stock
                </button>
                <span>API-INV-004 soportado · integración WebApp pendiente</span>
              </div>
            </>
          )}
        </aside>
      </div>

      {mobileDetailOpen && <button type="button" className="inventory-mobile-backdrop" aria-label="Cerrar detalle" onClick={() => setMobileDetailOpen(false)} />}
    </div>
  );
}
