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
  'grid-template-columns: minmax(0, 1fr) 36px;',
  'padding-inline: 10px;',
  '.ef-header-brand > div',
  '.ef-header-title',
  'white-space: normal;',
  '.ef-header-actions',
  'width: 36px;',
  '.ef-user-badge',
  'display: none;',
  '.ef-theme-toggle',
  'min-width: 36px;',
  'max-width: 36px;',
  'flex: 0 0 36px;',
  '.ef-app-footer',
  'flex-wrap: wrap;',
  'overflow-wrap: anywhere;',
]) {
  if (!styles.includes(marker)) failures.push(`Responsive shell guard is missing: ${marker}`);
}

for (const forbiddenTrack of [
  'grid-template-columns: 1fr auto;',
  'grid-template-columns: minmax(0, 1fr) auto;',
]) {
  if (styles.includes(forbiddenTrack)) {
    failures.push(`Mobile topbar must reserve an explicit in-viewport theme action column, not ${forbiddenTrack}`);
  }
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

console.log('Responsive shell guard PASS: mobile topbar reserves a fixed in-viewport theme toggle, hides the nonessential avatar at <=760px, keeps navigation scrolling local, footer wraps, and no overflow masking is used.');
