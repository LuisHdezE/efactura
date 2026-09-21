import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..');

const routes = fs.readFileSync(path.join(root, 'src/app/routes.tsx'), 'utf8');
const capabilities = fs.readFileSync(path.join(root, 'src/app/capabilities.ts'), 'utf8');
const page = fs.readFileSync(path.join(root, 'src/features/cash/CashPage.tsx'), 'utf8');

const failures = [];

if (!routes.includes("webId: 'WEB-012', state: 'active', capability: requireCapability('UI-CASH-001')")) {
  failures.push('WEB-012 must be an active governed route backed by UI-CASH-001.');
}

if (!capabilities.includes("uiId: 'UI-CASH-001'")) {
  failures.push('UI-CASH-001 capability registration is missing.');
}

const cashCapability = capabilities.match(/\{\s*uiId: 'UI-CASH-001'[\s\S]*?operations:\s*\[\]\s*\}/m)?.[0] ?? '';
if (!cashCapability.includes("status: 'IMPLEMENTED_VISUAL_PREVIEW'")) {
  failures.push('UI-CASH-001 must remain IMPLEMENTED_VISUAL_PREVIEW.');
}
if (!cashCapability.includes('operations: []')) {
  failures.push('UI-CASH-001 must keep operations: [] while API-CSH-* remain unavailable.');
}

for (const forbidden of ['fetch(', 'axios', '/api/v1/cash-shifts']) {
  if (page.includes(forbidden)) {
    failures.push(`CashPage.tsx must not contain live cash integration marker: ${forbidden}`);
  }
}

for (const required of ['Datos de demostración', 'Abrir turno', 'Cerrar turno', 'Conciliar diferencia', 'disabled']) {
  if (!page.includes(required)) {
    failures.push(`CashPage.tsx must preserve preview boundary marker: ${required}`);
  }
}

if (failures.length) {
  console.error('Cash preview guard FAIL');
  failures.forEach((failure) => console.error(`- ${failure}`));
  process.exit(1);
}

console.log('Cash preview guard PASS: governed route active, demo boundary explicit, no CSH HTTP integration registered.');
