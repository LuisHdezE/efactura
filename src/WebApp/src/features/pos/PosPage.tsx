import { useEffect, useMemo, useState } from 'react';
import type { CommercialItemDto, PartyDto, SaleLineDraft } from '../../contracts/api';
import { gateways } from '../../services';
import { getItemVisualMetadata } from './itemVisualMetadata';

const money = new Intl.NumberFormat('es-UY', { style: 'currency', currency: 'UYU' });

function ProductVisual({ item, compact = false }: { item: CommercialItemDto; compact?: boolean }) {
  const visual = getItemVisualMetadata(item.id);

  if (visual.imageUrl) {
    return (
      <img
        src={visual.imageUrl}
        alt={visual.alt}
        className={compact ? 'h-12 w-12 rounded-xl object-cover' : 'h-full w-full object-cover transition duration-300 group-hover:scale-[1.02]'}
      />
    );
  }

  return (
    <div className={compact ? 'flex h-12 w-12 items-center justify-center rounded-xl bg-slate-100 text-slate-500' : 'flex h-full w-full flex-col items-center justify-center bg-slate-100 px-4 text-center text-slate-500'}>
      <svg viewBox="0 0 24 24" aria-hidden="true" className={compact ? 'h-5 w-5' : 'mb-2 h-9 w-9'} fill="none" stroke="currentColor" strokeWidth="1.7">
        <path d="M4 7.5 12 3l8 4.5v9L12 21l-8-4.5v-9Z" />
        <path d="m4.5 7.8 7.5 4.1 7.5-4.1M12 12v9" />
      </svg>
      {!compact && <span className="text-xs font-semibold">Visual de demo no disponible</span>}
    </div>
  );
}

