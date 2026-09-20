import type { ItemCategoryDto, ReferenceDataCollectionDto, TaxProfileDto, UnitOfMeasureDto } from '../../contracts/api';

export const mockItemCategories: ItemCategoryDto[] = [
  { id: '10000000-0000-4000-8000-000000000001', version: 1, active: true, code: 'ALIM', name: 'Alimentos' },
  { id: '10000000-0000-4000-8000-000000000002', version: 1, active: true, code: 'BEB', name: 'Bebidas' },
  { id: '10000000-0000-4000-8000-000000000003', version: 1, active: true, code: 'LACT', name: 'Lácteos' },
  { id: '10000000-0000-4000-8000-000000000004', version: 1, active: true, code: 'PAN', name: 'Panadería' },
  { id: '10000000-0000-4000-8000-000000000005', version: 1, active: true, code: 'FRUT', name: 'Frutas' },
  { id: '10000000-0000-4000-8000-000000000006', version: 1, active: true, code: 'LIMP', name: 'Limpieza' },
];

export const mockTaxProfiles: TaxProfileDto[] = [
  {
    id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
    version: 1,
    code: 'IVA22-DEMO',
    name: 'IVA básica 22% · demo',
    treatmentCode: 'TAXED',
    ratePercent: 22,
    effectiveFrom: '2026-01-01',
    effectiveTo: null,
    sourceName: 'Fixture local WebApp',
    sourceReference: 'UI-CATALOG-001',
    sourceVersion: 'v1-theme-pair',
    active: true,
  },
  {
    id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
    version: 1,
    code: 'SERV22-DEMO',
    name: 'Servicio gravado 22% · demo',
    treatmentCode: 'TAXED',
    ratePercent: 22,
    effectiveFrom: '2026-01-01',
    effectiveTo: null,
    sourceName: 'Fixture local WebApp',
    sourceReference: 'UI-CATALOG-001',
    sourceVersion: 'v1-theme-pair',
    active: true,
  },
];

export const mockUnitsOfMeasure: ReferenceDataCollectionDto<UnitOfMeasureDto> = {
  sourceName: 'Proyección local de unidades configuradas',
  sourceVersion: 'UI-CATALOG-001-demo',
  items: [
    { code: 'UN', dgiCfe25_2Compatible: true },
    { code: 'KG', dgiCfe25_2Compatible: true },
  ],
};
