import type { CommercialItemDto } from '../../contracts/api';
import approvedSpriteUrl from '../../assets/ui-pos-001-v3-product-sprite.webp';

export interface ItemVisualMetadata {
  imageUrl: string | null;
  alt: string;
  column?: number;
  row?: number;
}

type SpriteCell = { column: number; row: number; alt: string };

const spriteByCode: Record<string, SpriteCell> = {
  'ART-001': { column: 0, row: 0, alt: 'Papas Fritas Clásicas 100g' },
  'ART-002': { column: 1, row: 0, alt: 'Agua Mineral 500ml' },
  'ART-003': { column: 2, row: 0, alt: 'Coca-Cola 600ml' },
  'ART-004': { column: 3, row: 0, alt: 'Leche Entera 1L' },
  'ART-005': { column: 4, row: 0, alt: 'Pan Francés' },
  'ART-006': { column: 0, row: 1, alt: 'Queso Mozzarella' },
  'ART-007': { column: 1, row: 1, alt: 'Jamón Cocido' },
  'ART-008': { column: 2, row: 1, alt: 'Banana' },
  'ART-009': { column: 3, row: 1, alt: 'Detergente 750ml' },
  'ART-010': { column: 4, row: 1, alt: 'Papel Higiénico 4u' },
  'ART-011': { column: 0, row: 2, alt: 'Galletitas Surtidas 300g' },
  'ART-012': { column: 1, row: 2, alt: 'Café Molido 500g' },
  'ART-013': { column: 2, row: 2, alt: 'Yerba Mate 1kg' },
  'ART-014': { column: 3, row: 2, alt: 'Azúcar 1kg' },
  'ART-015': { column: 4, row: 2, alt: 'Aceite de Girasol 1L' },
};

/**
 * Presentation-only metadata derived from the approved UI-POS-001 v3 theme pair.
 *
 * The source image is the same governed Git blob approved in PR #117, now also
 * referenced from src/assets so Vite emits a fingerprinted runtime URL. The
 * mapping is by stable mock catalog code and remains outside CommercialItemDto.
 */
export const getItemVisualMetadata = (item: CommercialItemDto): ItemVisualMetadata => {
  const sprite = spriteByCode[item.code];
  if (!sprite) {
    return {
      imageUrl: null,
      alt: item.kind === 'SERVICE' ? 'Servicio sin imagen de producto' : 'Sin imagen de demostración disponible',
    };
  }

  return {
    imageUrl: approvedSpriteUrl,
    column: sprite.column,
    row: sprite.row,
    alt: `Imagen de referencia aprobada para ${sprite.alt}`,
  };
};
