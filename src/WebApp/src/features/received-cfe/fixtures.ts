export type ImportOutcome = 'IMPORTED' | 'DUPLICATE' | 'INVALID' | 'REVIEW_REQUIRED';

export interface ReceivedCfeFixture {
  id: string;
  document: string;
  issuer: string;
  receivedAt: string;
  source: string;
  outcome: ImportOutcome;
  validation: string;
  hash: string;
  specification: string;
  signature: string;
  finding: { severity: string; location: string; message: string };
  linkage: string;
}

export const outcomeLabels: Record<ImportOutcome, string> = {
  IMPORTED: 'Importado',
  DUPLICATE: 'Duplicado',
  INVALID: 'Inválido',
  REVIEW_REQUIRED: 'Requiere revisión',
};

// Illustrative metadata only. No XML bytes, fiscal verdict or canonical server identity is stored here.
export const receivedCfeFixtures: ReceivedCfeFixture[] = [
  { id: 'demo-001', document: 'e-Factura A 000184', issuer: 'Proveedor del Sur · RUT demo', receivedAt: '24/09/2026', source: 'Bandeja de ejemplo', outcome: 'REVIEW_REQUIRED', validation: 'Hallazgo ilustrativo', hash: 'No disponible en demo', specification: 'Versión no verificada', signature: 'Sin verificar', finding: { severity: 'Advertencia demo', location: 'Emisor / identificación', message: 'Revisar la identidad del emisor en una validación futura del servidor.' }, linkage: 'Sin vínculo autoritativo' },
  { id: 'demo-002', document: 'e-Factura A 000183', issuer: 'Servicios Río · RUT demo', receivedAt: '23/09/2026', source: 'Carga de ejemplo', outcome: 'IMPORTED', validation: 'Resultado ilustrativo', hash: 'No disponible en demo', specification: 'Versión no verificada', signature: 'Sin verificar', finding: { severity: 'Información demo', location: 'Documento', message: 'Este resultado representa la etiqueta contractual; no hubo importación real.' }, linkage: 'Sin vínculo autoritativo' },
  { id: 'demo-003', document: 'e-Ticket B 000067', issuer: 'Comercial Centro · RUT demo', receivedAt: '22/09/2026', source: 'Bandeja de ejemplo', outcome: 'DUPLICATE', validation: 'Resultado ilustrativo', hash: 'No disponible en demo', specification: 'Versión no verificada', signature: 'Sin verificar', finding: { severity: 'Información demo', location: 'Identidad fiscal', message: 'Ejemplo de duplicado; no crea otro registro canónico.' }, linkage: 'Sin vínculo autoritativo' },
  { id: 'demo-004', document: 'e-Factura A 000182', issuer: 'Insumos Norte · RUT demo', receivedAt: '21/09/2026', source: 'Carga de ejemplo', outcome: 'INVALID', validation: 'Hallazgo ilustrativo', hash: 'No disponible en demo', specification: 'Versión no verificada', signature: 'Sin verificar', finding: { severity: 'Error demo', location: 'Detalle / línea 1', message: 'Ejemplo de campo requerido ausente; el XML no fue analizado aquí.' }, linkage: 'Sin vínculo autoritativo' },
];
