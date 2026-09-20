import type { AppGateways } from '../contracts';
import { mockItemCategories, mockTaxProfiles, mockUnitsOfMeasure } from './catalogReferenceData';
import { mockItems, mockParties } from './data';
import { mockSalesGateway } from './mockSales';
import { mockSupplierParties } from './supplierData';

const pause = <T,>(value: T) => new Promise<T>((resolve) => window.setTimeout(() => resolve(value), 120));

function filterPartiesByRole(role: 'CUSTOMER' | 'SUPPLIER', search = '') {
  const term = search.trim().toLowerCase();
  const source = role === 'SUPPLIER' ? [...mockParties, ...mockSupplierParties] : mockParties;
  return source.filter((party) =>
    party.roles.includes(role) &&
    (!term ||
      party.name.toLowerCase().includes(term) ||
      party.fiscalIdentities.some((identity) => identity.number.toLowerCase().includes(term)) ||
      party.contacts.some((contact) => contact.value.toLowerCase().includes(term)))
  );
}

export const mockGateways: AppGateways = {
  catalog: {
    async listItems(search = '') {
      const term = search.trim().toLowerCase();
      const items = mockItems.filter((item) => !term || item.code.toLowerCase().includes(term) || item.name.toLowerCase().includes(term));
      return pause({ items, page: 1, pageSize: 50, total: items.length });
    },
    async listCategories(search = '') {
      const term = search.trim().toLowerCase();
      const items = mockItemCategories.filter((item) => !term || item.code.toLowerCase().includes(term) || item.name.toLowerCase().includes(term));
      return pause({ items, page: 1, pageSize: 50, total: items.length });
    },
    async listTaxProfiles() {
      return pause({ items: mockTaxProfiles, page: 1, pageSize: 50, total: mockTaxProfiles.length });
    },
    async listUnitsOfMeasure() {
      return pause(mockUnitsOfMeasure);
    },
  },
  parties: {
    async listCustomers(search = '') {
      const items = filterPartiesByRole('CUSTOMER', search);
      return pause({ items, page: 1, pageSize: 50, total: items.length });
    },
    async listSuppliers(search = '') {
      const items = filterPartiesByRole('SUPPLIER', search);
      return pause({ items, page: 1, pageSize: 50, total: items.length });
    }
  },
  sales: mockSalesGateway,
};