export function PosPage() {
  const [items, setItems] = useState<CommercialItemDto[]>([]);
  const [customers, setCustomers] = useState<PartyDto[]>([]);
  const [search, setSearch] = useState('');
  const [customerId, setCustomerId] = useState('');
  const [cart, setCart] = useState<SaleLineDraft[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [draftPrepared, setDraftPrepared] = useState(false);

  useEffect(() => {
    Promise.all([gateways.catalog.listItems(), gateways.parties.listCustomers()])
      .then(([itemPage, partyPage]) => {
        setItems(itemPage.items);
        setCustomers(partyPage.items);
      })
      .catch(() => setLoadError('No se pudo cargar el catálogo o los clientes de demostración.'))
      .finally(() => setLoading(false));
  }, []);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return items.filter((item) => item.active && (!term || item.code.toLowerCase().includes(term) || item.name.toLowerCase().includes(term)));
  }, [items, search]);

  const selectedCustomer = customers.find((customer) => customer.id === customerId) ?? null;
  const total = cart.reduce((sum, line) => sum + line.quantity * line.unitPrice, 0);
  const canPrepare = cart.length > 0 && cart.every((line) => Number.isFinite(line.quantity) && line.quantity > 0 && Number.isFinite(line.unitPrice) && line.unitPrice >= 0);

  const addItem = (item: CommercialItemDto) => {
    setDraftPrepared(false);
    setCart((current) => {
      const existing = current.find((line) => line.itemId === item.id);
      if (existing) return current.map((line) => line.itemId === item.id ? { ...line, quantity: line.quantity + 1 } : line);
      return [...current, { itemId: item.id, itemCode: item.code, itemName: item.name, quantity: 1, unitPrice: 0 }];
    });
  };

  const updateLine = (itemId: string, patch: Partial<SaleLineDraft>) => {
    setDraftPrepared(false);
    setCart((current) => current.map((line) => line.itemId === itemId ? { ...line, ...patch } : line));
  };

  const removeLine = (itemId: string) => {
    setDraftPrepared(false);
    setCart((current) => current.filter((line) => line.itemId !== itemId));
  };

  return (
    <div className="p-4 pb-28 sm:p-6 sm:pb-28 lg:p-8 xl:pb-8">
      <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <span className="text-xs font-semibold uppercase tracking-[0.2em] text-blue-600">UI-POS-001</span>
            <span className="rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold uppercase tracking-wide text-amber-700 ring-1 ring-inset ring-amber-200">Demo mock</span>
          </div>
          <h1 className="text-3xl font-bold tracking-tight text-slate-950 sm:text-4xl">Punto de Venta</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-500">Construye una venta con productos o servicios usando datos de demostración alineados con los contratos actuales.</p>
        </div>
        <div className="max-w-xl rounded-2xl border border-blue-100 bg-blue-50/80 px-4 py-3 text-sm leading-5 text-blue-900">
          <strong className="font-semibold">Regla contractual:</strong> el catálogo no aporta precio de venta. El precio se informa en cada línea; impuestos y fiscalidad siguen siendo autoridad del servidor.
        </div>
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.45fr)_minmax(390px,0.8fr)]">
        <section className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
          <div className="border-b border-slate-200 p-4 sm:p-5">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
              <label className="block flex-1">
                <span className="mb-2 block text-xs font-semibold uppercase tracking-wide text-slate-500">Buscar artículo o servicio</span>
                <input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Código o nombre…"
                  className="w-full rounded-xl border border-slate-300 bg-slate-50 px-4 py-3 text-sm outline-none transition focus:border-blue-500 focus:bg-white focus:ring-4 focus:ring-blue-50"
                />
              </label>
              <div className="text-xs font-medium text-slate-500">{loading ? 'Cargando…' : `${filtered.length} resultado${filtered.length === 1 ? '' : 's'}`}</div>
            </div>
          </div>

          <div className="grid gap-4 p-4 sm:grid-cols-2 sm:p-5 lg:grid-cols-3">
            {loading && Array.from({ length: 6 }).map((_, index) => (
              <div key={index} className="overflow-hidden rounded-2xl border border-slate-200">
                <div className="aspect-[4/3] animate-pulse bg-slate-100" />
                <div className="space-y-3 p-4"><div className="h-3 w-20 animate-pulse rounded bg-slate-100" /><div className="h-5 w-3/4 animate-pulse rounded bg-slate-100" /><div className="h-10 animate-pulse rounded-xl bg-slate-100" /></div>
              </div>
            ))}

            {!loading && loadError && (
              <div className="col-span-full rounded-2xl border border-rose-200 bg-rose-50 p-6 text-center text-sm text-rose-800">{loadError}</div>
            )}

            {!loading && !loadError && filtered.length === 0 && (
              <div className="col-span-full rounded-2xl border border-dashed border-slate-300 bg-slate-50 px-6 py-14 text-center">
                <div className="text-base font-semibold text-slate-800">Sin resultados</div>
                <p className="mt-1 text-sm text-slate-500">Prueba otro código o nombre. Este estado no representa un error de API.</p>
              </div>
            )}

            {!loading && !loadError && filtered.map((item) => (
              <article key={item.id} className="group flex min-h-full flex-col overflow-hidden rounded-2xl border border-slate-200 bg-white transition hover:-translate-y-0.5 hover:border-blue-300 hover:shadow-lg hover:shadow-slate-200/60">
                <div className="relative aspect-[4/3] overflow-hidden bg-slate-100">
                  <ProductVisual item={item} />
                  <span className="absolute left-3 top-3 rounded-full bg-white/90 px-2.5 py-1 text-[11px] font-bold uppercase tracking-wide text-slate-700 shadow-sm backdrop-blur">{item.kind === 'PRODUCT' ? 'Producto' : 'Servicio'}</span>
                </div>
                <div className="flex flex-1 flex-col p-4">
                  <div className="mb-2 flex items-center justify-between gap-3 text-xs text-slate-500"><span className="font-semibold text-slate-700">{item.code}</span><span>Unidad {item.unit}</span></div>
                  <h2 className="font-semibold leading-5 text-slate-950">{item.name}</h2>
                  {item.description && <p className="mt-1 line-clamp-2 text-sm leading-5 text-slate-500">{item.description}</p>}
                  <button onClick={() => addItem(item)} className="mt-4 rounded-xl bg-blue-600 px-3 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 focus:outline-none focus:ring-4 focus:ring-blue-100">Agregar a venta</button>
                </div>
              </article>
            ))}
          </div>
        </section>

        <aside id="venta-actual" className="h-fit scroll-mt-24 overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm xl:sticky xl:top-24">
          <div className="border-b border-slate-200 p-5">
            <div className="flex items-start justify-between gap-3">
              <div><h2 className="text-xl font-bold text-slate-950">Venta actual</h2><p className="mt-0.5 text-sm text-slate-500">Borrador local de demostración</p></div>
              <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700">BORRADOR</span>
            </div>

            <label className="mt-5 block text-xs font-semibold uppercase tracking-wide text-slate-500">Cliente</label>
            <select
              value={customerId}
              onChange={(event) => { setCustomerId(event.target.value); setDraftPrepared(false); }}
              className="mt-1 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-50"
            >
              <option value="">Sin cliente seleccionado</option>
              {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.name}</option>)}
            </select>
            {selectedCustomer && <p className="mt-2 text-xs leading-5 text-slate-500">{selectedCustomer.kind === 'ORGANIZATION' ? 'Organización' : 'Persona'} · residencia {selectedCustomer.residenceCountry}</p>}
          </div>

          <div className="divide-y divide-slate-100">
            {cart.length === 0 ? (
              <div className="px-6 py-10 text-center">
                <div className="text-sm font-semibold text-slate-800">La venta está vacía</div>
                <p className="mt-1 text-sm leading-5 text-slate-500">Agrega al menos un producto o servicio. El precio unitario se captura después en cada línea.</p>
              </div>
            ) : cart.map((line) => {
              const item = items.find((candidate) => candidate.id === line.itemId);
              return (
                <div key={line.itemId} className="p-4 sm:p-5">
                  <div className="flex items-start gap-3">
                    {item && <div className="shrink-0 overflow-hidden rounded-xl ring-1 ring-slate-200"><ProductVisual item={item} compact /></div>}
                    <div className="min-w-0 flex-1"><div className="truncate font-semibold text-slate-900">{line.itemName}</div><div className="text-xs text-slate-500">{line.itemCode}</div></div>
                    <button onClick={() => removeLine(line.itemId)} className="text-sm font-medium text-rose-600 hover:text-rose-700">Quitar</button>
                  </div>
                  <div className="mt-3 grid grid-cols-2 gap-3">
                    <label className="text-xs font-medium text-slate-500">Cantidad<input type="number" min="0.001" step="0.001" value={line.quantity} onChange={(event) => updateLine(line.itemId, { quantity: Number(event.target.value) })} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none focus:border-blue-500" /></label>
                    <label className="text-xs font-medium text-slate-500">Precio unitario<input type="number" min="0" step="0.01" value={line.unitPrice} onChange={(event) => updateLine(line.itemId, { unitPrice: Number(event.target.value) })} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none focus:border-blue-500" /></label>
                  </div>
                  <div className="mt-2 text-right text-xs text-slate-500">Neto de línea: <strong className="text-slate-700">{money.format(line.quantity * line.unitPrice)}</strong></div>
                </div>
              );
            })}
          </div>

          <div className="border-t border-slate-200 bg-slate-50/70 p-5">
            <div className="flex items-end justify-between gap-4"><div><span className="text-sm font-medium text-slate-600">Neto informado</span><p className="mt-0.5 text-xs text-slate-400">Sin cálculo fiscal autoritativo en cliente</p></div><strong className="text-2xl tracking-tight text-slate-950">{money.format(total)}</strong></div>

            {draftPrepared && (
              <div className="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 px-3 py-3 text-sm leading-5 text-emerald-800">
                Borrador mock preparado con {cart.length} línea{cart.length === 1 ? '' : 's'}. No fue persistido ni fiscalizado.
              </div>
            )}

            <button onClick={() => setDraftPrepared(true)} disabled={!canPrepare} className="mt-4 w-full rounded-xl bg-blue-600 px-4 py-3 font-semibold text-white transition enabled:hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-slate-300">Preparar borrador</button>
            <p className="mt-3 text-xs leading-5 text-slate-500">Este incremento termina en preparación local. Validación y preview fiscal corresponden a `API-SAL-005/006`; confirmación durable corresponde a `API-SAL-007`.</p>
          </div>
        </aside>
      </div>

      {cart.length > 0 && (
        <a href="#venta-actual" className="fixed inset-x-4 bottom-4 z-20 flex items-center justify-between rounded-2xl border border-slate-200 bg-slate-950 px-4 py-3 text-white shadow-2xl shadow-slate-900/20 xl:hidden">
          <span><span className="block text-xs text-slate-300">{cart.length} línea{cart.length === 1 ? '' : 's'} en venta</span><strong className="text-lg">{money.format(total)}</strong></span>
          <span className="rounded-xl bg-white px-3 py-2 text-sm font-semibold text-slate-950">Ver venta</span>
        </a>
      )}
    </div>
  );
}
