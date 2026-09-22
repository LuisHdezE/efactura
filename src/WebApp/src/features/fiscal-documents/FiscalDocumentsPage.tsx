import { useMemo, useState } from 'react';
import './fiscal-documents-preview.css';

type FiscalStatus = 'Aceptado' | 'En proceso' | 'Rechazado';
type FiscalTab = 'Listado' | 'Eventos' | 'Representación';
type FiscalType = 'e-Factura (A)' | 'e-NC (C)' | 'e-ND (D)' | 'e-Tique (B)';

type FiscalEvent = {
  at: string;
  title: string;
  detail: string;
};

type FiscalDocument = {
  id: string;
  issuedAt: string;
  type: FiscalType;
  number: string;
  customer: string;
  rut: string;
  amount: number;
  currency: 'UYU';
  status: FiscalStatus;
  cae: string;
  caeExpiresAt: string;
  reference: string;
  observations: string;
  events: FiscalEvent[];
};

const documents: FiscalDocument[] = [
  {
    id: 'FIS-DEMO-001',
    issuedAt: '21/09/2026 · 10:24',
    type: 'e-Factura (A)',
    number: 'A 0003-001234',
    customer: 'Servicios Logísticos UY',
    rut: '218765430012',
    amount: 256000,
    currency: 'UYU',
    status: 'Aceptado',
    cae: '90261234567',
    caeExpiresAt: '30/10/2026',
    reference: 'FAC-1234',
    observations: 'Documento local de demostración. No constituye evidencia fiscal emitida por DGI.',
    events: [
      { at: '21/09/2026 · 10:24', title: 'Documento creado en fixture local', detail: 'La identidad y el importe existen solo para revisión de la interfaz.' },
      { at: '21/09/2026 · 10:25', title: 'Estado ilustrativo: Aceptado', detail: 'No proviene de DGI ni de un transporte fiscal real.' }
    ]
  },
  {
    id: 'FIS-DEMO-002',
    issuedAt: '20/09/2026 · 16:48',
    type: 'e-NC (C)',
    number: 'C 0001-000789',
    customer: 'Distribuidora Sur',
    rut: '217654320019',
    amount: 98730,
    currency: 'UYU',
    status: 'Aceptado',
    cae: '90261234568',
    caeExpiresAt: '30/10/2026',
    reference: 'NC-0789',
    observations: 'Nota de crédito ilustrativa sin efecto fiscal real.',
    events: [
      { at: '20/09/2026 · 16:48', title: 'Fixture creado', detail: 'No existe documento fiscal persistido detrás de esta fila.' },
      { at: '20/09/2026 · 16:49', title: 'Estado ilustrativo: Aceptado', detail: 'Resultado local para validar tratamiento visual.' }
    ]
  },
  {
    id: 'FIS-DEMO-003',
    issuedAt: '19/09/2026 · 14:13',
    type: 'e-Factura (A)',
    number: 'A 0003-001233',
    customer: 'Tecnología Global',
    rut: '216543210017',
    amount: 123500,
    currency: 'UYU',
    status: 'En proceso',
    cae: '90261234569',
    caeExpiresAt: '30/10/2026',
    reference: 'FAC-1233',
    observations: 'Estado de procesamiento de demostración. No representa un envío pendiente real.',
    events: [
      { at: '19/09/2026 · 14:13', title: 'Documento preparado localmente', detail: 'Evento de UI sin transporte fiscal.' },
      { at: '19/09/2026 · 14:14', title: 'Estado ilustrativo: En proceso', detail: 'No existe consulta activa a DGI.' }
    ]
  },
  {
    id: 'FIS-DEMO-004',
    issuedAt: '18/09/2026 · 11:02',
    type: 'e-ND (D)',
    number: 'D 0001-000456',
    customer: 'Cliente Demo',
    rut: '214444330018',
    amount: 50000,
    currency: 'UYU',
    status: 'Aceptado',
    cae: '90261234570',
    caeExpiresAt: '30/10/2026',
    reference: 'ND-0456',
    observations: 'Documento ilustrativo para validar densidad y detalle.',
    events: [
      { at: '18/09/2026 · 11:02', title: 'Fixture creado', detail: 'Datos de demostración no autoritativos.' },
      { at: '18/09/2026 · 11:03', title: 'Estado ilustrativo: Aceptado', detail: 'Sin evidencia de red o DGI.' }
    ]
  },
  {
    id: 'FIS-DEMO-005',
    issuedAt: '17/09/2026 · 09:51',
    type: 'e-Factura (A)',
    number: 'A 0003-001232',
    customer: 'Montevideo Partes',
    rut: '213333220011',
    amount: 178900,
    currency: 'UYU',
    status: 'Rechazado',
    cae: '90261234571',
    caeExpiresAt: '30/10/2026',
    reference: 'FAC-1232',
    observations: 'Rechazo ilustrativo. No debe interpretarse como una observación de DGI.',
    events: [
      { at: '17/09/2026 · 09:51', title: 'Fixture creado', detail: 'Documento local de demostración.' },
      { at: '17/09/2026 · 09:52', title: 'Estado ilustrativo: Rechazado', detail: 'No existe causal fiscal ni respuesta canónica asociada.' }
    ]
  },
  {
    id: 'FIS-DEMO-006',
    issuedAt: '16/09/2026 · 15:07',
    type: 'e-Tique (B)',
    number: 'B 0002-004567',
    customer: 'Consumidor Final',
    rut: '-',
    amount: 42150,
    currency: 'UYU',
    status: 'Aceptado',
    cae: '90261234572',
    caeExpiresAt: '30/10/2026',
    reference: 'TQ-4567',
    observations: 'e-Tique de demostración para validar la representación de consumidor final.',
    events: [
      { at: '16/09/2026 · 15:07', title: 'Fixture creado', detail: 'Sin emisión fiscal efectiva.' },
      { at: '16/09/2026 · 15:08', title: 'Estado ilustrativo: Aceptado', detail: 'No proviene de servicios fiscales.' }
    ]
  },
  {
    id: 'FIS-DEMO-007',
    issuedAt: '15/09/2026 · 18:20',
    type: 'e-Factura (A)',
    number: 'A 0003-001231',
    customer: 'Talleres del Centro',
    rut: '212222110015',
    amount: 89600,
    currency: 'UYU',
    status: 'Aceptado',
    cae: '90261234573',
    caeExpiresAt: '30/10/2026',
    reference: 'FAC-1231',
    observations: 'Fixture local sin representación fiscal descargable.',
    events: [
      { at: '15/09/2026 · 18:20', title: 'Fixture creado', detail: 'No se registra un CFE real.' },
      { at: '15/09/2026 · 18:21', title: 'Estado ilustrativo: Aceptado', detail: 'Estado usado únicamente para validación visual.' }
    ]
  },
  {
    id: 'FIS-DEMO-008',
    issuedAt: '14/09/2026 · 08:42',
    type: 'e-NC (C)',
    number: 'C 0001-000788',
    customer: 'Repuestos del Este',
    rut: '211111000013',
    amount: 67430,
    currency: 'UYU',
    status: 'En proceso',
    cae: '90261234574',
    caeExpiresAt: '30/10/2026',
    reference: 'NC-0788',
    observations: 'Estado pendiente ilustrativo sin operación fiscal en curso.',
    events: [
      { at: '14/09/2026 · 08:42', title: 'Fixture creado', detail: 'Fila local para revisión de filtros y estados.' },
      { at: '14/09/2026 · 08:43', title: 'Estado ilustrativo: En proceso', detail: 'No hay polling, transporte ni evidencia DGI.' }
    ]
  }
];

