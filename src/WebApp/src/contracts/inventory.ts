export interface InventoryPositionDto {
  id: string;
  version: number;
  itemId: string;
  locationId: string;
  quantity: number;
}

export interface StockMovementDto {
  id: string;
  positionId: string;
  itemId: string;
  locationId: string;
  kind: string;
  quantityBefore: number;
  quantityDelta: number;
  quantityAfter: number;
  reasonCode: string;
  explanation: string | null;
  occurredAtUtc: string;
}

export interface StockAdjustmentRequest {
  itemId: string;
  locationId: string;
  quantityDelta: number;
  reasonCode: string;
  expectedVersion: number;
  explanation?: string | null;
}

export interface StockAdjustmentResultDto {
  positionId: string;
  movementId: string;
  version: number;
  quantity: number;
  replayed: boolean;
}
