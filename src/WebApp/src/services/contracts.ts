import type {
  CommercialItemDto,
  ItemCategoryDto,
  PageResponse,
  PartyDto,
  ReferenceDataCollectionDto,
  SaleCreateInput,
  SaleDraftUpdateInput,
  SaleDto,
  SaleFiscalPreviewDto,
  SaleValidationDto,
  TaxProfileDto,
  UnitOfMeasureDto,
} from '../contracts/api';
import type { InventoryPositionDto, StockMovementDto } from '../contracts/inventory';

export interface CatalogGateway {
  listItems(search?: string): Promise<PageResponse<CommercialItemDto>>;
  listCategories(search?: string): Promise<PageResponse<ItemCategoryDto>>;
  listTaxProfiles(): Promise<PageResponse<TaxProfileDto>>;
  listUnitsOfMeasure(): Promise<ReferenceDataCollectionDto<UnitOfMeasureDto>>;
}

export interface InventoryPositionQuery {
  itemId?: string;
  locationId?: string;
}

export interface StockMovementQuery extends InventoryPositionQuery {
  positionId?: string;
}

export interface InventoryGateway {
  listPositions(query?: InventoryPositionQuery): Promise<PageResponse<InventoryPositionDto>>;
  getPosition(positionId: string): Promise<InventoryPositionDto>;
  listMovements(query?: StockMovementQuery): Promise<PageResponse<StockMovementDto>>;
}

export interface PartiesGateway {
  listCustomers(search?: string): Promise<PageResponse<PartyDto>>;
  listSuppliers(search?: string): Promise<PageResponse<PartyDto>>;
}

export interface SalesGateway {
  createSale(input: SaleCreateInput): Promise<SaleDto>;
  updateSaleDraft(saleId: string, input: SaleDraftUpdateInput): Promise<SaleDto>;
  validateSale(saleId: string, expectedVersion: number): Promise<SaleValidationDto>;
  getSaleFiscalPreview(saleId: string): Promise<SaleFiscalPreviewDto>;
}

export interface AppGateways {
  catalog: CatalogGateway;
  inventory: InventoryGateway;
  parties: PartiesGateway;
  sales: SalesGateway;
}
