export interface PageResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface CommercialItemDto {
  id: string;
  version: number;
  active: boolean;
  code: string;
  name: string;
  description: string | null;
  kind: 'PRODUCT' | 'SERVICE';
  unit: string;
  trackInventory: boolean;
  taxProfileId: string | null;
  categoryId: string | null;
}

export interface ItemCategoryDto {
  id: string;
  version: number;
  active: boolean;
  code: string;
  name: string;
}

export interface TaxProfileDto {
  id: string;
  version: number;
  code: string;
  name: string;
  treatmentCode: string;
  ratePercent: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  sourceName: string;
  sourceReference: string;
  sourceVersion: string;
  active: boolean;
}

export interface ReferenceDataCollectionDto<T> {
  sourceName: string;
  sourceVersion: string;
  items: T[];
}

export interface UnitOfMeasureDto {
  code: string;
  dgiCfe25_2Compatible: boolean;
}

export interface PartyFiscalIdentityDto {
  id: string;
  typeCode: string;
  number: string;
  issuingCountry: string;
  validFrom: string | null;
  validTo: string | null;
  active: boolean;
}

export interface PartyAddressDto {
  id: string;
  kind: 'FISCAL' | 'DELIVERY' | 'OTHER';
  addressLine: string;
  city: string;
  region: string | null;
  countryCode: string;
  postalCode: string | null;
  primary: boolean;
}

export interface PartyContactDto {
  id: string;
  typeCode: string;
  value: string;
  primary: boolean;
}

export interface PartyDto {
  id: string;
  version: number;
  active: boolean;
  kind: 'PERSON' | 'ORGANIZATION';
  name: string;
  residenceCountry: string;
  taxResidenceCountry: string;
  roles: Array<'CUSTOMER' | 'SUPPLIER'>;
  fiscalIdentities: PartyFiscalIdentityDto[];
  addresses: PartyAddressDto[];
  contacts: PartyContactDto[];
}

export interface SaleLineDraft {
  itemId: string;
  itemCode: string;
  itemName: string;
  quantity: number;
  unitPrice: number;
}

export type SaleCommercialIntent = 'CONSUMER_FINAL' | 'TAXPAYER_INVOICE' | 'EXPORT';

export interface SaleLineRequestInput {
  itemId: string;
  quantity: number;
  unitPrice: number;
  servicePerformanceScope?: string | null;
  serviceUseCountry?: string | null;
  exportServiceKind?: string | null;
}

export interface SaleCreateInput {
  intent: SaleCommercialIntent;
  currencyCode: string;
  effectiveOn: string;
  lines: SaleLineRequestInput[];
  locationId: string | null;
  terminalId: string | null;
  customerPartyId: string | null;
  deliveryCountry: string | null;
}

export interface SaleDraftUpdateInput {
  expectedVersion: number;
  intent: SaleCommercialIntent;
  currencyCode: string;
  effectiveOn: string;
  lines: SaleLineRequestInput[];
  customerPartyId: string | null;
  deliveryCountry: string | null;
}

export interface SaleLineDto {
  id: string;
  itemId: string;
  itemCode: string;
  itemName: string;
  kind: string;
  quantity: number;
  unitPrice: number;
  netAmount: number;
  taxProfileId: string | null;
}

export interface SaleDto {
  id: string;
  version: number;
  status: string;
  organizationId: string;
  locationId: string | null;
  terminalId: string | null;
  customerPartyId: string | null;
  intent: SaleCommercialIntent;
  currencyCode: string;
  effectiveOn: string;
  deliveryCountry: string | null;
  netAmount: number;
  validationFingerprint: string | null;
  validatedAtUtc: string | null;
  lines: SaleLineDto[];
}

export interface FiscalRuleReferenceDto {
  ruleId: string;
  sourceName: string;
  sourceReference: string;
  sourceVersion: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  clause: string | null;
}

export interface SaleFiscalPreviewLineDto {
  lineId: string;
  itemCode: string;
  netAmount: number;
  taxTreatmentStatus: string;
  taxTreatment: string;
  treatmentCode: string;
  taxRateStatus: string;
  vatLiability: string;
  vatRateKind: string;
  appliedRatePercent: number | null;
  previewTaxAmount: number | null;
  reasons: string[];
  missingFacts: string[];
  ruleReferences: FiscalRuleReferenceDto[];
}

export interface CfeCandidateDto {
  familyCode: number;
  family: string;
  receiverIdentification: string;
  reasons: string[];
}

export interface CfeDecisionDto {
  eligibilityStatus: string;
  selectionStatus: string;
  selectedFamilyCode: number | null;
  selectedFamily: string | null;
  candidates: CfeCandidateDto[];
  reasons: string[];
  missingFacts: string[];
  formatVersion: string;
}

export interface SaleFiscalPreviewDto {
  saleId: string;
  saleVersion: number;
  currencyCode: string;
  netAmount: number;
  previewTaxAmount: number | null;
  previewTotalAmount: number | null;
  lines: SaleFiscalPreviewLineDto[];
  overallTaxTreatmentStatus: string;
  overallTaxTreatment: string;
  treatmentCode: string;
  cfe: CfeDecisionDto;
  readyForValidation: boolean;
  validationFingerprint: string;
  findings: string[];
  arithmeticAuthority: string;
}

export interface SaleValidationDto {
  valid: boolean;
  replayed: boolean;
  sale: SaleDto;
  preview: SaleFiscalPreviewDto;
}
