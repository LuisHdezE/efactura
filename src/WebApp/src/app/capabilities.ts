export type HttpMethod = 'GET' | 'POST' | 'PATCH' | 'PUT';

export interface CapabilityOperation {
  apiId: string;
  operationId: string;
  method: HttpMethod;
  route: string;
  permission: string;
}

export interface UiCapability {
  uiId: string;
  label: string;
  route: string;
  status: 'IMPLEMENTED_API_MOCK_DATA' | 'IMPLEMENTED_VISUAL_PREVIEW';
  operations: CapabilityOperation[];
}

export const uiCapabilities: UiCapability[] = [
  {
    uiId: 'UI-DASHBOARD-001',
    label: 'Dashboard',
    route: '/dashboard',
    status: 'IMPLEMENTED_API_MOCK_DATA',
    operations: []
  },
  {
    uiId: 'UI-POS-001',
    label: 'Punto de Venta',
    route: '/pos',
    status: 'IMPLEMENTED_API_MOCK_DATA',
    operations: [
      { apiId: 'API-CAT-001', operationId: 'listItems', method: 'GET', route: '/api/v1/items', permission: 'catalog.read' },
      { apiId: 'API-PTY-001', operationId: 'listParties', method: 'GET', route: '/api/v1/parties', permission: 'parties.read' },
      { apiId: 'API-SAL-002', operationId: 'createSale', method: 'POST', route: '/api/v1/sales', permission: 'sales.create' },
      { apiId: 'API-SAL-003', operationId: 'getSale', method: 'GET', route: '/api/v1/sales/{saleId}', permission: 'sales.read' },
      { apiId: 'API-SAL-004', operationId: 'updateSaleDraft', method: 'PATCH', route: '/api/v1/sales/{saleId}', permission: 'sales.create' },
      { apiId: 'API-SAL-005', operationId: 'validateSale', method: 'POST', route: '/api/v1/sales/{saleId}/validate', permission: 'sales.create' },
      { apiId: 'API-SAL-006', operationId: 'getSaleFiscalPreview', method: 'GET', route: '/api/v1/sales/{saleId}/fiscal-preview', permission: 'sales.read' },
      { apiId: 'API-SAL-007', operationId: 'confirmSale', method: 'POST', route: '/api/v1/sales/{saleId}/confirm', permission: 'sales.confirm' }
    ]
  },
  {
    uiId: 'UI-CUSTOMER-001',
    label: 'Clientes',
    route: '/clientes',
    status: 'IMPLEMENTED_API_MOCK_DATA',
    operations: [
      { apiId: 'API-PTY-001', operationId: 'listParties', method: 'GET', route: '/api/v1/parties', permission: 'parties.read' },
      { apiId: 'API-PTY-002', operationId: 'createParty', method: 'POST', route: '/api/v1/parties', permission: 'parties.manage' },
      { apiId: 'API-PTY-003', operationId: 'getParty', method: 'GET', route: '/api/v1/parties/{partyId}', permission: 'parties.read' },
      { apiId: 'API-PTY-004', operationId: 'updateParty', method: 'PATCH', route: '/api/v1/parties/{partyId}', permission: 'parties.manage' },
      { apiId: 'API-PTY-005', operationId: 'addPartyFiscalIdentity', method: 'POST', route: '/api/v1/parties/{partyId}/fiscal-identities', permission: 'parties.fiscal.manage' },
      { apiId: 'API-PTY-006', operationId: 'updatePartyFiscalIdentity', method: 'PUT', route: '/api/v1/parties/{partyId}/fiscal-identities/{identityId}', permission: 'parties.fiscal.manage' },
      { apiId: 'API-PTY-007', operationId: 'setPartyRoles', method: 'PUT', route: '/api/v1/parties/{partyId}/roles', permission: 'parties.manage' }
    ]
  },
  {
    uiId: 'UI-SUPPLIER-001',
    label: 'Proveedores',
    route: '/proveedores',
    status: 'IMPLEMENTED_API_MOCK_DATA',
    operations: [
      { apiId: 'API-PTY-001', operationId: 'listParties', method: 'GET', route: '/api/v1/parties', permission: 'parties.read' },
      { apiId: 'API-PTY-002', operationId: 'createParty', method: 'POST', route: '/api/v1/parties', permission: 'parties.manage' },
      { apiId: 'API-PTY-003', operationId: 'getParty', method: 'GET', route: '/api/v1/parties/{partyId}', permission: 'parties.read' },
      { apiId: 'API-PTY-004', operationId: 'updateParty', method: 'PATCH', route: '/api/v1/parties/{partyId}', permission: 'parties.manage' },
      { apiId: 'API-PTY-005', operationId: 'addPartyFiscalIdentity', method: 'POST', route: '/api/v1/parties/{partyId}/fiscal-identities', permission: 'parties.fiscal.manage' },
      { apiId: 'API-PTY-006', operationId: 'updatePartyFiscalIdentity', method: 'PUT', route: '/api/v1/parties/{partyId}/fiscal-identities/{identityId}', permission: 'parties.fiscal.manage' },
      { apiId: 'API-PTY-007', operationId: 'setPartyRoles', method: 'PUT', route: '/api/v1/parties/{partyId}/roles', permission: 'parties.manage' },
      { apiId: 'API-REF-001', operationId: 'listCountries', method: 'GET', route: '/api/v1/reference-data/countries', permission: 'authenticated' },
      { apiId: 'API-REF-003', operationId: 'listFiscalIdentityTypes', method: 'GET', route: '/api/v1/reference-data/fiscal-identity-types', permission: 'authenticated' }
    ]
  },
  {
    uiId: 'UI-CATALOG-001',
    label: 'Productos y Servicios',
    route: '/catalogo',
    status: 'IMPLEMENTED_API_MOCK_DATA',
    operations: [
      { apiId: 'API-CAT-001', operationId: 'listItems', method: 'GET', route: '/api/v1/items', permission: 'catalog.read' },
      { apiId: 'API-CAT-002', operationId: 'createItem', method: 'POST', route: '/api/v1/items', permission: 'catalog.manage' },
      { apiId: 'API-CAT-003', operationId: 'getItem', method: 'GET', route: '/api/v1/items/{itemId}', permission: 'catalog.read' },
      { apiId: 'API-CAT-004', operationId: 'updateItem', method: 'PATCH', route: '/api/v1/items/{itemId}', permission: 'catalog.manage' },
      { apiId: 'API-CAT-005', operationId: 'deactivateItem', method: 'POST', route: '/api/v1/items/{itemId}/deactivate', permission: 'catalog.manage' },
      { apiId: 'API-CAT-006', operationId: 'listItemCategories', method: 'GET', route: '/api/v1/item-categories', permission: 'catalog.read' },
      { apiId: 'API-CAT-007', operationId: 'createItemCategory', method: 'POST', route: '/api/v1/item-categories', permission: 'catalog.manage' },
      { apiId: 'API-CAT-008', operationId: 'updateItemCategory', method: 'PATCH', route: '/api/v1/item-categories/{categoryId}', permission: 'catalog.manage' },
      { apiId: 'API-CAT-009', operationId: 'listTaxProfiles', method: 'GET', route: '/api/v1/tax-profiles', permission: 'catalog.read' },
      { apiId: 'API-REF-008', operationId: 'listUnitsOfMeasure', method: 'GET', route: '/api/v1/reference-data/units-of-measure', permission: 'catalog.read' }
    ]
  },
  {
    uiId: 'UI-INVENTORY-001',
    label: 'Inventario',
    route: '/inventario',
    status: 'IMPLEMENTED_API_MOCK_DATA',
    operations: [
      { apiId: 'API-INV-001', operationId: 'listInventoryPositions', method: 'GET', route: '/api/v1/inventory/positions', permission: 'inventory.read' },
      { apiId: 'API-INV-002', operationId: 'getInventoryPosition', method: 'GET', route: '/api/v1/inventory/positions/{positionId}', permission: 'inventory.read' },
      { apiId: 'API-INV-003', operationId: 'listStockMovements', method: 'GET', route: '/api/v1/inventory/movements', permission: 'inventory.read' },
      { apiId: 'API-INV-004', operationId: 'createStockAdjustment', method: 'POST', route: '/api/v1/inventory/adjustments', permission: 'inventory.adjust' }
    ]
  },
  {
    uiId: 'UI-TRANSFER-001',
    label: 'Transferencias',
    route: '/transferencias',
    status: 'IMPLEMENTED_VISUAL_PREVIEW',
    operations: []
  },
  {
    uiId: 'UI-PROCUREMENT-001',
    label: 'Órdenes de compra y Recepciones',
    route: '/compras',
    status: 'IMPLEMENTED_VISUAL_PREVIEW',
    operations: []
  }
];
