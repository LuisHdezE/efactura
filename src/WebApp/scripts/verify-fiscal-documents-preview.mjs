import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..');

const routes = fs.readFileSync(path.join(root, 'src/app/routes.tsx'), 'utf8');
const capabilities = fs.readFileSync(path.join(root, 'src/app/capabilities.ts'), 'utf8');
const page = fs.readFileSync(path.join(root, 'src/features/fiscal-documents/FiscalDocumentsPage.tsx'), 'utf8');

const failures = [];

if (!routes.includes("webId: 'WEB-013', state: 'active', capability: requireCapability('UI-FIS-001')")) {
  failures.push('WEB-013 must be an active governed route backed by UI-FIS-001.');
}

if (!capabilities.includes("uiId: 'UI-FIS-001'")) {
  failures.push('UI-FIS-001 capability registration is missing.');
}

const capability = capabilities.match(/\{\s*uiId: 'UI-FIS-001'[\s\S]*?operations:\s*\[\]\s*\}/m)?.[0] ?? '';
if (!capability.includes("status: 'IMPLEMENTED_VISUAL_PREVIEW'")) {
  failures.push('UI-FIS-001 must remain IMPLEMENTED_VISUAL_PREVIEW.');
}
if (!capability.includes('operations: []')) {
  failures.push('UI-FIS-001 must keep operations: [] while fiscal document APIs remain unavailable.');
}

for (const forbidden of ['fetch(', 'axios', '/api/v1/fiscal-documents', '/api/v1/fiscal-regularizations']) {
  if (page.includes(forbidden)) {
    failures.push(`FiscalDocumentsPage.tsx must not contain live fiscal integration marker: ${forbidden}`);
  }
}

for (const required of ['Datos de demostración', 'Preview UI · API pendiente', 'Descargar XML', 'Solicitar corrección', 'Solicitar entrega', 'disabled']) {
  if (!page.includes(required)) {
    failures.push(`FiscalDocumentsPage.tsx must preserve preview boundary marker: ${required}`);
  }
}

if (failures.length) {
  console.error('Fiscal documents preview guard FAIL');
  failures.forEach((failure) => console.error(`- ${failure}`));
  process.exit(1);
}

console.log('Fiscal documents preview guard PASS: governed route active, demo boundary explicit, no FIS/FDL HTTP integration registered.');
