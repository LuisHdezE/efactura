import { useEffect, useMemo, useState } from 'react';
import type { PartyDto } from '../../contracts/api';
import { gateways } from '../../services';
import './suppliers.css';

type DetailTab = 'summary' | 'fiscal' | 'contacts';
type StatusFilter = 'ALL' | 'ACTIVE' | 'INACTIVE';

const countryNames: Record<string, string> = {
  UY: 'Uruguay',
  CN: 'China',
};

function initials(name: string) {
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]?.toUpperCase()).join('') || 'PR';
}

function primaryIdentity(party: PartyDto) {
  return party.fiscalIdentities.find((identity) => identity.active) ?? party.fiscalIdentities[0] ?? null;
}

function primaryContact(party: PartyDto) {
  return party.contacts.find((contact) => contact.primary) ?? party.contacts[0] ?? null;
}

function countryLabel(code: string) {
  return countryNames[code] ?? code;
}

function kindLabel(kind: PartyDto['kind']) {
  return kind === 'ORGANIZATION' ? 'Empresa' : 'Persona';
}

function addressKindLabel(kind: PartyDto['addresses'][number]['kind']) {
  if (kind === 'FISCAL') return 'Fiscal';
  if (kind === 'DELIVERY') return 'Entrega';
  return 'Otra';
}

