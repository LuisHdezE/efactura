import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

const root = process.cwd();
const read = (relative) => fs.readFileSync(path.join(root, relative), 'utf8');

const page = read('src/features/cae/CaePage.tsx');
const css = read('src/features/cae/cae.css');
const mobileCss = read('src/features/cae/cae-mobile-filters.css');
const routes = read('src/app/routes.tsx');
const capabilities = read('src/app/capabilities.ts');
const services = read('src/services/index.ts');
const failures = [];

for (const marker of ['UI-CAE-001', "route: '/cae'", "status: 'IMPLEMENTED_API_MOCK_DATA'"]) {
  if (!capabilities.includes(marker)) failures.push(`Missing CAE capability marker: ${marker}`);
}

for (const apiId of ['API-CAE-001', 'API-CAE-002', 'API-CAE-003', 'API-CAE-004', 'API-CAE-005', 'API-CAE-006', 'API-CAE-007']) {
  if (!capabilities.includes(apiId)) failures.push(`Missing governed CAE operation mapping: ${apiId}`);
}

if (!routes.includes("webId: 'WEB-014', state: 'active'")) failures.push('WEB-014 must be active in the unified route registry.');
if (!routes.includes("requireCapability('UI-CAE-001')")) failures.push('WEB-014 must resolve UI-CAE-001 through capabilities.');

for (const forbidden of ['fetch(', 'axios', 'NextNumber', 'nextNumber', 'consumedPercentage', 'remainingNumber']) {
  if (page.includes(forbidden)) failures.push(`CaePage.tsx contains forbidden live/invented marker: ${forbidden}`);
}

for (const required of ['gateways.cae.listAuthorizations', 'gateways.cae.getAuthorization', 'gateways.cae.listAllocations', 'Importar CAE', 'Nueva asignación', 'Activar CAE', 'fiscal.manage_cae', 'cae-mobile-filter-toggle', 'aria-expanded={mobileFiltersOpen}', 'Filtros']) {
  if (!page.includes(required)) failures.push(`CaePage.tsx missing governed UI marker: ${required}`);
}

if (!services.includes('API mode is intentionally unavailable until the governed integration lane is enabled.')) {
  failures.push('Global API-mode fail-closed boundary must remain intact.');
}

for (const token of ['var(--surface)', 'var(--surface-2)', 'var(--text)', 'var(--text-soft)', 'var(--border)', 'var(--accent)']) {
  if (!css.includes(token)) failures.push(`CAE theme parity requires shared token: ${token}`);
}

for (const responsive of ['@media (max-width: 980px)', '@media (max-width: 760px)', '@media (max-width: 430px)', '.cae-mobile-list']) {
  if (!css.includes(responsive)) failures.push(`CAE responsive guard missing: ${responsive}`);
}

for (const mobileFilterMarker of ['.cae-mobile-filter-toggle', '.cae-filters.is-mobile-expanded .cae-filter-field', "[aria-expanded='true']"]) {
  if (!mobileCss.includes(mobileFilterMarker)) failures.push(`CAE mobile filter guard missing: ${mobileFilterMarker}`);
}

if (failures.length) {
  console.error('UI-CAE-001 verification failed:');
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}

console.log('UI-CAE-001 verification passed.');
