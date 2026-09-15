import type { AppGateways } from '../contracts';
import { mockItems, mockParties } from './data';

const pause = <T,>(value: T) => new Promise<T>((resolve) => window.setTimeout(() => resolve(value), 120));

export const mockGateways: AppGateways = {
  catalog: {
    async listItems(search = '') {
      const term = search.trim().toLowerCase();
      const items = mockItems.filter((item) => !term || item.code.toLowerCase().includes(term) || item.name.toLowerCase().includes(term));
      return pause({ items, page: 1, pageSize: 50, total: items.length });
    }
  },
  parties: {
    async listCustomers(search = '') {
      const term = search.trim().toLowerCase();
      const items = mockParties.filter((party) => party.roles.includes('CUSTOMER') && (!term || party.name.toLowerCase().includes(term) || party.fiscalIdentities.some((identity) => identity.number.toLowerCase().includes(term))));
      return pause({ items, page: 1, pageSize: 50, total: items.length });
    }
  }
};
