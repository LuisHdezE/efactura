import { readFileSync } from 'node:fs';

const capabilities = readFileSync(new URL('../src/app/capabilities.ts', import.meta.url), 'utf8');
const routes = readFileSync(new URL('../src/app/routes.tsx', import.meta.url), 'utf8');
const page = readFileSync(new URL('../src/features/receivables/ReceivablesPage.tsx', import.meta.url), 'utf8');

const failures = [];

const receivableCapability = capabilities.match(/\{\s*uiId: 'UI-RECEIVABLE-001',[\s\S]*?\n\s*\}/)?.[0] ?? '';

if (!receivableCapability.includes("route: '/cuentas-por-cobrar'")) {
  failures.push('UI-RECEIVABLE-001 must remain bound to /cuentas-por-cobrar.');
}

if (!receivableCapability.includes("status: 'IMPLEMENTED_VISUAL_PREVIEW'")) {
  failures.push('UI-RECEIVABLE-001 must remain IMPLEMENTED_VISUAL_PREVIEW until live API reconciliation.');
}

if (!receivableCapability.includes('operations: []')) {
  failures.push('UI-RECEIVABLE-001 must not register executable API operations while AR/COL HTTP is missing.');
}

if (!routes.includes("webId: 'WEB-010', state: 'active'")) {
  failures.push('WEB-010 must be an active governed route when this preview is shipped.');
}

if (!routes.includes("requireCapability('UI-RECEIVABLE-001')") || !routes.includes('<ReceivablesPage />')) {
  failures.push('WEB-010 must render ReceivablesPage through the governed capability registry.');
}

if (!page.includes('Datos de demostración.') || !page.includes('Preview UI · API pendiente')) {
  failures.push('Receivables preview must visibly disclose its local demonstration/API-pending state.');
}

if (!page.includes('Registrar cobro') || !page.includes('Ajustar cuenta') || !page.includes('Revertir cobranza')) {
  failures.push('Receivables preview must preserve the visually reserved financial actions.');
}

if (/\bfetch\s*\(/.test(page) || /\/api\/v1\/(receivables|collections)/.test(page)) {
  failures.push('Receivables visual preview must not call receivables/collections HTTP endpoints before live integration is authorized.');
}

if (failures.length > 0) {
  throw new Error(`Receivables preview guard failed:\n- ${failures.join('\n- ')}`);
}

console.log('Receivables preview guard PASS: governed route active, demo boundary explicit, no AR/COL HTTP integration registered.');