export function SuppliersPage() {
  const [suppliers, setSuppliers] = useState<PartyDto[]>([]);
  const [search, setSearch] = useState('');
  const [country, setCountry] = useState('ALL');
  const [status, setStatus] = useState<StatusFilter>('ALL');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<DetailTab>('summary');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;
    gateways.parties.listSuppliers()
      .then((page) => {
        if (!mounted) return;
        setSuppliers(page.items);
        setSelectedId(page.items[0]?.id ?? null);
        setError(null);
      })
      .catch(() => {
        if (mounted) setError('No pudimos cargar los proveedores de demostración.');
      })
      .finally(() => {
        if (mounted) setLoading(false);
      });

    return () => {
      mounted = false;
    };
  }, []);

  const countries = useMemo(
    () => Array.from(new Set(suppliers.map((supplier) => supplier.taxResidenceCountry))).sort(),
    [suppliers]
  );

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return suppliers.filter((party) => {
      if (country !== 'ALL' && party.taxResidenceCountry !== country) return false;
      if (status === 'ACTIVE' && !party.active) return false;
      if (status === 'INACTIVE' && party.active) return false;
      if (!term) return true;

      return (
        party.name.toLowerCase().includes(term) ||
        party.fiscalIdentities.some((identity) => identity.number.toLowerCase().includes(term)) ||
        party.contacts.some((contact) => contact.value.toLowerCase().includes(term))
      );
    });
  }, [suppliers, search, country, status]);

  const selected = filtered.find((party) => party.id === selectedId) ?? filtered[0] ?? null;

  return (
    <div className="supplier-page">
      <header className="supplier-heading-row">
        <div>
          <div className="supplier-breadcrumb">Comercial <span aria-hidden="true">›</span> Proveedores</div>
          <h1>Proveedores</h1>
          <p>Maestro de terceros con rol proveedor para bienes y servicios.</p>
        </div>
        <button
          className="supplier-primary-action"
          type="button"
          disabled
          title="La escritura permanece deshabilitada hasta habilitar la integración gobernada del WebApp."
        >
          <span aria-hidden="true">＋</span>
          Nuevo proveedor
        </button>
      </header>

      <div className="supplier-layout">
        <section className="supplier-panel supplier-directory" aria-label="Directorio de proveedores">
          <div className="supplier-filters">
            <label className="supplier-search">
              <span aria-hidden="true">⌕</span>
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Buscar por nombre, RUT o contacto…"
                aria-label="Buscar proveedores"
              />
            </label>

            <label className="supplier-select-wrap">
              <span className="sr-only">País fiscal</span>
              <select value={country} onChange={(event) => setCountry(event.target.value)}>
                <option value="ALL">Todos los países</option>
                {countries.map((code) => <option key={code} value={code}>{countryLabel(code)}</option>)}
              </select>
            </label>

            <label className="supplier-select-wrap">
              <span className="sr-only">Estado</span>
              <select value={status} onChange={(event) => setStatus(event.target.value as StatusFilter)}>
                <option value="ALL">Todos los estados</option>
                <option value="ACTIVE">Activos</option>
                <option value="INACTIVE">Inactivos</option>
              </select>
            </label>

            <div className="supplier-count" aria-live="polite">
              <strong>{filtered.length}</strong>
              <span>{filtered.length === 1 ? 'proveedor' : 'proveedores'}</span>
            </div>
          </div>

          <div className="supplier-table-wrap">
            <table className="supplier-table">
              <thead>
                <tr>
                  <th>Nombre / Razón social</th>
                  <th>RUT / Documento</th>
                  <th>País</th>
                  <th>Contacto principal</th>
                  <th>Estado</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={5}><div className="supplier-state">Cargando proveedores…</div></td></tr>
                ) : error ? (
                  <tr><td colSpan={5}><div className="supplier-state is-error">{error}</div></td></tr>
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={5}><div className="supplier-state"><strong>Sin coincidencias</strong><span>Probá otros filtros o una búsqueda diferente.</span></div></td></tr>
                ) : filtered.map((party) => {
                  const identity = primaryIdentity(party);
                  const contact = primaryContact(party);
                  const isSelected = selected?.id === party.id;

                  return (
                    <tr
                      key={party.id}
                      className={isSelected ? 'is-selected' : undefined}
                      tabIndex={0}
                      aria-selected={isSelected}
                      onClick={() => { setSelectedId(party.id); setActiveTab('summary'); }}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' || event.key === ' ') {
                          event.preventDefault();
                          setSelectedId(party.id);
                          setActiveTab('summary');
                        }
                      }}
                    >
                      <td>
                        <div className="supplier-name-cell">
                          <span className="supplier-avatar" aria-hidden="true">{initials(party.name)}</span>
                          <span><strong>{party.name}</strong><small>{kindLabel(party.kind)}</small></span>
                        </div>
                      </td>
                      <td>{identity ? <span className="supplier-document"><strong>{identity.number}</strong><small>{identity.typeCode}</small></span> : <span className="supplier-muted">Sin identificación</span>}</td>
                      <td><span className="supplier-country"><strong>{party.taxResidenceCountry}</strong><small>{countryLabel(party.taxResidenceCountry)}</small></span></td>
                      <td>{contact ? <span className="supplier-contact"><strong>{contact.value}</strong><small>{contact.typeCode}</small></span> : <span className="supplier-muted">Sin contacto</span>}</td>
                      <td><span className={`supplier-status ${party.active ? 'is-active' : 'is-inactive'}`}>{party.active ? 'Activo' : 'Inactivo'}</span></td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>

        <aside className="supplier-panel supplier-detail" aria-label="Detalle del proveedor">
          {!selected ? (
            <div className="supplier-empty-detail"><strong>Seleccioná un proveedor</strong><span>Su información aparecerá aquí.</span></div>
          ) : (
            <>
              <div className="supplier-detail-head">
                <div className="supplier-detail-party">
                  <span className="supplier-avatar is-large" aria-hidden="true">{initials(selected.name)}</span>
                  <div>
                    <h2>{selected.name}</h2>
                    <div className="supplier-detail-meta">
                      <span>{kindLabel(selected.kind)}</span>
                      {selected.roles.includes('CUSTOMER') && <span className="supplier-role-badge">Cliente + Proveedor</span>}
                    </div>
                  </div>
                </div>
                <span className={`supplier-status ${selected.active ? 'is-active' : 'is-inactive'}`}>{selected.active ? 'Activo' : 'Inactivo'}</span>
              </div>

              <div className="supplier-tabs" role="tablist" aria-label="Secciones del proveedor">
                <button type="button" role="tab" aria-selected={activeTab === 'summary'} className={activeTab === 'summary' ? 'is-active' : ''} onClick={() => setActiveTab('summary')}>Información</button>
                <button type="button" role="tab" aria-selected={activeTab === 'fiscal'} className={activeTab === 'fiscal' ? 'is-active' : ''} onClick={() => setActiveTab('fiscal')}>Identidades fiscales</button>
                <button type="button" role="tab" aria-selected={activeTab === 'contacts'} className={activeTab === 'contacts' ? 'is-active' : ''} onClick={() => setActiveTab('contacts')}>Direcciones y contactos</button>
              </div>

              {activeTab === 'summary' && (
                <div className="supplier-detail-body">
                  <dl className="supplier-summary-grid">
                    <div><dt>Tipo</dt><dd>{kindLabel(selected.kind)}</dd></div>
                    <div><dt>Versión</dt><dd>{selected.version}</dd></div>
                    <div><dt>Residencia</dt><dd>{countryLabel(selected.residenceCountry)}</dd></div>
                    <div><dt>Residencia fiscal</dt><dd>{countryLabel(selected.taxResidenceCountry)}</dd></div>
                    <div><dt>Identidades fiscales</dt><dd>{selected.fiscalIdentities.length}</dd></div>
                    <div><dt>Contactos</dt><dd>{selected.contacts.length}</dd></div>
                  </dl>

                  <div className="supplier-contract-note">
                    <strong>Maestro Party · Demo local</strong>
                    <span>No se muestran deuda, aging, órdenes de compra ni CFE recibidos porque esas proyecciones no están disponibles para esta vista.</span>
                  </div>
                </div>
              )}

              {activeTab === 'fiscal' && (
                <div className="supplier-detail-body">
                  <div className="supplier-detail-list">
                    {selected.fiscalIdentities.length === 0 ? <p className="supplier-muted">Sin identidades fiscales registradas.</p> : selected.fiscalIdentities.map((identity) => (
                      <article key={identity.id} className="supplier-info-card">
                        <div><strong>{identity.typeCode} · {identity.number}</strong><span>País emisor: {countryLabel(identity.issuingCountry)}</span></div>
                        <span className={`supplier-mini-state ${identity.active ? 'is-active' : ''}`}>{identity.active ? 'Activa' : 'Inactiva'}</span>
                      </article>
                    ))}
                  </div>
                </div>
              )}

              {activeTab === 'contacts' && (
                <div className="supplier-detail-body supplier-contact-grid">
                  <section>
                    <h3>Direcciones</h3>
                    <div className="supplier-detail-list">
                      {selected.addresses.length === 0 ? <p className="supplier-muted">Sin direcciones registradas.</p> : selected.addresses.map((address) => (
                        <article key={address.id} className="supplier-info-card is-stack">
                          <strong>{addressKindLabel(address.kind)}{address.primary ? ' · Principal' : ''}</strong>
                          <span>{address.addressLine}</span>
                          <small>{[address.city, address.region, countryLabel(address.countryCode)].filter(Boolean).join(' · ')}</small>
                        </article>
                      ))}
                    </div>
                  </section>
                  <section>
                    <h3>Contactos</h3>
                    <div className="supplier-detail-list">
                      {selected.contacts.length === 0 ? <p className="supplier-muted">Sin contactos registrados.</p> : selected.contacts.map((contact) => (
                        <article key={contact.id} className="supplier-info-card">
                          <div><strong>{contact.typeCode}</strong><span>{contact.value}</span></div>
                          {contact.primary && <span className="supplier-mini-state is-active">Principal</span>}
                        </article>
                      ))}
                    </div>
                  </section>
                </div>
              )}
            </>
          )}
        </aside>
      </div>

      <p className="supplier-demo-note">UI-SUPPLIER-001 · Datos locales de demostración. El backend sigue siendo la autoridad contractual.</p>
    </div>
  );
}
