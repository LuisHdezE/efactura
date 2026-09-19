import type { PartyDto } from '../../contracts/api';

export const mockSupplierParties: PartyDto[] = [
  {
    id: 'b1111111-1111-4111-8111-111111111111', version: 2, active: true, kind: 'ORGANIZATION', name: 'Tecnología Global S.A.', residenceCountry: 'UY', taxResidenceCountry: 'UY', roles: ['SUPPLIER'],
    fiscalIdentities: [{ id: 'bf111111-1111-4111-8111-111111111111', typeCode: 'RUT', number: '217654320019', issuingCountry: 'UY', validFrom: '2024-03-12', validTo: null, active: true }],
    addresses: [{ id: 'bd111111-1111-4111-8111-111111111111', kind: 'FISCAL', addressLine: 'Av. Italia 4380', city: 'Montevideo', region: 'Montevideo', countryCode: 'UY', postalCode: '11400', primary: true }],
    contacts: [
      { id: 'bc111111-1111-4111-8111-111111111111', typeCode: 'EMAIL', value: 'carlos@tecnologiaglobal.example.test', primary: true },
      { id: 'bc111111-1111-4111-8111-111111111112', typeCode: 'PHONE', value: '+598 2901 2345', primary: false }
    ]
  },
  {
    id: 'b2222222-2222-4222-8222-222222222222', version: 1, active: true, kind: 'ORGANIZATION', name: 'Repuestos del Sur', residenceCountry: 'UY', taxResidenceCountry: 'UY', roles: ['SUPPLIER'],
    fiscalIdentities: [{ id: 'bf222222-2222-4222-8222-222222222222', typeCode: 'RUT', number: '180987650015', issuingCountry: 'UY', validFrom: null, validTo: null, active: true }],
    addresses: [{ id: 'bd222222-2222-4222-8222-222222222222', kind: 'DELIVERY', addressLine: 'Ruta 8 km 25', city: 'Pando', region: 'Canelones', countryCode: 'UY', postalCode: null, primary: true }],
    contacts: [{ id: 'bc222222-2222-4222-8222-222222222222', typeCode: 'EMAIL', value: 'ventas@repuestosur.example.test', primary: true }]
  },
  {
    id: 'b3333333-3333-4333-8333-333333333333', version: 4, active: true, kind: 'ORGANIZATION', name: 'Importaciones Orion', residenceCountry: 'CN', taxResidenceCountry: 'CN', roles: ['SUPPLIER'],
    fiscalIdentities: [{ id: 'bf333333-3333-4333-8333-333333333333', typeCode: 'TAX_ID', number: '306543210017', issuingCountry: 'CN', validFrom: null, validTo: null, active: true }],
    addresses: [{ id: 'bd333333-3333-4333-8333-333333333333', kind: 'OTHER', addressLine: '88 Huacheng Ave', city: 'Guangzhou', region: 'Guangdong', countryCode: 'CN', postalCode: null, primary: true }],
    contacts: [{ id: 'bc333333-3333-4333-8333-333333333333', typeCode: 'EMAIL', value: 'contact@orion.example.test', primary: true }]
  },
  {
    id: 'b4444444-4444-4444-8444-444444444444', version: 1, active: false, kind: 'ORGANIZATION', name: 'Distribuidora Latina', residenceCountry: 'UY', taxResidenceCountry: 'UY', roles: ['SUPPLIER'],
    fiscalIdentities: [{ id: 'bf444444-4444-4444-8444-444444444444', typeCode: 'RUT', number: '214567890012', issuingCountry: 'UY', validFrom: null, validTo: null, active: true }],
    addresses: [],
    contacts: [{ id: 'bc444444-4444-4444-8444-444444444444', typeCode: 'EMAIL', value: 'ana@distribuidoralatina.example.test', primary: true }]
  },
  {
    id: 'b5555555-5555-4555-8555-555555555555', version: 1, active: true, kind: 'ORGANIZATION', name: 'Servicios Cloud UY', residenceCountry: 'UY', taxResidenceCountry: 'UY', roles: ['SUPPLIER', 'CUSTOMER'],
    fiscalIdentities: [{ id: 'bf555555-5555-4555-8555-555555555555', typeCode: 'RUT', number: '218765430018', issuingCountry: 'UY', validFrom: null, validTo: null, active: true }],
    addresses: [{ id: 'bd555555-5555-4555-8555-555555555555', kind: 'FISCAL', addressLine: 'Rambla República de México 6125', city: 'Montevideo', region: 'Montevideo', countryCode: 'UY', postalCode: '11500', primary: true }],
    contacts: [{ id: 'bc555555-5555-4555-8555-555555555555', typeCode: 'EMAIL', value: 'soporte@clouduy.example.test', primary: true }]
  }
];
