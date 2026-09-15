import type { CommercialItemDto, PageResponse, PartyDto } from '../contracts/api';

export interface CatalogGateway {
  listItems(search?: string): Promise<PageResponse<CommercialItemDto>>;
}

export interface PartiesGateway {
  listCustomers(search?: string): Promise<PageResponse<PartyDto>>;
}

export interface AppGateways {
  catalog: CatalogGateway;
  parties: PartiesGateway;
}
