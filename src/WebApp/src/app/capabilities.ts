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
  status: 'IMPLEMENTED_API_MOCK_DATA';
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
  }
];
