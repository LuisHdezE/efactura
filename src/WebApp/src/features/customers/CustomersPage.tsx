import { useEffect, useMemo, useState } from 'react';
import type { PartyDto } from '../../contracts/api';
import { gateways } from '../../services';

export function CustomersPage() {
  const [customers, setCustomers] = useState<PartyDto[]>([]);
  const [search, setSearch] = useState('');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    gateways.parties.listCustomers().then((page) => {
      setCustomers(page.items);
      setSelectedId(page.items[0]?.id ?? null);
    }).finally(() => setLoading(false));
  }, []);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return customers.filter((party) => !term || party.name.toLowerCase().includes(term) || party.fiscalIdentities.some((identity) => identity.number.toLowerCase().includes(term)));
  }, [customers, search]);
  const selected = customers.find((party) => party.id === selectedId) ?? filtered[0] ?? null;

  return (
    <div className="p-4 sm:p-6 lg:p-8">
      <div className="mb-6"><p className="text-xs font-semibold uppercase tracking-[0.2em] text-blue-600">UI-CUSTOMER-001</p><h1 className="text-3xl font-bold text-slate-950">Clientes</h1><p className="mt-1 text-sm text-slate-500">Party master con rol CUSTOMER. Sin saldos, aging ni crédito inventados.</p></div>
      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.35fr)_minmax(390px,0.75fr)]">
        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="border-b border-slate-200 p-4 sm:p-5"><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar por nombre o identificación fiscal..." className="w-full rounded-xl border border-slate-300 bg-slate-50 px-4 py-3 outline-none focus:border-blue-500 focus:bg-white" /></div>
          <div className="overflow-x-auto"><table className="w-full min-w-[720px] text-left text-sm"><thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500"><tr><th className="px-5 py-3">Nombre</th><th className="px-4 py-3">Tipo</th><th className="px-4 py-3">Identidad fiscal</th><th className="px-4 py-3">Contacto</th><th className="px-4 py-3">Roles</th></tr></thead><tbody className="divide-y divide-slate-100">{loading ? <tr><td colSpan={5} className="px-5 py-10 text-center text-slate-500">Cargando clientes…</td></tr> : filtered.map((party) => { const identity = party.fiscalIdentities.find((item) => item.active) ?? party.fiscalIdentities[0]; const contact = party.contacts.find((item) => item.primary) ?? party.contacts[0]; const active = party.id === selected?.id; return <tr key={party.id} onClick={() => setSelectedId(party.id)} className={`cursor-pointer transition ${active ? 'bg-blue-50' : 'hover:bg-slate-50'}`}><td className="px-5 py-4"><div className="font-semibold text-slate-900">{party.name}</div><div className="text-xs text-slate-500">{party.active ? 'Activo' : 'Inactivo'}</div></td><td className="px-4 py-4"><span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold">{party.kind}</span></td><td className="px-4 py-4">{identity ? <><div className="font-medium">{identity.typeCode} {identity.number}</div><div className="text-xs text-slate-500">{identity.issuingCountry}</div></> : <span className="text-slate-400">Sin identidad</span>}</td><td className="px-4 py-4">{contact?.value ?? 'Sin contacto'}</td><td className="px-4 py-4"><div className="flex flex-wrap gap-1">{party.roles.map((role) => <span key={role} className="rounded-full bg-blue-100 px-2 py-1 text-xs font-semibold text-blue-700">{role}</span>)}</div></td></tr>; })}</tbody></table></div>
        </section>

        <aside className="h-fit rounded-2xl border border-slate-200 bg-white p-5 shadow-sm xl:sticky xl:top-24">
          {!selected ? <div className="py-12 text-center text-sm text-slate-500">Selecciona un cliente.</div> : <><div className="flex items-start justify-between gap-4 border-b border-slate-200 pb-4"><div><h2 className="text-xl font-bold">{selected.name}</h2><p className="mt-1 text-sm text-slate-500">{selected.kind} · versión {selected.version}</p></div><span className="rounded-full bg-emerald-100 px-3 py-1 text-xs font-semibold text-emerald-700">{selected.active ? 'Activo' : 'Inactivo'}</span></div>
            <section className="border-b border-slate-200 py-4"><h3 className="mb-3 font-semibold">Resumen</h3><dl className="grid grid-cols-2 gap-x-4 gap-y-3 text-sm"><dt className="text-slate-500">Residencia</dt><dd className="font-medium">{selected.residenceCountry}</dd><dt className="text-slate-500">Residencia fiscal</dt><dd className="font-medium">{selected.taxResidenceCountry}</dd><dt className="text-slate-500">Roles</dt><dd className="font-medium">{selected.roles.join(', ')}</dd></dl></section>
            <section className="border-b border-slate-200 py-4"><h3 className="mb-3 font-semibold">Identidades fiscales</h3>{selected.fiscalIdentities.length === 0 ? <p className="text-sm text-slate-500">Sin identidades registradas.</p> : <div className="space-y-2">{selected.fiscalIdentities.map((identity) => <div key={identity.id} className="rounded-xl bg-slate-50 p-3 text-sm"><div className="font-semibold">{identity.typeCode} · {identity.number}</div><div className="mt-1 text-slate-500">País emisor: {identity.issuingCountry} · {identity.active ? 'Activa' : 'Inactiva'}</div></div>)}</div>}</section>
            <section className="py-4"><h3 className="mb-3 font-semibold">Direcciones y contactos</h3><div className="space-y-2">{selected.addresses.map((address) => <div key={address.id} className="rounded-xl border border-slate-200 p-3 text-sm"><div className="font-semibold">{address.kind}{address.primary ? ' · Principal' : ''}</div><div className="mt-1 text-slate-500">{address.addressLine}, {address.city}</div></div>)}{selected.contacts.map((contact) => <div key={contact.id} className="rounded-xl border border-slate-200 p-3 text-sm"><div className="font-semibold">{contact.typeCode}{contact.primary ? ' · Principal' : ''}</div><div className="mt-1 text-slate-500">{contact.value}</div></div>)}</div></section></>}
        </aside>
      </div>
    </div>
  );
}
