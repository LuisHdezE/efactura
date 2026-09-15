import { useEffect, useMemo, useState } from 'react';
import type { CommercialItemDto, PartyDto, SaleLineDraft } from '../../contracts/api';
import { gateways } from '../../services';

const money = new Intl.NumberFormat('es-UY', { style: 'currency', currency: 'UYU' });

export function PosPage() {
  const [items, setItems] = useState<CommercialItemDto[]>([]);
  const [customers, setCustomers] = useState<PartyDto[]>([]);
  const [search, setSearch] = useState('');
  const [customerId, setCustomerId] = useState('');
  const [cart, setCart] = useState<SaleLineDraft[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([gateways.catalog.listItems(), gateways.parties.listCustomers()])
      .then(([itemPage, partyPage]) => { setItems(itemPage.items); setCustomers(partyPage.items); })
      .finally(() => setLoading(false));
  }, []);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return items.filter((item) => !term || item.code.toLowerCase().includes(term) || item.name.toLowerCase().includes(term));
  }, [items, search]);

  const total = cart.reduce((sum, line) => sum + line.quantity * line.unitPrice, 0);

  const addItem = (item: CommercialItemDto) => {
    setCart((current) => {
      const existing = current.find((line) => line.itemId === item.id);
      if (existing) return current.map((line) => line.itemId === item.id ? { ...line, quantity: line.quantity + 1 } : line);
      return [...current, { itemId: item.id, itemCode: item.code, itemName: item.name, quantity: 1, unitPrice: 0 }];
    });
  };

  const updateLine = (itemId: string, patch: Partial<SaleLineDraft>) => setCart((current) => current.map((line) => line.itemId === itemId ? { ...line, ...patch } : line));
  const removeLine = (itemId: string) => setCart((current) => current.filter((line) => line.itemId !== itemId));

  return (
    <div className="p-4 sm:p-6 lg:p-8">
      <div className="mb-6 flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
        <div><p className="text-xs font-semibold uppercase tracking-[0.2em] text-blue-600">UI-POS-001</p><h1 className="text-3xl font-bold text-slate-950">Punto de Venta</h1><p className="mt-1 text-sm text-slate-500">Datos mock, estructura y acciones alineadas con la API v1 actual.</p></div>
        <div className="rounded-lg border border-blue-200 bg-blue-50 px-3 py-2 text-xs text-blue-800">El catálogo no aporta precio de venta. El precio se informa en cada línea.</div>
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.4fr)_minmax(390px,0.8fr)]">
        <section className="rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="border-b border-slate-200 p-4 sm:p-5">
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar por código o nombre..." className="w-full rounded-xl border border-slate-300 bg-slate-50 px-4 py-3 outline-none transition focus:border-blue-500 focus:bg-white" />
          </div>
          <div className="grid gap-3 p-4 sm:grid-cols-2 sm:p-5 lg:grid-cols-3">
            {loading ? <div className="col-span-full py-12 text-center text-slate-500">Cargando catálogo…</div> : filtered.map((item) => (
              <article key={item.id} className="flex min-h-44 flex-col rounded-xl border border-slate-200 p-4 transition hover:border-blue-300 hover:shadow-sm">
                <div className="mb-3 flex items-start justify-between gap-2"><span className="rounded-md bg-slate-100 px-2 py-1 text-xs font-semibold text-slate-600">{item.code}</span><span className="text-xs text-slate-400">{item.kind === 'PRODUCT' ? 'Producto' : 'Servicio'}</span></div>
                <h2 className="font-semibold text-slate-900">{item.name}</h2><p className="mt-1 text-sm text-slate-500">Unidad: {item.unit}</p>
                <button onClick={() => addItem(item)} className="mt-auto rounded-lg bg-blue-600 px-3 py-2 text-sm font-semibold text-white hover:bg-blue-700">Agregar a venta</button>
              </article>
            ))}
          </div>
        </section>

        <aside className="h-fit rounded-2xl border border-slate-200 bg-white shadow-sm xl:sticky xl:top-24">
          <div className="border-b border-slate-200 p-5"><div className="flex items-center justify-between"><div><h2 className="text-xl font-bold">Venta actual</h2><p className="text-sm text-slate-500">Borrador local de demostración</p></div><span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold">BORRADOR</span></div>
            <label className="mt-4 block text-xs font-semibold uppercase tracking-wide text-slate-500">Cliente</label>
            <select value={customerId} onChange={(event) => setCustomerId(event.target.value)} className="mt-1 w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-sm"><option value="">Consumidor final / sin Party</option>{customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.name}</option>)}</select>
          </div>
          <div className="divide-y divide-slate-100">
            {cart.length === 0 ? <div className="p-8 text-center text-sm text-slate-500">Agrega un producto o servicio. El precio se solicitará en la línea de venta.</div> : cart.map((line) => (
              <div key={line.itemId} className="p-4"><div className="flex items-start justify-between gap-3"><div><div className="font-semibold">{line.itemName}</div><div className="text-xs text-slate-500">{line.itemCode}</div></div><button onClick={() => removeLine(line.itemId)} className="text-sm font-medium text-rose-600">Quitar</button></div>
                <div className="mt-3 grid grid-cols-2 gap-3"><label className="text-xs text-slate-500">Cantidad<input type="number" min="0.001" step="0.001" value={line.quantity} onChange={(event) => updateLine(line.itemId, { quantity: Number(event.target.value) })} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm" /></label><label className="text-xs text-slate-500">Precio unitario<input type="number" min="0" step="0.01" value={line.unitPrice} onChange={(event) => updateLine(line.itemId, { unitPrice: Number(event.target.value) })} className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm" /></label></div>
              </div>
            ))}
          </div>
          <div className="border-t border-slate-200 p-5"><div className="mb-4 flex items-center justify-between text-lg"><span className="font-semibold">Neto informado</span><strong>{money.format(total)}</strong></div><button disabled={cart.length === 0 || cart.some((line) => line.unitPrice <= 0)} className="w-full rounded-xl bg-blue-600 px-4 py-3 font-semibold text-white enabled:hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-slate-300">Preparar borrador</button><p className="mt-3 text-xs leading-5 text-slate-500">La demo no calcula impuestos en cliente ni afirma aceptación DGI. La vista previa fiscal pertenece a `API-SAL-006` y se integrará en la etapa API.</p></div>
        </aside>
      </div>
    </div>
  );
}
