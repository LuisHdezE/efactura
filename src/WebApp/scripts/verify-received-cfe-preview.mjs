import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (file) => fs.readFileSync(path.join(root, file), 'utf8');
const routes = read('src/app/routes.tsx');
const capabilities = read('src/app/capabilities.ts');
const page = read('src/features/received-cfe/ReceivedCfePage.tsx');
const fixtures = read('src/features/received-cfe/fixtures.ts');
const css = read('src/features/received-cfe/received-cfe-preview.css');
const failures = [];
if (!routes.includes("webId: 'WEB-016', state: 'active', capability: requireCapability('UI-RCV-001')")) failures.push('WEB-016 route missing');
const capability = capabilities.match(/\{\s*uiId: 'UI-RCV-001'[\s\S]*?operations:\s*\[\]\s*\}/)?.[0] ?? '';
for (const marker of ["route: '/cfe-recibidos'", "status: 'IMPLEMENTED_VISUAL_PREVIEW'", 'operations: []']) if (!capability.includes(marker)) failures.push(`capability missing ${marker}`);
for (const marker of ['Datos de demostración', 'API pendiente', 'Importar XML', 'Importar lote', 'Validar sin importar', 'Descargar XML original', 'disabled', 'filtered.find((item) => item.id === selectedId)']) if (!page.includes(marker)) failures.push(`page missing ${marker}`);
for (const marker of ['IMPORTED', 'DUPLICATE', 'INVALID', 'REVIEW_REQUIRED', 'No disponible en demo', 'Sin verificar']) if (!fixtures.includes(marker)) failures.push(`fixture missing ${marker}`);
for (const marker of ['var(--surface)', 'var(--surface-2)', 'var(--text)', 'var(--border)', '@media (max-width:760px)', '@media (max-width:520px)', '.received-cfe-mobile-list', 'overflow-wrap:anywhere']) if (!css.includes(marker)) failures.push(`style missing ${marker}`);
for (const forbidden of ['fetch(', 'axios', '/api/v1/', 'input type="file"']) if (page.includes(forbidden) || fixtures.includes(forbidden)) failures.push(`live integration forbidden: ${forbidden}`);
if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('Received CFE preview guard PASS');
