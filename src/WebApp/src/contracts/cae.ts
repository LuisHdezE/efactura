export interface CaeAuthorizationDto {
  id: string;
  version: number;
  cfeType: number;
  authorizationNumber: string;
  series: string;
  rangeFrom: number;
  rangeTo: number;
  validFrom: string;
  validTo: string;
  status: string;
  verificationMethod: string;
  sourceArtifactId: string;
  sourceArtifactHash: string;
  sourceName: string;
  sourceReference: string;
  importedAtUtc: string;
  activatedAtUtc: string | null;
  alertCode: string | null;
  replayed?: boolean;
}

export interface CaeAllocationDto {
  id: string;
  caeAuthorizationId: string;
  version: number;
  locationId: string;
  terminalId: string | null;
  rangeFrom: number;
  rangeTo: number;
  status: string;
  createdAtUtc: string;
  closedAtUtc: string | null;
  replayed?: boolean;
}

export interface CaeAuthorizationQuery {
  cfeType?: number;
  page?: number;
  pageSize?: number;
}
