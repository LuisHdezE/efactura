import { useMemo, useState } from 'react';
import { outcomeLabels, receivedCfeFixtures, type ImportOutcome } from './fixtures';
import './received-cfe-preview.css';

export function ReceivedCfePage() {
  const [search, setSearch] = useState('');
  const [outcome, setOutcome] = useState<'ALL' | ImportOutcome>('ALL');
  const [selectedId, setSelectedId] = useState(receivedCfeFixtures[0].id);
  const filtered = useMemo(() => receivedCfeFixtures.filter((item) =>
    (outcome === 'ALL' || item.outcome === outcome) &&
    (!search.trim() || [item.document, item.issuer, item.source].some((value) => value.toLocaleLowerCase().includes(search.trim().toLocaleLowerCase())))
  ), [outcome, search]);
  const selected = filtered.find((item) => item.id === selectedId) ?? filtered[0];

  return <div className="received-cfe-page">
    <header className="received-cfe-heading">
      <div><div className="received-cfe-breadcrumb">Fiscal <span aria-hidden="true">›</span> CFE recibidos</div><h1>CFE recibidos</h1><p>Consulta visual de documentos recibidos y hallazgos XML de demostración.</p></div>
      <div className="received-cfe-actions"><span className="received-cfe-badge">Vista de demostración · API pendiente</span><button type="button" disabled title="Requiere API y permisos reales">Importar XML</button><button type="button" disabled title="Requiere API y permisos reales">Importar lote</button><button type="button" disabled title="La validación sin importar requiere API real">Validar sin importar</button></div>
    </header>
    <div className="received-cfe-note" role="note"><strong>Datos de demostración.</strong> Fixtures locales: no se carga ni valida un archivo, no se consulta al servidor o DGI, y ningún documento se importa o descarga.</div>
    <section className="received-cfe-summary" aria-label="Resumen local de demostración">
      {(['IMPORTED', 'DUPLICATE', 'INVALID', 'REVIEW_REQUIRED'] as const).map((status) => <div key={status} className="received-cfe-summary-card"><span>{outcomeLabels[status]}</span><strong>{receivedCfeFixtures.filter((item) => item.outcome === status).length}</strong><small>Ejemplo local</small></div>)}
    </section>
    <div className="received-cfe-workspace">
      <section className="received-cfe-panel" aria-label="Documentos de demostración">
        <div className="received-cfe-panel-title"><div><small>BANDEJA DE EJEMPLO</small><h2>Documentos recibidos</h2></div><span>{filtered.length} de {receivedCfeFixtures.length} ejemplos</span></div>
        <div className="received-cfe-filters"><label>Buscar<input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Documento, emisor u origen…" /></label><label>Resultado<select value={outcome} onChange={(event) => setOutcome(event.target.value as 'ALL' | ImportOutcome)}><option value="ALL">Todos</option>{Object.entries(outcomeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><button type="button" onClick={() => { setSearch(''); setOutcome('ALL'); }}>Limpiar</button></div>
        <div className="received-cfe-table-wrap"><table><thead><tr><th>Documento</th><th>Emisor</th><th>Recibido</th><th>Resultado</th></tr></thead><tbody>{filtered.map((item) => <tr key={item.id} className={selected?.id === item.id ? 'is-selected' : ''} tabIndex={0} onClick={() => setSelectedId(item.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setSelectedId(item.id); } }}><td><strong>{item.document}</strong><small>{item.source}</small></td><td>{item.issuer}</td><td>{item.receivedAt}</td><td><span className={`received-cfe-status is-${item.outcome.toLowerCase()}`}>{outcomeLabels[item.outcome]}</span></td></tr>)}</tbody></table></div>
        <div className="received-cfe-mobile-list">{filtered.map((item) => <button type="button" key={item.id} className={selected?.id === item.id ? 'is-selected' : ''} onClick={() => setSelectedId(item.id)}><strong>{item.document}</strong><span>{item.issuer}</span><span>{item.receivedAt} · {item.source}</span><span className={`received-cfe-status is-${item.outcome.toLowerCase()}`}>{outcomeLabels[item.outcome]}</span></button>)}</div>
        {filtered.length === 0 && <p className="received-cfe-empty">Sin coincidencias en los ejemplos locales. Ajusta la búsqueda o el filtro.</p>}
      </section>
      <aside className="received-cfe-panel received-cfe-detail" aria-label="Detalle del ejemplo seleccionado">
        <div className="received-cfe-panel-title"><div><small>DETALLE DE DEMOSTRACIÓN</small><h2>{selected?.document ?? 'Sin selección'}</h2></div></div>
        {selected ? <><div className="received-cfe-detail-body"><span className={`received-cfe-status is-${selected.outcome.toLowerCase()}`}>{outcomeLabels[selected.outcome]} · ejemplo</span><p>Resultado ilustrativo. No existe evidencia de importación, validación o estado fiscal del servidor.</p><dl><div><dt>Origen</dt><dd>{selected.source}</dd></div><div><dt>Fecha recibida</dt><dd>{selected.receivedAt}</dd></div><div><dt>Archivo original / hash</dt><dd>{selected.hash}</dd></div><div><dt>Especificación</dt><dd>{selected.specification}</dd></div><div><dt>Firma</dt><dd>{selected.signature}</dd></div><div><dt>Vínculo</dt><dd>{selected.linkage}</dd></div></dl><h3>Hallazgo ilustrativo</h3><div className="received-cfe-finding"><strong>{selected.finding.severity}</strong><span>{selected.finding.location}</span><p>{selected.finding.message}</p></div></div><div className="received-cfe-detail-footer"><button type="button" disabled title="Descarga original disponible solo mediante API autorizada">Descargar XML original</button><span>Descarga bloqueada hasta disponer de API y permisos reales.</span></div></> : <p className="received-cfe-empty">Selecciona un documento de la lista.</p>}
      </aside>
    </div>
  </div>;
}
