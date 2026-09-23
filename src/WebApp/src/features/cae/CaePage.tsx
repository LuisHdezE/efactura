import { useEffect, useMemo, useState } from 'react';
import type { CaeAllocationDto, CaeAuthorizationDto } from '../../contracts/cae';
import { dataMode, gateways } from '../../services';
import './cae.css';
import './cae-mobile-filters.css';

const cfeLabels: Record<number, string> = {
  101: 'e-Ticket',
  102: 'e-NC Ticket',
  103: 'e-ND Ticket',
  111: 'e-Factura',
  112: 'e-NC Factura',
  113: 'e-ND Factura',
  121: 'e-Factura Exportación',
};

function cfeLabel(value: number) {
  return cfeLabels[value] ?? `CFE ${value}`;
}

function statusLabel(status: string) {
  switch (status) {
    case 'ACTIVE': return 'Vigente';
    case 'VERIFIED': return 'Verificado';
    case 'EXHAUSTED': return 'Agotado';
    case 'EXPIRED': return 'Vencido';
    case 'CLOSED': return 'Cerrada';
    default: return status;
  }
}

function statusClass(status: string) {
  return status.toLowerCase().replaceAll('_', '-');
}

function formatDate(value: string) {
  const date = new Date(`${value}T00:00:00Z`);
  return new Intl.DateTimeFormat('es-UY', { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' }).format(date);
}

function formatDateTime(value: string | null) {
  if (!value) return '—';
  return new Intl.DateTimeFormat('es-UY', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', timeZone: 'UTC'
  }).format(new Date(value));
}

function formatRange(from: number, to: number) {
  const number = new Intl.NumberFormat('es-UY');
  return `${number.format(from)} - ${number.format(to)}`;
}

function alertLabel(code: string | null) {
  if (!code) return 'Sin alertas';
  if (code === 'cae.expired') return 'CAE vencido';
  if (code === 'cae.exhausted' || code === 'cae.direct_range_exhausted') return 'Rango agotado';
  return code;
}

export function CaePage() {
  const [authorizations, setAuthorizations] = useState<CaeAuthorizationDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [detail, setDetail] = useState<CaeAuthorizationDto | null>(null);
  const [allocations, setAllocations] = useState<CaeAllocationDto[]>([]);
  const [allocationCounts, setAllocationCounts] = useState<Record<string, number>>({});
  const [search, setSearch] = useState('');
  const [cfeType, setCfeType] = useState('ALL');
  const [status, setStatus] = useState('ALL');
  const [validity, setValidity] = useState('ALL');
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [detailError, setDetailError] = useState<string | null>(null);
  const [guideOpen, setGuideOpen] = useState(true);
  const [mobileFiltersOpen, setMobileFiltersOpen] = useState(false);

  useEffect(() => {
    let mounted = true;
    setLoading(true);

    gateways.cae.listAuthorizations({ page: 1, pageSize: 50 })
      .then(async (page) => {
        if (!mounted) return;
        setAuthorizations(page.items);
        setSelectedId(page.items[0]?.id ?? null);
        setError(null);

        const countPairs = await Promise.all(page.items.map(async (item) => {
          try {
            const allocationPage = await gateways.cae.listAllocations(item.id);
            return [item.id, allocationPage.total] as const;
          } catch {
            return [item.id, 0] as const;
          }
        }));
        if (mounted) setAllocationCounts(Object.fromEntries(countPairs));
      })
      .catch(() => {
        if (mounted) setError('No pudimos cargar las autorizaciones CAE de demostración.');
      })
      .finally(() => {
        if (mounted) setLoading(false);
      });

    return () => { mounted = false; };
  }, []);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return authorizations.filter((item) => {
      const matchesSearch = !term || [item.authorizationNumber, item.series, cfeLabel(item.cfeType), item.sourceReference]
        .some((value) => value.toLowerCase().includes(term));
      const matchesType = cfeType === 'ALL' || item.cfeType === Number(cfeType);
      const matchesStatus = status === 'ALL' || item.status === status;
      const matchesValidity = validity === 'ALL'
        || (validity === 'CURRENT' && item.status !== 'EXPIRED')
        || (validity === 'EXPIRED' && item.status === 'EXPIRED');
      return matchesSearch && matchesType && matchesStatus && matchesValidity;
    });
  }, [authorizations, search, cfeType, status, validity]);

  const selectedSummary = filtered.find((item) => item.id === selectedId) ?? filtered[0] ?? null;

  useEffect(() => {
    if (!selectedSummary) {
      setDetail(null);
      setAllocations([]);
      return;
    }

    let mounted = true;
    setDetailLoading(true);
    setDetailError(null);

    Promise.all([
      gateways.cae.getAuthorization(selectedSummary.id),
      gateways.cae.listAllocations(selectedSummary.id),
    ])
      .then(([authorization, allocationPage]) => {
        if (!mounted) return;
        setDetail(authorization);
        setAllocations(allocationPage.items);
      })
      .catch(() => {
        if (!mounted) return;
        setDetail(selectedSummary);
        setAllocations([]);
        setDetailError('No pudimos cargar el detalle completo de este CAE.');
      })
      .finally(() => {
        if (mounted) setDetailLoading(false);
      });

    return () => { mounted = false; };
  }, [selectedSummary?.id]);

  const selected = detail?.id === selectedSummary?.id ? detail : selectedSummary;
  const clearFilters = () => {
    setSearch('');
    setCfeType('ALL');
    setStatus('ALL');
    setValidity('ALL');
    setMobileFiltersOpen(false);
  };

  return (
    <div className="cae-page">
      <header className="cae-heading">
        <div>
          <div className="cae-breadcrumb">Fiscal <span aria-hidden="true">›</span> CAE</div>
          <h1>Administración de CAE</h1>
          <p>Gestiona autorizaciones, rangos y asignaciones por punto de emisión sin inventar autoridad de numeración.</p>
        </div>
        <div className="cae-heading-actions">
          <button type="button" className="cae-secondary-action" onClick={() => setGuideOpen((value) => !value)}>
            ⓘ {guideOpen ? 'Ocultar guía' : 'Ver guía'}
          </button>
          <button type="button" className="cae-primary-action" disabled title="Requiere sesión/permiso fiscal.manage_cae e integración HTTP gobernada.">
            ⇧ Importar CAE
          </button>
        </div>
      </header>

      {guideOpen && (
        <div className="cae-guide" role="note">
          <span aria-hidden="true">ⓘ</span>
          <div><strong>Administración fiscal gobernada.</strong> Esta vista usa datos de demostración. Las lecturas CAE existen en el contrato HTTP, pero la WebApp aún no dispone de sesión/permisos autoritativos ni integración API habilitada.</div>
        </div>
      )}

      <div className="cae-mode-strip">
        <span>{dataMode === 'mock' ? 'Mock local' : 'API'}</span>
        <strong>API-CAE-001..007 soportada · integración WebApp pendiente</strong>
      </div>

      <div className="cae-workspace">
        <section className="cae-panel cae-browser" aria-label="Autorizaciones CAE">
          <div className={`cae-filters ${mobileFiltersOpen ? 'is-mobile-expanded' : ''}`}>
            <label className="cae-search">
              <span aria-hidden="true">⌕</span>
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar autorización, tipo CFE, serie…" aria-label="Buscar CAE" />
            </label>
            <button
              type="button"
              className="cae-mobile-filter-toggle"
              aria-expanded={mobileFiltersOpen}
              aria-controls="cae-filter-options"
              onClick={() => setMobileFiltersOpen((value) => !value)}
            >
              <span aria-hidden="true">☰</span> Filtros
            </button>
            <div id="cae-filter-options" className="cae-filter-options">
              <label className="cae-filter-field"><span>Tipo CFE</span><select value={cfeType} onChange={(event) => setCfeType(event.target.value)}><option value="ALL">Todos</option>{Object.entries(cfeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
              <label className="cae-filter-field"><span>Estado</span><select value={status} onChange={(event) => setStatus(event.target.value)}><option value="ALL">Todos</option><option value="ACTIVE">Vigente</option><option value="VERIFIED">Verificado</option><option value="EXHAUSTED">Agotado</option><option value="EXPIRED">Vencido</option></select></label>
              <label className="cae-filter-field"><span>Vigencia</span><select value={validity} onChange={(event) => setValidity(event.target.value)}><option value="ALL">Todas</option><option value="CURRENT">Con vigencia</option><option value="EXPIRED">Vencida</option></select></label>
            </div>
            <button type="button" className="cae-clear" onClick={clearFilters}>Limpiar</button>
          </div>

          <div className="cae-table-wrap">
            <table className="cae-table">
              <thead><tr><th>Tipo CFE</th><th>Autorización</th><th>Serie</th><th>Rango autorizado</th><th>Vigencia</th><th>Estado</th><th>Asignaciones</th><th aria-label="Abrir" /></tr></thead>
              <tbody>
                {loading ? <tr><td colSpan={8}><div className="cae-state">Cargando autorizaciones…</div></td></tr>
                  : error ? <tr><td colSpan={8}><div className="cae-state is-error">{error}</div></td></tr>
                  : filtered.length === 0 ? <tr><td colSpan={8}><div className="cae-state"><strong>Sin coincidencias</strong><span>Probá con otros filtros.</span></div></td></tr>
                  : filtered.map((item) => {
                    const isSelected = selectedSummary?.id === item.id;
                    return (
                      <tr key={item.id} className={isSelected ? 'is-selected' : undefined} tabIndex={0} aria-selected={isSelected} onClick={() => setSelectedId(item.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setSelectedId(item.id); } }}>
                        <td><span className="cae-type"><span className="cae-doc-icon">▤</span><strong>{cfeLabel(item.cfeType)}</strong></span></td>
                        <td><strong>{item.authorizationNumber}</strong></td>
                        <td>{item.series}</td>
                        <td>{formatRange(item.rangeFrom, item.rangeTo)}</td>
                        <td><span className="cae-validity"><span>{formatDate(item.validFrom)}</span><span>{formatDate(item.validTo)}</span></span></td>
                        <td><span className={`cae-status is-${statusClass(item.status)}`}>{statusLabel(item.status)}</span></td>
                        <td className="cae-count">{allocationCounts[item.id] ?? '—'}</td>
                        <td className="cae-chevron" aria-hidden="true">›</td>
                      </tr>
                    );
                  })}
              </tbody>
            </table>
          </div>

          <div className="cae-mobile-list" aria-label="CAE en formato móvil">
            {filtered.map((item) => {
              const isSelected = selectedSummary?.id === item.id;
              return (
                <button key={item.id} type="button" className={`cae-mobile-card ${isSelected ? 'is-selected' : ''}`} onClick={() => setSelectedId(item.id)}>
                  <span className="cae-doc-icon">▤</span>
                  <span className="cae-mobile-main"><small>{cfeLabel(item.cfeType)} · Serie {item.series}</small><strong>{item.authorizationNumber}</strong><span>{formatRange(item.rangeFrom, item.rangeTo)}</span><em>{formatDate(item.validFrom)} - {formatDate(item.validTo)}</em></span>
                  <span className={`cae-status is-${statusClass(item.status)}`}>{statusLabel(item.status)}</span>
                </button>
              );
            })}
          </div>

          <footer className="cae-list-footer"><span>Mostrando {filtered.length} de {authorizations.length} autorizaciones locales</span><span>Sin ejecución HTTP</span></footer>
        </section>

        <aside className="cae-panel cae-detail" aria-label="Detalle del CAE seleccionado">
          {!selected ? <div className="cae-empty-detail"><strong>Seleccioná un CAE</strong><span>El detalle aparecerá aquí.</span></div> : (
            <>
              <div className="cae-detail-head">
                <div><span>Detalle del CAE</span><h2>{selected.authorizationNumber}</h2><p>{cfeLabel(selected.cfeType)} · Serie {selected.series}</p></div>
                <span className={`cae-status is-${statusClass(selected.status)}`}>{statusLabel(selected.status)}</span>
              </div>

              {detailLoading && <div className="cae-detail-note">Actualizando detalle…</div>}
              {detailError && <div className="cae-detail-note is-error">{detailError}</div>}

              <dl className="cae-detail-grid">
                <div><dt>Tipo CFE</dt><dd>{cfeLabel(selected.cfeType)} ({selected.cfeType})</dd></div>
                <div><dt>Serie</dt><dd>{selected.series}</dd></div>
                <div><dt>Rango autorizado</dt><dd>{formatRange(selected.rangeFrom, selected.rangeTo)}</dd></div>
                <div><dt>Fecha inicio</dt><dd>{formatDate(selected.validFrom)}</dd></div>
                <div><dt>Fecha fin</dt><dd>{formatDate(selected.validTo)}</dd></div>
                <div><dt>Estado</dt><dd>{statusLabel(selected.status)}</dd></div>
                <div><dt>Verificación</dt><dd>{selected.verificationMethod}</dd></div>
                <div><dt>Procedencia</dt><dd>{selected.sourceName}</dd></div>
                <div><dt>Referencia</dt><dd>{selected.sourceReference}</dd></div>
                <div><dt>Versión</dt><dd>{selected.version}</dd></div>
                <div><dt>Importado</dt><dd>{formatDateTime(selected.importedAtUtc)}</dd></div>
                <div><dt>Alertas</dt><dd><span className={`cae-alert ${selected.alertCode ? 'has-alert' : ''}`}>{alertLabel(selected.alertCode)}</span></dd></div>
              </dl>

              <section className="cae-allocations">
                <div className="cae-section-head"><div><span>Asignaciones</span><strong>{allocations.length}</strong></div><button type="button" disabled title="Requiere fiscal.manage_cae e integración HTTP gobernada.">⊕ Nueva asignación</button></div>
                {allocations.length === 0 ? <div className="cae-allocation-empty">Sin asignaciones para este CAE.</div> : (
                  <div className="cae-allocation-list">{allocations.map((allocation) => (
                    <article key={allocation.id} className="cae-allocation-row">
                      <div><strong>{allocation.locationId}</strong><span>{allocation.terminalId ?? 'Sin terminal específica'}</span></div>
                      <div><span>Rango</span><strong>{formatRange(allocation.rangeFrom, allocation.rangeTo)}</strong></div>
                      <span className={`cae-status is-${statusClass(allocation.status)}`}>{statusLabel(allocation.status)}</span>
                    </article>
                  ))}</div>
                )}
              </section>

              <div className="cae-detail-actions">
                <button type="button" className="cae-activate" disabled title="La activación requiere permiso fiscal.manage_cae e integración WebApp real.">▷ Activar CAE</button>
                <button type="button" disabled title="Las mutaciones CAE están bloqueadas en modo demo.">⋯ Más acciones</button>
              </div>
              <div className="cae-mutation-boundary">Importar, activar, crear y cerrar asignaciones permanecen bloqueados mientras la WebApp no tenga sesión/permisos autoritativos.</div>
            </>
          )}
        </aside>
      </div>
    </div>
  );
}
