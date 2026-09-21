import { readFileSync } from 'node:fs';

const capabilities = readFileSync(new URL('../src/app/capabilities.ts', import.meta.url), 'utf8');
const routes = readFileSync(new URL('../src/app/routes.tsx', import.meta.url), 'utf8');
const page = readFileSync(new URL('../src/features/payables/PayablesPage.tsx', import.meta.url), 'utf8');

const failures = [];
const payableCapability = capabilities.match(/\{\s*uiId: 'UI-PAYABLE-001',[\s\S]*?\n\s*\}/)?.[0] ?? '';

if (!payableCapability.includes("route: '/cuentas-por-pagar'")) {
  failures.push('UI-PAYABLE-001 must remain bound to /cuentas-por-pagar.');
}

if (!payableCapability.includes("status: 'IMPLEMENTED_VISUAL_PREVIEW'")) {
  failures.push('UI-PAYABLE-001 must remain IMPLEMENTED_VISUAL_PREVIEW until live API reconciliation.');
}

if (!payableCapability.includes('operations: []')) {
  failures.push('UI-PAYABLE-001 must not register executable API operations while AP/PAY HTTP is missing.');
}

if (!routes.includes("webId: 'WEB-011', state: 'active'")) {
  failures.push('WEB-011 must be an active governed route when this preview is shipped.');
}

if (!routes.includes("requireCapability('UI-PAYABLE-001')") || !routes.includes('<PayablesPage />')) {
  failures.push('WEB-011 must render PayablesPage through the governed capability registry.');
}

if (!page.includes('Datos de demostración.') || !page.includes('Preview UI · API pendiente')) {
  failures.push('Payables preview must visibly disclose its local demonstration/API-pending state.');
}

if (!page.includes('Registrar pago') || !page.includes('Sin política de excedente inventada')) {
  failures.push('Payables preview must preserve the disabled supplier-payment boundary and excess-payment policy warning.');
}

if (!page.includes('disabled title="Requiere API-PAY-001')) {
  failures.push('Supplier-payment submit control must remain explicitly disabled while API-PAY-001 is missing.');
}

if (/\bfetch\s*\(/.test(page) || /\/api\/v1\/(payables|supplier-payments)/.test(page)) {
  failures.push('Payables visual preview must not call payables/supplier-payments HTTP endpoints before live integration is authorized.');
}

if (failures.length > 0) {
  throw new Error(`Payables preview guard failed:\n- ${failures.join('\n- ')}`);
}

console.log('Payables preview guard PASS: governed route active, demo boundary explicit, no AP/PAY HTTP integration registered.');
