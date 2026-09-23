import type { CaeAllocationDto, CaeAuthorizationDto } from '../../contracts/cae';

export const mockCaeAuthorizations: CaeAuthorizationDto[] = [
  {
    id: 'cae-demo-001', version: 4, cfeType: 111, authorizationNumber: '90261234567', series: 'A',
    rangeFrom: 1, rangeTo: 50000, validFrom: '2026-09-01', validTo: '2026-10-30', status: 'ACTIVE',
    verificationMethod: 'DEMO_VERIFIED', sourceArtifactId: 'artifact-demo-001', sourceArtifactHash: 'demo-sha256-001',
    sourceName: 'Fixture local', sourceReference: 'CAE-DEMO-001', importedAtUtc: '2026-09-01T12:00:00Z',
    activatedAtUtc: '2026-09-01T12:05:00Z', alertCode: null
  },
  {
    id: 'cae-demo-002', version: 3, cfeType: 101, authorizationNumber: '90261234568', series: 'B',
    rangeFrom: 1, rangeTo: 100000, validFrom: '2026-08-15', validTo: '2026-11-15', status: 'ACTIVE',
    verificationMethod: 'DEMO_VERIFIED', sourceArtifactId: 'artifact-demo-002', sourceArtifactHash: 'demo-sha256-002',
    sourceName: 'Fixture local', sourceReference: 'CAE-DEMO-002', importedAtUtc: '2026-08-15T14:20:00Z',
    activatedAtUtc: '2026-08-15T14:25:00Z', alertCode: null
  },
  {
    id: 'cae-demo-003', version: 2, cfeType: 112, authorizationNumber: '90261234569', series: 'C',
    rangeFrom: 1, rangeTo: 10000, validFrom: '2026-09-10', validTo: '2027-03-10', status: 'VERIFIED',
    verificationMethod: 'DEMO_VERIFIED', sourceArtifactId: 'artifact-demo-003', sourceArtifactHash: 'demo-sha256-003',
    sourceName: 'Fixture local', sourceReference: 'CAE-DEMO-003', importedAtUtc: '2026-09-10T09:30:00Z',
    activatedAtUtc: null, alertCode: null
  },
  {
    id: 'cae-demo-004', version: 5, cfeType: 113, authorizationNumber: '90261234570', series: 'D',
    rangeFrom: 1, rangeTo: 10000, validFrom: '2026-09-05', validTo: '2027-03-05', status: 'ACTIVE',
    verificationMethod: 'DEMO_VERIFIED', sourceArtifactId: 'artifact-demo-004', sourceArtifactHash: 'demo-sha256-004',
    sourceName: 'Fixture local', sourceReference: 'CAE-DEMO-004', importedAtUtc: '2026-09-05T11:00:00Z',
    activatedAtUtc: '2026-09-05T11:04:00Z', alertCode: null
  },
  {
    id: 'cae-demo-005', version: 7, cfeType: 121, authorizationNumber: '90261234571', series: 'E',
    rangeFrom: 1, rangeTo: 25000, validFrom: '2026-08-01', validTo: '2027-08-01', status: 'ACTIVE',
    verificationMethod: 'DEMO_VERIFIED', sourceArtifactId: 'artifact-demo-005', sourceArtifactHash: 'demo-sha256-005',
    sourceName: 'Fixture local', sourceReference: 'CAE-DEMO-005', importedAtUtc: '2026-08-01T10:15:00Z',
    activatedAtUtc: '2026-08-01T10:18:00Z', alertCode: null
  },
  {
    id: 'cae-demo-006', version: 8, cfeType: 102, authorizationNumber: '90261234572', series: 'F',
    rangeFrom: 50001, rangeTo: 100000, validFrom: '2025-11-01', validTo: '2026-08-30', status: 'EXPIRED',
    verificationMethod: 'DEMO_VERIFIED', sourceArtifactId: 'artifact-demo-006', sourceArtifactHash: 'demo-sha256-006',
    sourceName: 'Fixture local', sourceReference: 'CAE-DEMO-006', importedAtUtc: '2025-11-01T13:00:00Z',
    activatedAtUtc: '2025-11-01T13:05:00Z', alertCode: 'cae.expired'
  },
  {
    id: 'cae-demo-007', version: 9, cfeType: 103, authorizationNumber: '90261234573', series: 'G',
    rangeFrom: 100001, rangeTo: 200000, validFrom: '2025-12-01', validTo: '2026-12-01', status: 'EXHAUSTED',
    verificationMethod: 'DEMO_VERIFIED', sourceArtifactId: 'artifact-demo-007', sourceArtifactHash: 'demo-sha256-007',
    sourceName: 'Fixture local', sourceReference: 'CAE-DEMO-007', importedAtUtc: '2025-12-01T15:30:00Z',
    activatedAtUtc: '2025-12-01T15:35:00Z', alertCode: 'cae.exhausted'
  }
];

export const mockCaeAllocations: CaeAllocationDto[] = [
  { id: 'alloc-demo-001', caeAuthorizationId: 'cae-demo-001', version: 2, locationId: 'Casa Central', terminalId: 'POS-01', rangeFrom: 1, rangeTo: 10000, status: 'ACTIVE', createdAtUtc: '2026-09-01T12:10:00Z', closedAtUtc: null },
  { id: 'alloc-demo-002', caeAuthorizationId: 'cae-demo-001', version: 2, locationId: 'Sucursal 01', terminalId: 'POS-02', rangeFrom: 10001, rangeTo: 30000, status: 'ACTIVE', createdAtUtc: '2026-09-01T12:12:00Z', closedAtUtc: null },
  { id: 'alloc-demo-003', caeAuthorizationId: 'cae-demo-001', version: 1, locationId: 'Sucursal 02', terminalId: null, rangeFrom: 30001, rangeTo: 50000, status: 'ACTIVE', createdAtUtc: '2026-09-01T12:14:00Z', closedAtUtc: null },
  { id: 'alloc-demo-004', caeAuthorizationId: 'cae-demo-002', version: 2, locationId: 'Casa Central', terminalId: 'POS-01', rangeFrom: 1, rangeTo: 50000, status: 'ACTIVE', createdAtUtc: '2026-08-15T14:30:00Z', closedAtUtc: null },
  { id: 'alloc-demo-005', caeAuthorizationId: 'cae-demo-002', version: 1, locationId: 'Sucursal 01', terminalId: 'POS-02', rangeFrom: 50001, rangeTo: 100000, status: 'ACTIVE', createdAtUtc: '2026-08-15T14:32:00Z', closedAtUtc: null },
  { id: 'alloc-demo-006', caeAuthorizationId: 'cae-demo-003', version: 1, locationId: 'Casa Central', terminalId: null, rangeFrom: 1, rangeTo: 10000, status: 'CLOSED', createdAtUtc: '2026-09-10T09:45:00Z', closedAtUtc: '2026-09-12T18:00:00Z' },
  { id: 'alloc-demo-007', caeAuthorizationId: 'cae-demo-005', version: 1, locationId: 'Exportaciones', terminalId: null, rangeFrom: 1, rangeTo: 25000, status: 'ACTIVE', createdAtUtc: '2026-08-01T10:25:00Z', closedAtUtc: null }
];
