import { readFileSync } from 'node:fs';

const indexHtml = readFileSync(new URL('../dist/index.html', import.meta.url), 'utf8');
const mainSource = readFileSync(new URL('../src/main.tsx', import.meta.url), 'utf8');
const htaccess = readFileSync(new URL('../dist/.htaccess', import.meta.url), 'utf8');

const assetRefs = [...indexHtml.matchAll(/(?:src|href)="([^"]+)"/g)].map((match) => match[1]);
const deployAssetRefs = assetRefs.filter((ref) => ref.startsWith('/efactura/assets/'));
const rootAssetRefs = assetRefs.filter((ref) => ref.startsWith('/assets/'));

if (deployAssetRefs.length === 0) {
  throw new Error('Deploy routing guard failed: no /efactura/assets/ references were emitted.');
}

if (rootAssetRefs.length > 0) {
  throw new Error(`Deploy routing guard failed: root-scoped asset references remain: ${rootAssetRefs.join(', ')}`);
}

if (!mainSource.includes('import.meta.env.BASE_URL') || !mainSource.includes('basename={routerBasename}')) {
  throw new Error('Deploy routing guard failed: BrowserRouter is not bound to Vite BASE_URL.');
}

if (!htaccess.includes('RewriteRule ^ index.html [L]')) {
  throw new Error('Deploy routing guard failed: SPA fallback rewrite is missing from dist/.htaccess.');
}

console.log(`Deploy routing guard PASS: ${deployAssetRefs.length} asset references use /efactura/ and BrowserRouter follows BASE_URL.`);