import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..');
const read = (relative) => fs.readFileSync(path.join(root, relative), 'utf8');

const routes = read('src/app/routes.tsx');
const capabilities = read('src/app/capabilities.ts');
const page = read('src/features/contingency/ContingencyPage.tsx');
const fixtures = read('src/features/contingency/fixtures.ts');
const styles = read('src/features/contingency/contingency-preview.css');
const failures = [];

if (!routes.includes("webId: 'WEB-015', state: 'active', capability: requireCapability('UI-CON-001')")) {
  failures.push('WEB-015 must be an active governed route backed by UI-CON-001.');
}

const capability = capabilities.match(/\{\s*uiId: 'UI-CON-001'[\s\S]*?operations:\s*\[\]\s*\}/m)?.[0] ?? '';
if (!capability.includes("route: '/contingencia'")) failures.push('UI-CON-001 must own /contingencia.');
if (!capability.includes("status: 'IMPLEMENTED_VISUAL_PREVIEW'")) failures.push('UI-CON-001 must remain IMPLEMENTED_VISUAL_PREVIEW.');
if (!capability.includes('operations: []')) failures.push('UI-CON-001 must keep operations: [] while CNT/SYN HTTP is missing.');

for (const forbidden of ['fetch(', 'axios', '/api/v1/contingency', '/api/v1/sync']) {
  if (page.includes(forbidden) || fixtures.includes(forbidden)) {
    failures.push(`Contingency preview must not contain live integration marker: ${forbidden}`);
  }
}

for (const required of [
  'Datos de demostración',
  'Preview UI · API pendiente',
  'Cliente / API',
  'DGI / Proveedor',
  'Sin consulta HTTP',
  'Entrar en contingencia',
  'Forzar sincronización',
  'Reconciliar CFC',
  'disabled',
]) {
  if (!page.includes(required) && !fixtures.includes(required)) {
    failures.push(`Contingency preview boundary marker is missing: ${required}`);
  }
}

for (const status of ['APPLIED', 'ALREADY_APPLIED', 'REJECTED', 'CONFLICT', 'REVIEW_REQUIRED', 'DEPENDENCY_BLOCKED']) {
  if (!fixtures.includes(status)) failures.push(`Canonical sync status fixture is missing: ${status}`);
}

for (const label of ['Aplicada', 'Ya aplicada', 'Rechazada', 'Conflicto', 'Requiere revisión', 'Dependencia bloqueada']) {
  if (!fixtures.includes(label)) failures.push(`Canonical textual sync label is missing: ${label}`);
}

for (const token of ['var(--surface)', 'var(--surface-2)', 'var(--text)', 'var(--text-soft)', 'var(--border)', 'var(--accent)']) {
  if (!styles.includes(token)) failures.push(`Contingency theme parity token is missing: ${token}`);
}

for (const responsive of ['@media (max-width: 1080px)', '@media (max-width: 760px)', '@media (max-width: 520px)', '.contingency-mobile-records']) {
  if (!styles.includes(responsive)) failures.push(`Contingency responsive marker is missing: ${responsive}`);
}

for (const wrapMarker of [
  '.contingency-demo-note span',
  '.contingency-status-card p',
  '.contingency-mobile-card > span:not(.contingency-status)',
  '.contingency-detail-section p',
  'overflow-wrap: anywhere;',
  'white-space: normal;',
]) {
  if (!styles.includes(wrapMarker)) failures.push(`Contingency narrow-mobile wrapping guard is missing: ${wrapMarker}`);
}

if (!styles.includes(":root[data-theme='dark']")) {
  failures.push('Contingency preview must preserve a valid explicit dark-theme parity hook.');
}

if (!page.includes('filteredRecords.find((record) => record.id === selectedId)')) {
  failures.push('Contingency detail selection must stay aligned with the active filtered result set.');
}

if (failures.length) {
  console.error('Contingency preview guard FAIL');
  failures.forEach((failure) => console.error(`- ${failure}`));
  process.exit(1);
}

console.log('Contingency preview guard PASS: WEB-015 active, UI-CON-001 visual-only, operations empty, deterministic fixtures, server-owned actions blocked, filtered detail selection aligned, theme/responsive markers present, narrow-mobile wrapping guarded.');
