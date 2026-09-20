import type { ReactElement } from 'react';
import { CatalogPage } from '../features/catalog/CatalogPage';
import { CustomersPage } from '../features/customers/CustomersPage';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { PosPage } from '../features/pos/PosPage';
import { SuppliersPage } from '../features/suppliers/SuppliersPage';
import { uiCapabilities, type UiCapability } from './capabilities';

export type NavigationGroup =
  | 'Inicio'
  | 'Comercial'
  | 'Inventario y Compras'
  | 'Finanzas'
  | 'Fiscal'
  | 'Reportes'
  | 'Administración';

interface ShellNavigationBase {
  webId: string;
  icon: string;
  navigationGroup: NavigationGroup;
}

export interface ActiveShellNavigationItem extends ShellNavigationBase {
  state: 'active';
  capability: UiCapability;
  element: ReactElement;
}

export interface PlannedShellNavigationItem extends ShellNavigationBase {
  state: 'planned';
  label: string;
}

export type ShellNavigationItem = ActiveShellNavigationItem | PlannedShellNavigationItem;
export type ShellRouteDefinition = ActiveShellNavigationItem;

export interface ShellNavigationGroup {
  name: NavigationGroup;
  items: ShellNavigationItem[];
}

function requireCapability(uiId: string): UiCapability {
  const capability = uiCapabilities.find((item) => item.uiId === uiId);

  if (!capability) {
    throw new Error(`Missing UI capability registration for ${uiId}`);
  }

  return capability;
}

export function navigationLabel(item: ShellNavigationItem): string {
  return item.state === 'active' ? item.capability.label : item.label;
}

export const shellNavigationItems: ShellNavigationItem[] = [
  { webId: 'WEB-002', state: 'active', capability: requireCapability('UI-DASHBOARD-001'), icon: '▦', navigationGroup: 'Inicio', element: <DashboardPage /> },
  { webId: 'WEB-003', state: 'active', capability: requireCapability('UI-POS-001'), icon: '▣', navigationGroup: 'Comercial', element: <PosPage /> },
  { webId: 'WEB-004', state: 'active', capability: requireCapability('UI-CUSTOMER-001'), icon: '●', navigationGroup: 'Comercial', element: <CustomersPage /> },
  { webId: 'WEB-005', state: 'active', capability: requireCapability('UI-SUPPLIER-001'), icon: '◆', navigationGroup: 'Comercial', element: <SuppliersPage /> },
  { webId: 'WEB-006', state: 'active', capability: requireCapability('UI-CATALOG-001'), icon: '▤', navigationGroup: 'Comercial', element: <CatalogPage /> },
  { webId: 'WEB-007', state: 'planned', label: 'Inventario', icon: '▥', navigationGroup: 'Inventario y Compras' },
  { webId: 'WEB-008', state: 'planned', label: 'Transferencias', icon: '⇄', navigationGroup: 'Inventario y Compras' },
  { webId: 'WEB-009', state: 'planned', label: 'Órdenes de compra y Recepciones', icon: '↓', navigationGroup: 'Inventario y Compras' },
  { webId: 'WEB-010', state: 'planned', label: 'Cuentas por cobrar', icon: '↗', navigationGroup: 'Finanzas' },
  { webId: 'WEB-011', state: 'planned', label: 'Cuentas por pagar', icon: '↙', navigationGroup: 'Finanzas' },
  { webId: 'WEB-012', state: 'planned', label: 'Caja y conciliación', icon: '▰', navigationGroup: 'Finanzas' },
  { webId: 'WEB-013', state: 'planned', label: 'Documentos fiscales', icon: '≡', navigationGroup: 'Fiscal' },
  { webId: 'WEB-014', state: 'planned', label: 'CAE', icon: '#', navigationGroup: 'Fiscal' },
  { webId: 'WEB-015', state: 'planned', label: 'Contingencia / Sincronización', icon: '↻', navigationGroup: 'Fiscal' },
  { webId: 'WEB-016', state: 'planned', label: 'CFE recibidos', icon: '⇩', navigationGroup: 'Fiscal' },
  { webId: 'WEB-017', state: 'planned', label: 'Reportes y Calendario fiscal', icon: '▧', navigationGroup: 'Reportes' },
  { webId: 'WEB-018', state: 'planned', label: 'Auditoría / Seguridad / Configuración', icon: '⚙', navigationGroup: 'Administración' },
  { webId: 'WEB-019', state: 'planned', label: 'Consola técnica', icon: '⌘', navigationGroup: 'Administración' }
];

export const shellRoutes: ShellRouteDefinition[] = shellNavigationItems.filter(
  (item): item is ActiveShellNavigationItem => item.state === 'active'
);

export function groupShellNavigationItems(items: ShellNavigationItem[]): ShellNavigationGroup[] {
  return items.reduce<ShellNavigationGroup[]>((groups, item) => {
    const existing = groups.find((group) => group.name === item.navigationGroup);

    if (existing) {
      existing.items.push(item);
      return groups;
    }

    groups.push({ name: item.navigationGroup, items: [item] });
    return groups;
  }, []);
}

export const defaultShellRoute = '/pos';
