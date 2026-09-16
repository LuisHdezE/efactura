import type { CommercialItemDto, PartyDto } from '../../contracts/api';

const taxProfileId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';

export const mockItems: CommercialItemDto[] = [
  { id: '11111111-1111-4111-8111-111111111111', version: 1, active: true, code: 'ART-001', name: 'Papas Fritas Clásicas 100g', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'UN', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000001' },
  { id: '22222222-2222-4222-8222-222222222222', version: 1, active: true, code: 'ART-002', name: 'Agua Mineral 500ml', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'UN', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000002' },
  { id: '33333333-3333-4333-8333-333333333333', version: 1, active: true, code: 'ART-003', name: 'Coca-Cola 600ml', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'UN', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000002' },
  { id: '44444444-4444-4444-8444-444444444444', version: 2, active: true, code: 'ART-004', name: 'Leche Entera 1L', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'UN', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000003' },
  { id: '66666666-6666-4666-8666-666666666666', version: 1, active: true, code: 'ART-005', name: 'Pan Francés (kg)', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'KG', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000004' },
  { id: '77777777-7777-4777-8777-777777777777', version: 1, active: true, code: 'ART-006', name: 'Queso Mozzarella (kg)', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'KG', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000003' },
  { id: '88888888-8888-4888-8888-888888888888', version: 1, active: true, code: 'ART-007', name: 'Jamón Cocido (kg)', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'KG', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000003' },
  { id: '99999999-9999-4999-8999-999999999999', version: 1, active: true, code: 'ART-008', name: 'Banana (kg)', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'KG', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000005' },
  { id: 'aaaaaaaa-0001-4001-8001-aaaaaaaa0001', version: 1, active: true, code: 'ART-009', name: 'Detergente 750ml', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'UN', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000006' },
  { id: 'aaaaaaaa-0002-4002-8002-aaaaaaaa0002', version: 1, active: true, code: 'ART-010', name: 'Papel Higiénico 4u', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'UN', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000006' },
  { id: 'aaaaaaaa-0003-4003-8003-aaaaaaaa0003', version: 1, active: true, code: 'ART-011', name: 'Galletitas Surtidas 300g', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'UN', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000001' },
  { id: 'aaaaaaaa-0004-4004-8004-aaaaaaaa0004', version: 1, active: true, code: 'ART-012', name: 'Café Molido 500g', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'UN', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000001' },
  { id: 'aaaaaaaa-0005-4005-8005-aaaaaaaa0005', version: 1, active: true, code: 'ART-013', name: 'Yerba Mate 1kg', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'UN', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000001' },
  { id: 'aaaaaaaa-0006-4006-8006-aaaaaaaa0006', version: 1, active: true, code: 'ART-014', name: 'Azúcar 1kg', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'UN', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000001' },
  { id: 'aaaaaaaa-0007-4007-8007-aaaaaaaa0007', version: 1, active: true, code: 'ART-015', name: 'Aceite de Girasol 1L', description: 'Producto de demostración', kind: 'PRODUCT', unit: 'UN', trackInventory: true, taxProfileId, categoryId: '10000000-0000-4000-8000-000000000001' },
  { id: '55555555-5555-4555-8555-555555555555', version: 1, active: true, code: 'SERV-001', name: 'Servicio de entrega', description: 'Servicio de demostración', kind: 'SERVICE', unit: 'UN', trackInventory: false, taxProfileId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', categoryId: null }
];

export const mockParties: PartyDto[] = [
  {
    id: 'a1111111-1111-4111-8111-111111111111', version: 1, active: true, kind: 'PERSON', name: 'Juan Pérez García', residenceCountry: 'UY', taxResidenceCountry: 'UY', roles: ['CUSTOMER', 'SUPPLIER'],
    fiscalIdentities: [{ id: 'f1111111-1111-4111-8111-111111111111', typeCode: 'CI', number: '4.123.456-7', issuingCountry: 'UY', validFrom: '2015-03-10', validTo: null, active: true }],
    addresses: [{ id: 'd1111111-1111-4111-8111-111111111111', kind: 'FISCAL', addressLine: 'Av. 18 de Julio 1234', city: 'Montevideo', region: 'Montevideo', countryCode: 'UY', postalCode: '11100', primary: true }],
    contacts: [{ id: 'c1111111-1111-4111-8111-111111111111', typeCode: 'EMAIL', value: 'juan.perez@example.test', primary: true }, { id: 'c1111111-1111-4111-8111-111111111112', typeCode: 'PHONE', value: '098 123 456', primary: false }]
  },
  {
    id: 'a2222222-2222-4222-8222-222222222222', version: 3, active: true, kind: 'ORGANIZATION', name: 'Corralón del Sur S.A.', residenceCountry: 'UY', taxResidenceCountry: 'UY', roles: ['CUSTOMER'],
    fiscalIdentities: [{ id: 'f2222222-2222-4222-8222-222222222222', typeCode: 'RUT', number: '216789340012', issuingCountry: 'UY', validFrom: '2018-01-01', validTo: null, active: true }],
    addresses: [{ id: 'd2222222-2222-4222-8222-222222222222', kind: 'FISCAL', addressLine: 'Camino Maldonado 2150', city: 'Montevideo', region: 'Montevideo', countryCode: 'UY', postalCode: null, primary: true }],
    contacts: [{ id: 'c2222222-2222-4222-8222-222222222222', typeCode: 'EMAIL', value: 'ventas@corralondelsur.example.test', primary: true }]
  },
  {
    id: 'a3333333-3333-4333-8333-333333333333', version: 1, active: true, kind: 'PERSON', name: 'María Fernanda López', residenceCountry: 'UY', taxResidenceCountry: 'UY', roles: ['CUSTOMER'],
    fiscalIdentities: [{ id: 'f3333333-3333-4333-8333-333333333333', typeCode: 'CI', number: '3.987.654-1', issuingCountry: 'UY', validFrom: null, validTo: null, active: true }],
    addresses: [], contacts: [{ id: 'c3333333-3333-4333-8333-333333333333', typeCode: 'EMAIL', value: 'mflopez@example.test', primary: true }]
  },
  {
    id: 'a4444444-4444-4444-8444-444444444444', version: 2, active: true, kind: 'ORGANIZATION', name: 'Distribuidora Montevideo Ltda.', residenceCountry: 'UY', taxResidenceCountry: 'UY', roles: ['CUSTOMER', 'SUPPLIER'],
    fiscalIdentities: [{ id: 'f4444444-4444-4444-8444-444444444444', typeCode: 'RUT', number: '210456780019', issuingCountry: 'UY', validFrom: null, validTo: null, active: true }],
    addresses: [{ id: 'd4444444-4444-4444-8444-444444444444', kind: 'DELIVERY', addressLine: 'Ruta 5 km 14', city: 'La Paz', region: 'Canelones', countryCode: 'UY', postalCode: null, primary: true }],
    contacts: [{ id: 'c4444444-4444-4444-8444-444444444444', typeCode: 'PHONE', value: '2400 1111', primary: true }]
  }
];
