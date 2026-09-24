import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..');
const read = (relative) => fs.readFileSync(path.join(root, relative), 'utf8');

const main = read('src/main.tsx');
const shell = read('src/layout/AppShell.tsx');
const mobileNavigation = read('src/layout/MobileNavigation.tsx');
const styles = read('src/shell-responsive.css');
const failures = [];

const visualParityImport = main.indexOf("./visual-parity.css");
const responsiveImport = main.indexOf("./shell-responsive.css");

if (responsiveImport === -1) {
  failures.push('Global shell responsive stylesheet must be loaded by main.tsx.');
}

if (visualParityImport === -1 || responsiveImport < visualParityImport) {
  failures.push('shell-responsive.css must load after visual-parity.css so shell invariants win the cascade.');
}

if (!shell.includes('className="ef-workspace lg:grid lg:grid-cols-[208px_minmax(0,1fr)]"')) {
  failures.push('Governed AppShell workspace marker changed unexpectedly.');
}

if (!shell.includes('<main className="min-w-0">')) {
  failures.push('AppShell main must preserve its min-w-0 contract.');
}

if (!mobileNavigation.includes('overflow-x-auto')) {
  failures.push('Mobile navigation must keep horizontal overflow local to its own scroll container.');
}

for (const marker of [
  '.ef-app,',
  '.ef-workspace,',
  '.ef-workspace > main',
  '.ef-mobile-nav,',
  '.ef-mobile-nav > nav',
  'width: 100%;',
  'min-width: 0;',
  'max-width: 100%;',
  '@media (max-width: 760px)',
  '.ef-global-topbar',
  'grid-template-columns: minmax(0, 1fr) auto;',
  '.ef-header-brand > div,',
  '.ef-header-title',
  'white-space: normal;',
  'justify-self: end;',
  'flex: 0 0 auto;',
  '.ef-app-footer',
  'flex-wrap: wrap;',
  'overflow-wrap: anywhere;',
]) {
  if (!styles.includes(marker)) failures.push(`Responsive shell guard is missing: ${marker}`);
}

if (styles.includes('grid-template-columns: 1fr auto;')) {
  failures.push('Mobile topbar must use minmax(0, 1fr) rather than intrinsic 1fr to prevent document overflow.');
}

for (const forbidden of ['overflow-x: hidden', 'overflow-x:hidden', 'overflow-x: clip', 'overflow-x:clip']) {
  if (styles.includes(forbidden)) {
    failures.push(`Shell fix must not mask horizontal overflow with ${forbidden}.`);
  }
}

if (failures.length) {
  console.error('Responsive shell guard FAIL');
  failures.forEach((failure) => console.error(`- ${failure}`));
  process.exit(1);
}

console.log('Responsive shell guard PASS: mobile topbar uses a shrinkable grid track, brand/actions stay width-safe, navigation scrolling remains local, footer wraps, and no overflow masking is used.');