const uyu = new Intl.NumberFormat('es-UY', { style: 'currency', currency: 'UYU', maximumFractionDigits: 0 });
const formatUyu = (value: number) => uyu.format(value).replace('UYU', '').trim();

function statusClass(status: FiscalStatus) {
  if (status === 'En proceso') return 'processing';
  return status.toLowerCase();
}

export function FiscalDocumentsPage() {
  const [tab, setTab] = useState<FiscalTab>('Listado');
  const [selectedId, setSelectedId] = useState(documents[0].id);
  const [search, setSearch] = useState('');
  const [type, setType] = useState<'Todos' | FiscalType>('Todos');
  const [status, setStatus] = useState<'Todos' | FiscalStatus>('Todos');

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return documents.filter((item) => {
      const matchesTerm = !term || [item.number, item.customer, item.rut, item.reference, item.type].some((value) => value.toLowerCase().includes(term));
      const matchesType = type === 'Todos' || item.type === type;
      const matchesStatus = status === 'Todos' || item.status === status;
      return matchesTerm && matchesType && matchesStatus;
    });
  }, [search, type, status]);

  const selected = documents.find((item) => item.id === selectedId) ?? filtered[0] ?? documents[0];

  return (
    <div className="fiscal-documents-page">
      <header className="fiscal-documents-heading">
        <div>
          <div className="fiscal-documents-breadcrumb">Fiscal <span aria-hidden="true">›</span> Documentos fiscales</div>
          <h1>Documentos fiscales</h1>
          <p>Consulta comprobantes electrónicos de demostración, su ciclo fiscal ilustrativo y el espacio reservado para sus artefactos autorizados.</p>
        </div>
        <div className="fiscal-documents-heading-actions">
          <span className="fiscal-documents-preview-badge">Preview UI · API pendiente</span>
          <button type="button" className="fiscal-documents-secondary" disabled title="No existe una operación de exportación gobernada para WEB-013.">Exportar <span>DEMO</span></button>
        </div>
      </header>

      <div className="fiscal-documents-demo-note" role="note">
        <strong>Datos de demostración.</strong> Documentos, estados, totales, eventos, CAE y referencias son fixtures locales. Ningún estado proviene de DGI y ninguna acción fiscal se ejecuta.
      </div>

      <section className="fiscal-documents-summary" aria-label="Resumen fiscal de demostración">
        <article className="fiscal-summary-card is-total"><div className="fiscal-summary-icon">▤</div><div><span>Total de documentos</span><strong>1.240</strong><em>Fixture visual · últimos 30 días</em></div></article>
        <article className="fiscal-summary-card is-accepted"><div className="fiscal-summary-icon">✓</div><div><span>Aceptados</span><strong>1.165</strong><em>94,0% · demo</em></div></article>
        <article className="fiscal-summary-card is-processing"><div className="fiscal-summary-icon">◷</div><div><span>En proceso</span><strong>48</strong><em>3,9% · demo</em></div></article>
        <article className="fiscal-summary-card is-rejected"><div className="fiscal-summary-icon">×</div><div><span>Rechazados</span><strong>27</strong><em>2,1% · demo</em></div></article>
      </section>

      <div className="fiscal-documents-tabs" role="tablist" aria-label="Secciones de documentos fiscales">
        {(['Listado', 'Eventos', 'Representación'] as FiscalTab[]).map((item) => (
          <button key={item} type="button" className={tab === item ? 'is-active' : undefined} onClick={() => setTab(item)}>{item}</button>
        ))}
      </div>

      {tab === 'Listado' && (
        <div className="fiscal-documents-workspace">
          <section className="fiscal-documents-panel fiscal-documents-list-panel" aria-label="Listado de documentos fiscales de demostración">
            <div className="fiscal-documents-filters">
              <label className="fiscal-documents-search"><span aria-hidden="true">⌕</span><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar número, RUT, cliente, tipo…" /></label>
              <label className="fiscal-documents-filter-field"><span>Tipo</span><select value={type} onChange={(event) => setType(event.target.value as 'Todos' | FiscalType)}><option>Todos</option><option>e-Factura (A)</option><option>e-NC (C)</option><option>e-ND (D)</option><option>e-Tique (B)</option></select></label>
              <label className="fiscal-documents-filter-field"><span>Estado</span><select value={status} onChange={(event) => setStatus(event.target.value as 'Todos' | FiscalStatus)}><option>Todos</option><option>Aceptado</option><option>En proceso</option><option>Rechazado</option></select></label>
              <button type="button" className="fiscal-documents-clear" onClick={() => { setSearch(''); setType('Todos'); setStatus('Todos'); }}>Limpiar</button>
            </div>

            <div className="fiscal-documents-table-wrap">
              <table className="fiscal-documents-table">
                <thead><tr><th>Fecha</th><th>Tipo</th><th>Número</th><th>Cliente / RUT</th><th>Importe</th><th>Estado</th></tr></thead>
                <tbody>{filtered.map((item) => (
                  <tr key={item.id} className={selected.id === item.id ? 'is-selected' : undefined} tabIndex={0} onClick={() => setSelectedId(item.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setSelectedId(item.id); } }}>
                    <td>{item.issuedAt.split(' · ')[0]}</td><td>{item.type}</td><td><strong>{item.number}</strong><span>{item.reference}</span></td><td><strong>{item.customer}</strong><span>{item.rut}</span></td><td><strong>{formatUyu(item.amount)}</strong><span>{item.currency}</span></td><td><span className={`fiscal-status is-${statusClass(item.status)}`}>{item.status}</span></td>
                  </tr>
                ))}</tbody>
              </table>
            </div>

            <div className="fiscal-documents-mobile-list">{filtered.map((item) => (
              <button type="button" key={item.id} className={`fiscal-document-mobile-card ${selected.id === item.id ? 'is-selected' : ''}`} onClick={() => setSelectedId(item.id)}>
                <div className="fiscal-document-mobile-icon">▤</div>
                <div className="fiscal-document-mobile-main"><small>{item.issuedAt.split(' · ')[0]}</small><strong>{item.number}</strong><span>{item.customer}</span><b>{formatUyu(item.amount)} {item.currency}</b></div>
                <span className={`fiscal-status is-${statusClass(item.status)}`}>{item.status}</span>
              </button>
            ))}</div>

            <div className="fiscal-documents-list-footer"><span>Mostrando {filtered.length} de {documents.length} documentos locales</span><span>Sin consulta HTTP</span></div>
          </section>

          <aside className="fiscal-documents-panel fiscal-document-detail" aria-label="Detalle del documento seleccionado">
            <div className="fiscal-document-detail-head"><div><span>Detalle del documento</span><h2>{selected.number}</h2><p>{selected.type}</p></div><span className={`fiscal-status is-${statusClass(selected.status)}`}>{selected.status}</span></div>
            <dl className="fiscal-document-detail-grid">
              <div><dt>Fecha emisión</dt><dd>{selected.issuedAt}</dd></div><div><dt>Tipo</dt><dd>{selected.type}</dd></div><div><dt>Cliente</dt><dd>{selected.customer}</dd></div><div><dt>RUT</dt><dd>{selected.rut}</dd></div><div><dt>Importe total</dt><dd>{formatUyu(selected.amount)} {selected.currency}</dd></div><div><dt>Estado</dt><dd>{selected.status}</dd></div><div><dt>CAE</dt><dd>{selected.cae}</dd></div><div><dt>Vencimiento CAE</dt><dd>{selected.caeExpiresAt}</dd></div><div><dt>Referencia</dt><dd>{selected.reference}</dd></div>
            </dl>
            <div className="fiscal-document-info-box"><strong>Representación local</strong><span>{selected.observations}</span></div>
            <div className="fiscal-document-actions">
              <button type="button" disabled>Ver representación</button>
              <button type="button" disabled>Descargar XML</button>
              <button type="button" onClick={() => setTab('Eventos')}>Ver eventos</button>
              <button type="button" disabled>Solicitar corrección <span>DEMO</span></button>
              <button type="button" disabled>Solicitar entrega <span>DEMO</span></button>
            </div>
          </aside>
        </div>
      )}

      {tab === 'Eventos' && (
        <section className="fiscal-documents-panel fiscal-events-panel" aria-label="Eventos fiscales de demostración">
          <div className="fiscal-section-heading"><div><span>Eventos y trazabilidad · DEMO</span><h2>{selected.number}</h2><p>Secuencia local para validar la presentación. No es una bitácora de DGI ni del transporte fiscal.</p></div><span className={`fiscal-status is-${statusClass(selected.status)}`}>{selected.status}</span></div>
          <div className="fiscal-events-list">{selected.events.map((event) => (
            <article key={`${event.at}-${event.title}`} className="fiscal-event"><span className="fiscal-event-dot"/><div><time>{event.at}</time><strong>{event.title}</strong><p>{event.detail}</p></div></article>
          ))}</div>
          <div className="fiscal-document-info-box"><strong>Autoridad pendiente</strong><span>Los eventos reales deben provenir del servidor. Este preview no consulta ni simula respuestas de DGI como evidencia canónica.</span></div>
        </section>
      )}

      {tab === 'Representación' && (
        <section className="fiscal-documents-panel fiscal-representation-panel" aria-label="Representación y XML no ejecutables">
          <div className="fiscal-section-heading"><div><span>Representación y XML</span><h2>{selected.number}</h2><p>Espacio reservado para artefactos fiscales autorizados cuando exista soporte HTTP gobernado.</p></div><span className="fiscal-documents-preview-badge">No ejecutable</span></div>
          <div className="fiscal-artifact-placeholder">
            <div className="fiscal-artifact-icon">▤</div>
            <strong>No hay artefacto fiscal local</strong>
            <p>El preview no sintetiza XML, PDF, representación imprimible ni evidencia inmutable.</p>
          </div>
          <div className="fiscal-artifact-actions"><button type="button" disabled>Ver representación · DEMO</button><button type="button" disabled>Descargar XML · DEMO</button></div>
          <div className="fiscal-document-info-box"><strong>Corrección y regularización bloqueadas</strong><span>Crear correcciones, abrir o resolver regularizaciones y solicitar entregas siguen siendo operaciones exclusivas del servidor.</span></div>
        </section>
      )}

      <div className="fiscal-documents-bottom-note"><strong>Vista preliminar</strong><span>`UI-FIS-001` usa fixtures locales y `operations: []`. Ningún control modifica el ciclo fiscal real ni descarga artefactos autoritativos.</span></div>
    </div>
  );
}
