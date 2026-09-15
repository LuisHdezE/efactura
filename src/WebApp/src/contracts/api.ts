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
