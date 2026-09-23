import type { AppGateways } from '../contracts';
import { mockCaeAllocations, mockCaeAuthorizations } from './caeData';
import { mockItemCategories, mockTaxProfiles, mockUnitsOfMeasure } from './catalogReferenceData';
import { mockItems, mockParties } from './data';
import { mockInventoryPositions, mockStockMovements } from './inventoryData';
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
  inventory: {
    async listPositions(query = {}) {
      const items = mockInventoryPositions.filter((position) =>
        (!query.itemId || position.itemId === query.itemId)
        && (!query.locationId || position.locationId === query.locationId)
      );
      return pause({ items, page: 1, pageSize: 50, total: items.length });
    },
    async getPosition(positionId) {
      const position = mockInventoryPositions.find((item) => item.id === positionId);
      if (!position) throw new Error('Mock inventory position not found.');
      return pause(position);
    },
    async listMovements(query = {}) {
      const items = mockStockMovements.filter((movement) =>
        (!query.itemId || movement.itemId === query.itemId)
        && (!query.locationId || movement.locationId === query.locationId)
        && (!query.positionId || movement.positionId === query.positionId)
      );
      return pause({ items, page: 1, pageSize: 50, total: items.length });
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
  cae: {
    async listAuthorizations(query = {}) {
      const items = mockCaeAuthorizations.filter((authorization) => !query.cfeType || authorization.cfeType === query.cfeType);
      const page = query.page ?? 1;
      const pageSize = query.pageSize ?? 50;
      const offset = (page - 1) * pageSize;
      return pause({ items: items.slice(offset, offset + pageSize), page, pageSize, total: items.length });
    },
    async getAuthorization(caeId) {
      const authorization = mockCaeAuthorizations.find((item) => item.id === caeId);
      if (!authorization) throw new Error('Mock CAE authorization not found.');
      return pause(authorization);
    },
    async listAllocations(caeId) {
      const items = mockCaeAllocations.filter((item) => item.caeAuthorizationId === caeId);
      return pause({ items, page: 1, pageSize: 50, total: items.length });
    }
  }
};
