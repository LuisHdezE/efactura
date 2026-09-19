import type { ReactElement } from 'react';
import { CustomersPage } from '../features/customers/CustomersPage';
import { PosPage } from '../features/pos/PosPage';
import { uiCapabilities, type UiCapability } from './capabilities';

export interface ShellRouteDefinition {
  capability: UiCapability;
  icon: string;
  element: ReactElement;
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
    capability: requireCapability('UI-POS-001'),
    icon: '🛒',
    element: <PosPage />
  },
  {
    capability: requireCapability('UI-CUSTOMER-001'),
    icon: '◉',
    element: <CustomersPage />
  }
];

export const defaultShellRoute = '/pos';
