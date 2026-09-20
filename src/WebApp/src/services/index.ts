import type { AppGateways } from './contracts';
import { mockGateways } from './mock/mockGateways';

export type DataMode = 'mock' | 'api';

export const dataMode = (import.meta.env.VITE_DATA_MODE ?? 'mock') as DataMode;

function unavailableApiGateways(): AppGateways {
  const notIntegrated = async () => {
    throw new Error('API mode is intentionally unavailable until the governed integration lane is enabled.');
  };
  return {
    catalog: {
      listItems: notIntegrated,
      listCategories: notIntegrated,
      listTaxProfiles: notIntegrated,
      listUnitsOfMeasure: notIntegrated,
    },
    inventory: {
      listPositions: notIntegrated,
      getPosition: notIntegrated,
      listMovements: notIntegrated,
    },
    parties: { listCustomers: notIntegrated, listSuppliers: notIntegrated },
    sales: {
      createSale: notIntegrated,
      updateSaleDraft: notIntegrated,
      validateSale: notIntegrated,
      getSaleFiscalPreview: notIntegrated,
    },
  };
}

export const gateways: AppGateways = dataMode === 'mock' ? mockGateways : unavailableApiGateways();
