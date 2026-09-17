import { useEffect, useMemo, useState } from 'react';
import type { PartyDto } from '../../contracts/api';
import { gateways } from '../../services';
import './customers.css';

function getInitials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('') || 'CL';
}

function kindLabel(kind: PartyDto['kind']) {
  return kind === 'PERSON' ? 'Persona' : 'Empresa';
}

function roleLabel(role: PartyDto['roles'][number]) {
  return role === 'CUSTOMER' ? 'Cliente' : 'Proveedor';
}

function addressLabel(kind: PartyDto['addresses'][number]['kind']) {
  if (kind === 'FISCAL') return 'Fiscal';
  if (kind === 'DELIVERY') return 'Entrega';
  return 'Otra';
}

function contactLabel(typeCode: string) {
  if (typeCode === 'EMAIL') return 'Email';
  if (typeCode === 'PHONE') return 'Teléfono';
  return typeCode;
}

function primaryIdentity(party: PartyDto) {
  return party.fiscalIdentities.find((identity) => identity.active) ?? party.fiscalIdentities[0] ?? null;
}

function primaryContact(party: PartyDto) {
  return party.contacts.find((contact) => contact.primary) ?? party.contacts[0] ?? null;
}

export function CustomersPage() {
  const [customers, setCustomers] = useState<PartyDto[]>([]);
  const [search, setSearch] = useState('');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    gateways.parties
      .listCustomers()
      .then((page) => {
        if (!active) return;
        setCustomers(page.items);
        setSelectedId(page.items[0]?.id ?? null);
        setError(null);
      })
      .catch(() => {
        if (!active) return;
        setError('No pudimos cargar los clientes de demostración.');
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, []);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return customers.filter((party) => {
      if (!term) return true;
      return (
        party.name.toLowerCase().includes(term) ||
        party.fiscalIdentities.some((identity) => identity.number.toLowerCase().includes(term)) ||
        party.contacts.some((contact) => contact.value.toLowerCase().includes(term))
      );
    });
  }, [customers, search]);

  const selected = filtered.find((party) => party.id === selectedId) ?? filtered[0] ?? null;

  return (
    <div className="customer-page">
      <div className="customer-toolbar">
        <div>
          <h1 className="customer-heading">Clientes</h1>
          <p className="customer-subheading">Personas y empresas registradas con rol de cliente.</p>
        </div>
        <button
          type="button"
          className="customer-primary-action"
          disabled
          title="La escritura de clientes todavía no está conectada en esta demo WebApp."
        >
          <span aria-hidden="true">＋</span>
          Nuevo cliente
        </button>
      </div>

      <div className="customer-layout">
        <section className="customer-panel customer-directory" aria-label="Listado de clientes">
          <div className="customer-directory-toolbar">
            <label className="customer-search-shell">
              <span className="customer-search-icon" aria-hidden="true">⌕</span>
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Buscar por nombre, documento o contacto…"
                aria-label="Buscar clientes"
              />
            </label>
            <div className="customer-result-count" aria-live="polite">
              <strong>{filtered.length}</strong>
              <span>{filtered.length === 1 ? 'cliente' : 'clientes'}</span>
            </div>
          </div>

          <div className="customer-table-wrap">
            <table className="customer-table">
              <thead>
                <tr>
                  <th>Cliente</th>
                  <th>Identificación</th>
                  <th className="customer-col-contact">Contacto</th>
                  <th className="customer-col-meta">Tipo y roles</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={4}>
                      <div className="customer-state">Cargando clientes…</div>
                    </td>
                  </tr>
                ) : error ? (
                  <tr>
                    <td colSpan={4}>
                      <div className="customer-state customer-state-error">{error}</div>
                    </td>
                  </tr>
                ) : filtered.length === 0 ? (
                  <tr>
                    <td colSpan={4}>
                      <div className="customer-state">
                        <strong>Sin coincidencias</strong>
                        <span>Probá con otro nombre, documento o contacto.</span>
                      </div>
                    </td>
                  </tr>
                ) : (
                  filtered.map((party) => {
                    const identity = primaryIdentity(party);
                    const contact = primaryContact(party);
                    const isSelected = party.id === selected?.id;

                    return (
                      <tr
                        key={party.id}
                        className={isSelected ? 'is-selected' : undefined}
                        onClick={() => setSelectedId(party.id)}
                        onKeyDown={(event) => {
                          if (event.key === 'Enter' || event.key === ' ') {
                            event.preventDefault();
                            setSelectedId(party.id);
                          }
                        }}
                        tabIndex={0}
                        aria-selected={isSelected}
                      >
                        <td>
                          <div className="customer-person-cell">
                            <div className={`customer-avatar ${party.kind === 'ORGANIZATION' ? 'is-company' : ''}`} aria-hidden="true">
                              {getInitials(party.name)}
                            </div>
                            <div className="customer-person-copy">
                              <div className="customer-name">{party.name}</div>
                              <div className="customer-row-status">
                                <span className={`customer-status-dot ${party.active ? 'is-active' : ''}`} />
                                {party.active ? 'Activo' : 'Inactivo'}
                              </div>
                            </div>
                          </div>
                        </td>
                        <td>
                          {identity ? (
                            <div className="customer-identity-cell">
                              <strong>{identity.typeCode}</strong>
                              <span>{identity.number}</span>
                              <small>{identity.issuingCountry}</small>
                            </div>
                          ) : (
                            <span className="customer-muted">Sin identificación</span>
                          )}
                        </td>
                        <td className="customer-col-contact">
                          {contact ? (
                            <div className="customer-contact-cell">
                              <span>{contactLabel(contact.typeCode)}</span>
                              <strong>{contact.value}</strong>
                            </div>
                          ) : (
                            <span className="customer-muted">Sin contacto</span>
                          )}
                        </td>
                        <td className="customer-col-meta">
                          <div className="customer-row-badges">
                            <span className="customer-badge customer-badge-neutral">{kindLabel(party.kind)}</span>
                            {party.roles.map((role) => (
                              <span key={role} className={`customer-badge ${role === 'CUSTOMER' ? 'customer-badge-blue' : 'customer-badge-violet'}`}>
                                {roleLabel(role)}
                              </span>
                            ))}
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </section>

        <aside className="customer-panel customer-detail" aria-label="Detalle del cliente">
          {!selected ? (
            <div className="customer-detail-empty">
              <div className="customer-detail-empty-icon" aria-hidden="true">◎</div>
              <strong>Seleccioná un cliente</strong>
              <span>El detalle aparecerá en este panel.</span>
            </div>
          ) : (
            <>
              <div className="customer-detail-head">
                <div className="customer-detail-identity">
                  <div className={`customer-avatar customer-avatar-large ${selected.kind === 'ORGANIZATION' ? 'is-company' : ''}`} aria-hidden="true">
                    {getInitials(selected.name)}
                  </div>
                  <div className="customer-detail-title-wrap">
                    <div className="customer-detail-title-row">
                      <h2>{selected.name}</h2>
                      <span className={`customer-status-badge ${selected.active ? 'is-active' : ''}`}>
                        {selected.active ? 'Activo' : 'Inactivo'}
                      </span>
                    </div>
                    <div className="customer-detail-meta">
                      <span>{kindLabel(selected.kind)}</span>
                      <span>Versión {selected.version}</span>
                    </div>
                    <div className="customer-detail-roles">
                      {selected.roles.map((role) => (
                        <span key={role} className={`customer-badge ${role === 'CUSTOMER' ? 'customer-badge-blue' : 'customer-badge-violet'}`}>
                          {roleLabel(role)}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>
                <button
                  type="button"
                  className="customer-secondary-action"
                  disabled
                  title="La edición todavía no está conectada en esta demo WebApp."
                >
                  Editar
                </button>
              </div>

              <section className="customer-detail-section">
                <div className="customer-section-heading">
                  <h3>Información general</h3>
                </div>
                <dl className="customer-summary-grid">
                  <div>
                    <dt>Residencia</dt>
                    <dd>{selected.residenceCountry}</dd>
                  </div>
                  <div>
                    <dt>Residencia fiscal</dt>
                    <dd>{selected.taxResidenceCountry}</dd>
                  </div>
                  <div>
                    <dt>Identificaciones</dt>
                    <dd>{selected.fiscalIdentities.length}</dd>
                  </div>
                  <div>
                    <dt>Contactos</dt>
                    <dd>{selected.contacts.length}</dd>
                  </div>
                </dl>
              </section>

              <section className="customer-detail-section">
                <div className="customer-section-heading">
                  <h3>Identificación fiscal</h3>
                  <span>{selected.fiscalIdentities.length}</span>
                </div>
                {selected.fiscalIdentities.length === 0 ? (
                  <p className="customer-section-empty">Sin identificaciones registradas.</p>
                ) : (
                  <div className="customer-detail-list">
                    {selected.fiscalIdentities.map((identity) => (
                      <div key={identity.id} className="customer-detail-card">
                        <div>
                          <strong>{identity.typeCode} · {identity.number}</strong>
                          <span>País emisor {identity.issuingCountry}</span>
                        </div>
                        <span className={`customer-mini-state ${identity.active ? 'is-active' : ''}`}>
                          {identity.active ? 'Activa' : 'Inactiva'}
                        </span>
                      </div>
                    ))}
                  </div>
                )}
              </section>

              <section className="customer-detail-section customer-detail-section-split">
                <div>
                  <div className="customer-section-heading">
                    <h3>Direcciones</h3>
                    <span>{selected.addresses.length}</span>
                  </div>
                  {selected.addresses.length === 0 ? (
                    <p className="customer-section-empty">Sin direcciones registradas.</p>
                  ) : (
                    <div className="customer-detail-list">
                      {selected.addresses.map((address) => (
                        <div key={address.id} className="customer-detail-card customer-detail-card-stack">
                          <div className="customer-card-label-row">
                            <strong>{addressLabel(address.kind)}</strong>
                            {address.primary && <span>Principal</span>}
                          </div>
                          <p>{address.addressLine}</p>
                          <small>{[address.city, address.region, address.countryCode].filter(Boolean).join(' · ')}</small>
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                <div>
                  <div className="customer-section-heading">
                    <h3>Contactos</h3>
                    <span>{selected.contacts.length}</span>
                  </div>
                  {selected.contacts.length === 0 ? (
                    <p className="customer-section-empty">Sin contactos registrados.</p>
                  ) : (
                    <div className="customer-detail-list">
                      {selected.contacts.map((contact) => (
                        <div key={contact.id} className="customer-detail-card customer-contact-card">
                          <div>
                            <strong>{contactLabel(contact.typeCode)}</strong>
                            <span>{contact.value}</span>
                          </div>
                          {contact.primary && <span className="customer-primary-pill">Principal</span>}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </section>
            </>
          )}
        </aside>
      </div>

      <p className="customer-demo-note">
        Vista de demostración basada en el maestro Party. No se muestran saldos, crédito, aging ni datos financieros no soportados.
      </p>
    </div>
  );
}
