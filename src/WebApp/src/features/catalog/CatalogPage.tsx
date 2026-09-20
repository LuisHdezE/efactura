import { useEffect, useMemo, useState } from 'react';
import type {
  CommercialItemDto,
  ItemCategoryDto,
  TaxProfileDto,
  UnitOfMeasureDto,
} from '../../contracts/api';
import { dataMode, gateways } from '../../services';
import './catalog.css';

type KindFilter = 'ALL' | CommercialItemDto['kind'];
type StatusFilter = 'ALL' | 'ACTIVE' | 'INACTIVE';
type InventoryFilter = 'ALL' | 'TRACKED' | 'UNTRACKED';
type DetailTab = 'information' | 'fiscal' | 'category';

function kindLabel(kind: CommercialItemDto['kind']) {
  return kind === 'PRODUCT' ? 'Producto' : 'Servicio';
}

function inventoryLabel(item: CommercialItemDto) {
  return item.trackInventory ? 'Controla stock' : 'No controla stock';
}

function compactId(id: string | null) {
  if (!id) return 'Sin asignar';
  return `${id.slice(0, 8)}…${id.slice(-4)}`;
}

export function CatalogPage() {
  const [items, setItems] = useState<CommercialItemDto[]>([]);
  const [categories, setCategories] = useState<ItemCategoryDto[]>([]);
  const [taxProfiles, setTaxProfiles] = useState<TaxProfileDto[]>([]);
  const [units, setUnits] = useState<UnitOfMeasureDto[]>([]);
  const [search, setSearch] = useState('');
  const [kind, setKind] = useState<KindFilter>('ALL');
  const [status, setStatus] = useState<StatusFilter>('ALL');
  const [inventory, setInventory] = useState<InventoryFilter>('ALL');
  const [categoryId, setCategoryId] = useState('ALL');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<DetailTab>('information');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    Promise.all([
      gateways.catalog.listItems(),
      gateways.catalog.listCategories(),
      gateways.catalog.listTaxProfiles(),
      gateways.catalog.listUnitsOfMeasure(),
    ])
      .then(([itemPage, categoryPage, taxPage, unitReference]) => {
        if (!mounted) return;
        setItems(itemPage.items);
        setCategories(categoryPage.items);
        setTaxProfiles(taxPage.items);
        setUnits(unitReference.items);
        setSelectedId(itemPage.items[0]?.id ?? null);
        setError(null);
      })
      .catch(() => {
        if (mounted) setError('No pudimos cargar el catálogo de demostración.');
      })
      .finally(() => {
        if (mounted) setLoading(false);
      });

    return () => {
      mounted = false;
    };
  }, []);

  const categoryById = useMemo(
    () => new Map(categories.map((category) => [category.id, category])),
    [categories]
  );

  const taxProfileById = useMemo(
    () => new Map(taxProfiles.map((profile) => [profile.id, profile])),
    [taxProfiles]
  );

  const unitByCode = useMemo(
    () => new Map(units.map((unit) => [unit.code, unit])),
    [units]
  );

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();

    return items.filter((item) => {
      if (kind !== 'ALL' && item.kind !== kind) return false;
      if (status === 'ACTIVE' && !item.active) return false;
      if (status === 'INACTIVE' && item.active) return false;
      if (inventory === 'TRACKED' && !item.trackInventory) return false;
      if (inventory === 'UNTRACKED' && item.trackInventory) return false;
      if (categoryId !== 'ALL' && item.categoryId !== categoryId) return false;
      if (!term) return true;

      return item.code.toLowerCase().includes(term) || item.name.toLowerCase().includes(term);
    });
  }, [items, search, kind, status, inventory, categoryId]);

  const selected = filtered.find((item) => item.id === selectedId) ?? filtered[0] ?? null;
  const selectedCategory = selected?.categoryId ? categoryById.get(selected.categoryId) ?? null : null;
  const selectedTaxProfile = selected?.taxProfileId ? taxProfileById.get(selected.taxProfileId) ?? null : null;
  const selectedUnit = selected ? unitByCode.get(selected.unit) ?? null : null;

  return (
    <div className="catalog-page">
      <header className="catalog-heading-row">
        <div>
          <div className="catalog-breadcrumb">Comercial <span aria-hidden="true">›</span> Productos y Servicios</div>
          <h1>Productos y Servicios</h1>
          <p>Maestro comercial unificado para productos y servicios.</p>
        </div>

        <div className="catalog-heading-actions" aria-label="Acciones de catálogo">
          <button
            className="catalog-secondary-action"
            type="button"
            disabled
            title="La gestión de categorías permanece deshabilitada hasta habilitar la integración gobernada."
          >
            Categorías
          </button>
          <button
            className="catalog-primary-action"
            type="button"
            disabled
            title="La escritura permanece deshabilitada hasta habilitar la integración gobernada del WebApp."
          >
            <span aria-hidden="true">＋</span>
            Nuevo item
          </button>
        </div>
      </header>

      <div className="catalog-layout">
        <section className="catalog-panel catalog-browser" aria-label="Catálogo de productos y servicios">
          <div className="catalog-filters">
            <label className="catalog-search">
              <span aria-hidden="true">⌕</span>
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Buscar por código o nombre…"
                aria-label="Buscar productos y servicios"
              />
            </label>

            <label className="catalog-select-wrap">
              <span className="sr-only">Tipo</span>
              <select value={kind} onChange={(event) => setKind(event.target.value as KindFilter)}>
                <option value="ALL">Todos los tipos</option>
                <option value="PRODUCT">Productos</option>
                <option value="SERVICE">Servicios</option>
              </select>
            </label>

            <label className="catalog-select-wrap">
              <span className="sr-only">Categoría</span>
              <select value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
                <option value="ALL">Todas las categorías</option>
                {categories.filter((category) => category.active).map((category) => (
                  <option key={category.id} value={category.id}>{category.name}</option>
                ))}
              </select>
            </label>

            <label className="catalog-select-wrap">
              <span className="sr-only">Inventario</span>
              <select value={inventory} onChange={(event) => setInventory(event.target.value as InventoryFilter)}>
                <option value="ALL">Todo inventario</option>
                <option value="TRACKED">Controla stock</option>
                <option value="UNTRACKED">No controla stock</option>
              </select>
            </label>

            <label className="catalog-select-wrap">
              <span className="sr-only">Estado</span>
              <select value={status} onChange={(event) => setStatus(event.target.value as StatusFilter)}>
                <option value="ALL">Todos los estados</option>
                <option value="ACTIVE">Activos</option>
                <option value="INACTIVE">Inactivos</option>
              </select>
            </label>
          </div>

          <div className="catalog-table-wrap">
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Nombre</th>
                  <th>Tipo</th>
                  <th>Categoría</th>
                  <th>Unidad</th>
                  <th>Inventario</th>
                  <th>Perfil fiscal</th>
                  <th>Estado</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={8}><div className="catalog-state">Cargando catálogo…</div></td></tr>
                ) : error ? (
                  <tr><td colSpan={8}><div className="catalog-state is-error">{error}</div></td></tr>
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={8}><div className="catalog-state"><strong>Sin coincidencias</strong><span>Probá otros filtros o una búsqueda diferente.</span></div></td></tr>
                ) : filtered.map((item) => {
                  const category = item.categoryId ? categoryById.get(item.categoryId) : null;
                  const taxProfile = item.taxProfileId ? taxProfileById.get(item.taxProfileId) : null;
                  const isSelected = selected?.id === item.id;

                  return (
                    <tr
                      key={item.id}
                      className={isSelected ? 'is-selected' : undefined}
                      tabIndex={0}
                      aria-selected={isSelected}
                      onClick={() => { setSelectedId(item.id); setActiveTab('information'); }}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' || event.key === ' ') {
                          event.preventDefault();
                          setSelectedId(item.id);
                          setActiveTab('information');
                        }
                      }}
                    >
                      <td><span className="catalog-code">{item.code}</span></td>
                      <td>
                        <span className="catalog-name-cell">
                          <strong>{item.name}</strong>
                          <small>{item.description || 'Sin descripción'}</small>
                        </span>
                      </td>
                      <td><span className={`catalog-kind is-${item.kind.toLowerCase()}`}>{kindLabel(item.kind)}</span></td>
                      <td>{category ? <span className="catalog-reference"><strong>{category.name}</strong><small>{category.code}</small></span> : <span className="catalog-muted">Sin categoría</span>}</td>
                      <td><span className="catalog-unit">{item.unit}</span></td>
                      <td><span className={`catalog-inventory ${item.trackInventory ? 'is-tracked' : ''}`}>{inventoryLabel(item)}</span></td>
                      <td>{taxProfile ? <span className="catalog-reference"><strong>{taxProfile.name}</strong><small>{taxProfile.code}</small></span> : <span className="catalog-muted">Sin perfil</span>}</td>
                      <td><span className={`catalog-status ${item.active ? 'is-active' : 'is-inactive'}`}>{item.active ? 'Activo' : 'Inactivo'}</span></td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <footer className="catalog-table-footer">
            <span><strong>{filtered.length}</strong> {filtered.length === 1 ? 'item visible' : 'items visibles'}</span>
            <span>Página 1 de 1 · Demo local</span>
          </footer>
        </section>

        <aside className="catalog-panel catalog-detail" aria-label="Detalle del item seleccionado">
          {!selected ? (
            <div className="catalog-empty-detail"><strong>Seleccioná un item</strong><span>Su maestro comercial aparecerá aquí.</span></div>
          ) : (
            <>
              <div className="catalog-detail-head">
                <div>
                  <div className="catalog-detail-code">{selected.code}</div>
                  <h2>{selected.name}</h2>
                  <div className="catalog-detail-meta">
                    <span className={`catalog-kind is-${selected.kind.toLowerCase()}`}>{kindLabel(selected.kind)}</span>
                    <span className={`catalog-status ${selected.active ? 'is-active' : 'is-inactive'}`}>{selected.active ? 'Activo' : 'Inactivo'}</span>
                  </div>
                </div>
                <button
                  type="button"
                  className="catalog-edit-action"
                  disabled
                  title="La edición permanece deshabilitada hasta habilitar la integración gobernada."
                >
                  Editar
                </button>
              </div>

              <div className="catalog-tabs" role="tablist" aria-label="Secciones del item">
                <button type="button" role="tab" aria-selected={activeTab === 'information'} className={activeTab === 'information' ? 'is-active' : ''} onClick={() => setActiveTab('information')}>Información</button>
                <button type="button" role="tab" aria-selected={activeTab === 'fiscal'} className={activeTab === 'fiscal' ? 'is-active' : ''} onClick={() => setActiveTab('fiscal')}>Fiscal y unidad</button>
                <button type="button" role="tab" aria-selected={activeTab === 'category'} className={activeTab === 'category' ? 'is-active' : ''} onClick={() => setActiveTab('category')}>Categoría</button>
              </div>

              {activeTab === 'information' && (
                <div className="catalog-detail-body">
                  <dl className="catalog-summary-grid">
                    <div><dt>Código</dt><dd>{selected.code}</dd></div>
                    <div><dt>Tipo</dt><dd>{kindLabel(selected.kind)}</dd></div>
                    <div><dt>Unidad</dt><dd>{selected.unit}</dd></div>
                    <div><dt>Versión</dt><dd>{selected.version}</dd></div>
                    <div><dt>Inventario</dt><dd>{inventoryLabel(selected)}</dd></div>
                    <div><dt>Estado</dt><dd>{selected.active ? 'Activo' : 'Inactivo'}</dd></div>
                  </dl>

                  <section className="catalog-detail-section">
                    <h3>Descripción</h3>
                    <p>{selected.description || 'Sin descripción registrada.'}</p>
                  </section>

                  {selected.kind === 'SERVICE' && (
                    <div className="catalog-contract-note">
                      <strong>Regla de dominio</strong>
                      <span>Los servicios no controlan inventario. Esta vista mantiene `trackInventory = false` y no muestra cantidades de stock.</span>
                    </div>
                  )}
                </div>
              )}

              {activeTab === 'fiscal' && (
                <div className="catalog-detail-body">
                  <section className="catalog-detail-section">
                    <h3>Perfil fiscal</h3>
                    {selectedTaxProfile ? (
                      <div className="catalog-info-card">
                        <div>
                          <strong>{selectedTaxProfile.name}</strong>
                          <span>{selectedTaxProfile.code} · {selectedTaxProfile.treatmentCode}</span>
                          <small>Vigente desde {selectedTaxProfile.effectiveFrom}{selectedTaxProfile.effectiveTo ? ` hasta ${selectedTaxProfile.effectiveTo}` : ''}</small>
                        </div>
                        <span className="catalog-rate">{selectedTaxProfile.ratePercent}%</span>
                      </div>
                    ) : (
                      <p className="catalog-muted">Sin perfil fiscal asignado.</p>
                    )}
                  </section>

                  <section className="catalog-detail-section">
                    <h3>Unidad comercial</h3>
                    <div className="catalog-info-card">
                      <div>
                        <strong>{selected.unit}</strong>
                        <span>{selectedUnit ? 'Unidad presente en la proyección configurada' : 'Unidad comercial del item'}</span>
                      </div>
                      {selectedUnit && (
                        <span className={`catalog-mini-state ${selectedUnit.dgiCfe25_2Compatible ? 'is-active' : ''}`}>
                          {selectedUnit.dgiCfe25_2Compatible ? 'CFE 25.2 compatible' : 'Revisar CFE 25.2'}
                        </span>
                      )}
                    </div>
                  </section>

                  <div className="catalog-contract-note">
                    <strong>Referencia demo, no catálogo DGI</strong>
                    <span>Las unidades son texto comercial configurable con sugerencias runtime. La UI no presenta una enumeración fiscal cerrada.</span>
                  </div>
                </div>
              )}

              {activeTab === 'category' && (
                <div className="catalog-detail-body">
                  {selectedCategory ? (
                    <div className="catalog-info-card is-stack">
                      <strong>{selectedCategory.name}</strong>
                      <span>Código: {selectedCategory.code}</span>
                      <small>Versión {selectedCategory.version} · {selectedCategory.active ? 'Activa' : 'Inactiva'}</small>
                    </div>
                  ) : (
                    <div className="catalog-empty-reference">
                      <strong>Sin categoría</strong>
                      <span>El contrato permite que un item no tenga categoría asignada.</span>
                    </div>
                  )}

                  <div className="catalog-contract-note">
                    <strong>Identificadores contractuales</strong>
                    <span>categoryId: {compactId(selected.categoryId)} · taxProfileId: {compactId(selected.taxProfileId)}</span>
                  </div>
                </div>
              )}
            </>
          )}
        </aside>
      </div>

      <p className="catalog-demo-note">
        UI-CATALOG-001 · {dataMode === 'mock' ? 'Datos locales de demostración.' : 'Modo API.'} El backend sigue siendo la autoridad contractual; esta vista no expone precio, costo, stock disponible, barcode ni media.
      </p>
    </div>
  );
}
