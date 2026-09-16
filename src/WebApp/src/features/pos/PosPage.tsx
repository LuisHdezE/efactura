import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import type {
  CommercialItemDto,
  PartyDto,
  SaleCommercialIntent,
  SaleCreateInput,
  SaleDto,
  SaleFiscalPreviewDto,
  SaleLineDraft,
  SaleValidationDto,
} from '../../contracts/api';
import { gateways } from '../../services';
import { getItemVisualMetadata } from './itemVisualMetadata';

const money = new Intl.NumberFormat('es-UY', { style: 'currency', currency: 'UYU' });

type SaleBusyState = 'saving' | 'validating' | 'preview' | null;
type CatalogFilter = 'ALL' | 'PRODUCT' | 'SERVICE';

function ProductVisual({ item, compact = false }: { item: CommercialItemDto; compact?: boolean }) {
  const visual = getItemVisualMetadata(item);

  if (visual.imageUrl && visual.column !== undefined && visual.row !== undefined) {
    return (
      <svg
        viewBox="0 0 160 160"
        role="img"
        aria-label={visual.alt}
        className={`pos-product-visual ${compact ? 'compact' : ''}`}
      >
        <image
          href={visual.imageUrl}
          x={-visual.column * 160}
          y={-visual.row * 160}
          width="800"
          height="480"
          preserveAspectRatio="none"
        />
      </svg>
    );
  }

  return (
    <div className={`pos-product-visual ${compact ? 'compact' : ''} flex items-center justify-center`} aria-label={visual.alt}>
      <svg viewBox="0 0 24 24" aria-hidden="true" className={compact ? 'h-4 w-4 opacity-50' : 'h-7 w-7 opacity-40'} fill="none" stroke="currentColor" strokeWidth="1.7">
        <path d="M4 7.5 12 3l8 4.5v9L12 21l-8-4.5v-9Z" />
        <path d="m4.5 7.8 7.5 4.1 7.5-4.1M12 12v9" />
      </svg>
    </div>
  );
}

