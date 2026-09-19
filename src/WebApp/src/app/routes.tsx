import type { ReactElement } from 'react';
import { CustomersPage } from '../features/customers/CustomersPage';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { PosPage } from '../features/pos/PosPage';
import { uiCapabilities, type UiCapability } from './capabilities';

export type NavigationGroup = 'Inicio' | 'Comercial';

export interface ShellRouteDefinition {
  capability: UiCapability;
  icon: string;
  navigationGroup: NavigationGroup;
  element: ReactElement;
}

export interface ShellRouteGroup {
  name: NavigationGroup;
  routes: ShellRouteDefinition[];
}

function requireCapability(uiId: string): UiCapability {
  const capability = uiCapabilities.find((item) => item.uiId === uiId);

  if (!capability) {
    throw new Error(`Missing UI capability registration for ${uiId}`);
  }

  return capability;
}

export const shellRoutes: ShellRouteDefinition[] = [
  {
    capability: requireCapability('UI-DASHBOARD-001'),
    icon: '▦',
    navigationGroup: 'Inicio',
    element: <DashboardPage />
  },
  {
    capability: requireCapability('UI-POS-001'),
    icon: '🛒',
    navigationGroup: 'Comercial',
    element: <PosPage />
  },
  {
    capability: requireCapability('UI-CUSTOMER-001'),
    icon: '◉',
    navigationGroup: 'Comercial',
    element: <CustomersPage />
  }
];

export function groupShellRoutes(routes: ShellRouteDefinition[]): ShellRouteGroup[] {
  return routes.reduce<ShellRouteGroup[]>((groups, route) => {
    const existing = groups.find((group) => group.name === route.navigationGroup);

    if (existing) {
      existing.routes.push(route);
      return groups;
    }

    groups.push({ name: route.navigationGroup, routes: [route] });
    return groups;
  }, []);
}

export const defaultShellRoute = '/pos';