function PreviewValue({ label, value }: { label: string; value: string }) {
  return (
    <div className="pos-preview-value">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

export function PosPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const search = searchParams.get('q') ?? '';
  const [items, setItems] = useState<CommercialItemDto[]>([]);
  const [customers, setCustomers] = useState<PartyDto[]>([]);
  const [catalogFilter, setCatalogFilter] = useState<CatalogFilter>('ALL');
  const [customerId, setCustomerId] = useState('');
  const [intent, setIntent] = useState<SaleCommercialIntent>('CONSUMER_FINAL');
  const [deliveryCountry, setDeliveryCountry] = useState('');
  const [effectiveOn] = useState(() => new Date().toISOString().slice(0, 10));
  const [cart, setCart] = useState<SaleLineDraft[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [sale, setSale] = useState<SaleDto | null>(null);
  const [draftDirty, setDraftDirty] = useState(false);
  const [validation, setValidation] = useState<SaleValidationDto | null>(null);
  const [preview, setPreview] = useState<SaleFiscalPreviewDto | null>(null);
  const [saleBusy, setSaleBusy] = useState<SaleBusyState>(null);
  const [saleError, setSaleError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([gateways.catalog.listItems(), gateways.parties.listCustomers()])
      .then(([itemPage, partyPage]) => {
        setItems(itemPage.items);
        setCustomers(partyPage.items);
      })
      .catch(() => setLoadError('No se pudo cargar el catálogo o los clientes de demostración.'))
      .finally(() => setLoading(false));
  }, []);

  const setSearch = (value: string) => {
    const next = new URLSearchParams(searchParams);
    if (value.trim()) next.set('q', value);
    else next.delete('q');
    setSearchParams(next, { replace: true });
  };

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return items.filter((item) => {
      const matchesText = !term || item.code.toLowerCase().includes(term) || item.name.toLowerCase().includes(term);
      const matchesKind = catalogFilter === 'ALL' || item.kind === catalogFilter;
      return item.active && matchesText && matchesKind;
    });
  }, [items, search, catalogFilter]);

  const selectedCustomer = customers.find((customer) => customer.id === customerId) ?? null;
  const selectedIdentity = selectedCustomer?.fiscalIdentities.find((identity) => identity.active) ?? null;
  const total = cart.reduce((sum, line) => sum + line.quantity * line.unitPrice, 0);
  const canPrepare = cart.length > 0 && cart.every((line) => Number.isFinite(line.quantity) && line.quantity > 0 && Number.isFinite(line.unitPrice) && line.unitPrice >= 0);
  const canSaveDraft = canPrepare && saleBusy === null && (!sale || draftDirty);
  const canUseSavedDraft = sale !== null && !draftDirty && saleBusy === null;

  const markDraftChanged = () => {
    if (sale) setDraftDirty(true);
    setValidation(null);
    setPreview(null);
    setSaleError(null);
  };

  const addItem = (item: CommercialItemDto) => {
    markDraftChanged();
    setCart((current) => {
      const existing = current.find((line) => line.itemId === item.id);
      if (existing) return current.map((line) => line.itemId === item.id ? { ...line, quantity: line.quantity + 1 } : line);
      return [...current, { itemId: item.id, itemCode: item.code, itemName: item.name, quantity: 1, unitPrice: 0 }];
    });
  };

  const updateLine = (itemId: string, patch: Partial<SaleLineDraft>) => {
    markDraftChanged();
    setCart((current) => current.map((line) => line.itemId === itemId ? { ...line, ...patch } : line));
  };

  const removeLine = (itemId: string) => {
    markDraftChanged();
    setCart((current) => current.filter((line) => line.itemId !== itemId));
  };

  const commonSaleInput = () => ({
    intent,
    currencyCode: 'UYU',
    effectiveOn,
    lines: cart.map((line) => ({ itemId: line.itemId, quantity: line.quantity, unitPrice: line.unitPrice })),
    customerPartyId: customerId || null,
    deliveryCountry: deliveryCountry.trim() || null,
  });

  const saveDraft = async () => {
    if (!canSaveDraft) return;
    setSaleBusy('saving');
    setSaleError(null);
    try {
      let saved: SaleDto;
      if (sale) {
        saved = await gateways.sales.updateSaleDraft(sale.id, { expectedVersion: sale.version, ...commonSaleInput() });
      } else {
        const input: SaleCreateInput = { ...commonSaleInput(), locationId: null, terminalId: null };
        saved = await gateways.sales.createSale(input);
      }
      setSale(saved);
      setDraftDirty(false);
      setValidation(null);
      setPreview(null);
    } catch (error) {
      setSaleError(error instanceof Error ? error.message : 'No se pudo guardar el borrador mock.');
    } finally {
      setSaleBusy(null);
    }
  };

  const validateSale = async () => {
    if (!sale || draftDirty || saleBusy !== null) return;
    setSaleBusy('validating');
    setSaleError(null);
    try {
      const result = await gateways.sales.validateSale(sale.id, sale.version);
      setValidation(result);
      setPreview(result.preview);
      setSale(result.sale);
      setDraftDirty(false);
    } catch (error) {
      setSaleError(error instanceof Error ? error.message : 'No se pudo validar la venta mock.');
    } finally {
      setSaleBusy(null);
    }
  };

  const loadPreview = async () => {
    if (!sale || draftDirty || saleBusy !== null) return;
    setSaleBusy('preview');
    setSaleError(null);
    try {
      setPreview(await gateways.sales.getSaleFiscalPreview(sale.id));
    } catch (error) {
      setSaleError(error instanceof Error ? error.message : 'No se pudo cargar el preview fiscal mock.');
    } finally {
      setSaleBusy(null);
    }
  };

  const statusLabel = draftDirty
    ? 'CAMBIOS SIN GUARDAR'
    : validation
      ? validation.valid ? 'VALIDADO MOCK' : 'REQUIERE REVISIÓN'
      : sale ? 'BORRADOR MOCK' : 'BORRADOR LOCAL';

  return (
    <div className="pos-page">
      <div className="pos-toolbar">
        <div>
          <div className="pos-kicker">UI-POS-001 · v3-theme-pair</div>
          <h1 className="pos-heading">Productos</h1>
          <p className="pos-subheading">Selecciona un producto para agregarlo a la venta.</p>
        </div>
        <div className="pos-contract-note">
          <strong>Demo contractual:</strong> el catálogo no expone precio de venta. El precio se informa por línea y la fiscalidad sigue siendo autoridad del servidor.
        </div>
      </div>

      <div className="pos-layout">
        <section className="pos-panel">
          <div className="pos-panel-header">
            <div className="pos-search-row">
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Buscar en catálogo…"
                className="pos-search"
              />
              <div className="pos-counter">{loading ? 'Cargando…' : `${filtered.length} resultado${filtered.length === 1 ? '' : 's'}`}</div>
            </div>
            <div className="pos-filter-row" aria-label="Filtro de catálogo">
              {([
                ['ALL', 'Todos'],
                ['PRODUCT', 'Productos'],
                ['SERVICE', 'Servicios'],
              ] as const).map(([value, label]) => (
                <button key={value} type="button" onClick={() => setCatalogFilter(value)} className={`pos-filter-chip ${catalogFilter === value ? 'is-active' : ''}`}>
                  {label}
                </button>
              ))}
            </div>
          </div>

          <div className="pos-grid">
            {loading && Array.from({ length: 10 }).map((_, index) => (
              <div key={index} className="pos-product-card animate-pulse">
                <div className="pos-product-visual" />
                <div className="pos-product-body gap-2"><div className="h-2 w-1/2 rounded bg-slate-200" /><div className="h-3 w-3/4 rounded bg-slate-200" /><div className="mt-auto h-7 rounded bg-slate-200" /></div>
              </div>
            ))}

            {!loading && loadError && <div className="pos-empty text-rose-700">{loadError}</div>}

            {!loading && !loadError && filtered.length === 0 && (
              <div className="pos-empty"><strong>Sin resultados.</strong> Prueba otro código, nombre o filtro. Este estado no representa un error de API.</div>
            )}

            {!loading && !loadError && filtered.map((item) => (
              <article key={item.id} className="pos-product-card">
                <ProductVisual item={item} />
                <div className="pos-product-body">
                  <h2 className="pos-product-name">{item.name}</h2>
                  <div className="pos-product-code">Cód. {item.code}</div>
                  <div className="pos-product-meta">{item.kind === 'PRODUCT' ? (item.unit === 'KG' ? 'Kilogramo' : 'Unidad') : 'Servicio'}</div>
                  <button type="button" onClick={() => addItem(item)} className="pos-add-button">＋ Agregar</button>
                </div>
              </article>
            ))}
          </div>
        </section>

        <aside id="venta-actual" className="pos-panel pos-sale scroll-mt-20">
          <div className="pos-sale-head">
            <div>
              <div className="pos-sale-title">Venta actual</div>
              <div className="pos-sale-meta">{sale ? `Versión v${sale.version}` : 'Sin versión persistida'} · ciclo mock API-shaped</div>
            </div>
            <span className="pos-status">{statusLabel}</span>
          </div>

          <div className="pos-sale-form">
            <div className="pos-form-grid">
              <label className="pos-label">
                Intent comercial
                <select value={intent} onChange={(event) => { setIntent(event.target.value as SaleCommercialIntent); markDraftChanged(); }} className="pos-select">
                  <option value="CONSUMER_FINAL">Consumidor final</option>
                  <option value="TAXPAYER_INVOICE">Factura a contribuyente</option>
                  <option value="EXPORT">Exportación</option>
                </select>
              </label>
              <label className="pos-label">
                Cliente
                <select value={customerId} onChange={(event) => { setCustomerId(event.target.value); markDraftChanged(); }} className="pos-select">
                  <option value="">Consumidor final · sin identificar</option>
                  {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.name}</option>)}
                </select>
              </label>
            </div>

            {intent === 'EXPORT' && (
              <label className="pos-label mt-2 block">
                País de entrega
                <input value={deliveryCountry} onChange={(event) => { setDeliveryCountry(event.target.value.toUpperCase()); markDraftChanged(); }} maxLength={2} placeholder="Código ISO, ej. BR" className="pos-input" />
              </label>
            )}

            <div className="pos-client-note">
              {selectedCustomer ? (
                <><strong>{selectedCustomer.name}</strong>{selectedIdentity ? ` · ${selectedIdentity.typeCode}: ${selectedIdentity.number}` : ''} · residencia {selectedCustomer.residenceCountry}</>
              ) : (
                <><strong>Consumidor Final</strong> · sin cliente identificado · UYU · {effectiveOn}</>
              )}
              <div className="mt-1 opacity-70">Ubicación y terminal aún no integrados; el mock los mantiene en null.</div>
            </div>
          </div>

          <div className="pos-lines">
            {cart.length > 0 && (
              <div className="pos-line-header">
                <span>Producto</span><span>Cant.</span><span>Precio unit.</span><span>Subtotal</span><span />
              </div>
            )}
            {cart.length === 0 ? (
              <div className="pos-empty m-2">La venta está vacía. Agrega un producto o servicio; luego informa cantidad y precio unitario.</div>
            ) : cart.map((line) => {
              const item = items.find((candidate) => candidate.id === line.itemId);
              return (
                <div key={line.itemId} className="pos-line">
                  <div className="pos-line-product">
                    {item && <ProductVisual item={item} compact />}
                    <div className="min-w-0">
                      <div className="pos-line-name">{line.itemName}</div>
                      <div className="pos-line-code">{line.itemCode}</div>
                    </div>
                  </div>
                  <input aria-label={`Cantidad ${line.itemName}`} type="number" min="0.001" step="0.001" value={line.quantity} onChange={(event) => updateLine(line.itemId, { quantity: Number(event.target.value) })} className="pos-input" />
                  <input aria-label={`Precio unitario ${line.itemName}`} type="number" min="0" step="0.01" value={line.unitPrice} onChange={(event) => updateLine(line.itemId, { unitPrice: Number(event.target.value) })} className="pos-input" />
                  <div className="pos-line-total">{money.format(line.quantity * line.unitPrice)}</div>
                  <button type="button" onClick={() => removeLine(line.itemId)} className="pos-remove" aria-label={`Quitar ${line.itemName}`}>×</button>
                </div>
              );
            })}
          </div>

          <div className="pos-sale-footer">
            <div className="pos-total-row">
              <div>
                <div className="pos-total-label">Neto informado</div>
                <div className="text-[9px] opacity-50">Sin cálculo fiscal autoritativo en cliente</div>
              </div>
              <strong className="pos-total-value">{money.format(total)}</strong>
            </div>

            <div className="pos-preview">
              <div className="pos-preview-title">Vista previa fiscal</div>
              <div className="pos-preview-copy">
                {preview ? 'El tratamiento impositivo y la selección CFE permanecen sujetos al resultado representado por el contrato.' : 'El tratamiento impositivo y la selección CFE requieren revisión. Guarda el borrador para consultar el preview mock.'}
              </div>
              {preview && (
                <>
                  <div className="pos-preview-grid">
                    <PreviewValue label="Neto" value={money.format(preview.netAmount)} />
                    <PreviewValue label="Impuestos" value={preview.previewTaxAmount === null ? 'No resueltos' : money.format(preview.previewTaxAmount)} />
                    <PreviewValue label="Total" value={preview.previewTotalAmount === null ? 'No resuelto' : money.format(preview.previewTotalAmount)} />
                    <PreviewValue label="CFE" value={preview.cfe.selectionStatus} />
                  </div>
                  {preview.findings.length > 0 && <div className="pos-preview-copy mt-2">{preview.findings.join(' · ')}</div>}
                </>
              )}
            </div>

            <div className="pos-actions pos-actions-three">
              <button type="button" onClick={saveDraft} disabled={!canSaveDraft} className="pos-action-secondary">
                {saleBusy === 'saving' ? 'Guardando…' : sale ? (draftDirty ? 'Actualizar borrador' : 'Borrador guardado') : 'Guardar borrador'}
              </button>
              <button type="button" onClick={loadPreview} disabled={!canUseSavedDraft} className="pos-action-secondary">
                {saleBusy === 'preview' ? 'Cargando…' : 'Ver preview fiscal'}
              </button>
              <button type="button" onClick={validateSale} disabled={!canUseSavedDraft} className="pos-action-primary">
                {saleBusy === 'validating' ? 'Validando…' : '✓ Validar'}
              </button>
            </div>

            <div aria-live="polite">
              {saleError && <div className="pos-message error">{saleError}</div>}
              {sale && !draftDirty && <div className="pos-message info">Borrador mock guardado · versión {sale.version}. Vive solo en memoria del navegador; no fue persistido por la API real.</div>}
              {validation && <div className="pos-message info"><strong>{validation.valid ? 'Validación mock completada.' : 'Validación mock con hallazgos.'}</strong> Representa la forma de `API-SAL-005`, no una ejecución del backend desplegado.</div>}
            </div>

            <div className="mt-2 text-[9px] leading-4 opacity-55">
              `API-SAL-007 confirmSale` no se habilita aquí: aún falta una fuente gobernada de settlement/medios de pago. No se afirma aceptación DGI.
            </div>
          </div>
        </aside>
      </div>

      {cart.length > 0 && (
        <a href="#venta-actual" className="pos-mobile-summary fixed inset-x-3 bottom-3 z-30 flex items-center justify-between rounded-xl px-3 py-2.5 shadow-2xl lg:hidden">
          <span><span className="block text-[10px] opacity-65">{cart.length} línea{cart.length === 1 ? '' : 's'} en venta</span><strong className="text-base">{money.format(total)}</strong></span>
          <span className="rounded-lg bg-white px-2.5 py-1.5 text-[11px] font-bold text-slate-950">Ver venta</span>
        </a>
      )}
    </div>
  );
}
