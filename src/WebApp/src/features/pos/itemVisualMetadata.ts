import type { CommercialItemDto } from '../../contracts/api';

export interface ItemVisualMetadata {
  imageUrl: string | null;
  alt: string;
  column?: number;
  row?: number;
}

type ProductAsset = { file: string; alt: string };

const productAssetByCode: Record<string, ProductAsset> = {
  'ART-001': { file: 'papas-fritas-clasicas-100g.png', alt: 'Papas Fritas Clásicas 100g' },
  'ART-002': { file: 'agua-mineral-500ml.png', alt: 'Agua Mineral 500ml' },
  'ART-003': { file: 'coca-cola-600ml.png', alt: 'Coca-Cola 600ml' },
  'ART-004': { file: 'leche-entera-1l.png', alt: 'Leche Entera 1L' },
  'ART-005': { file: 'pan-frances-kg.png', alt: 'Pan Francés' },
  'ART-006': { file: 'queso-mozzarella-kg.png', alt: 'Queso Mozzarella' },
  'ART-007': { file: 'jamon-cocido-kg.png', alt: 'Jamón Cocido' },
  'ART-008': { file: 'banana-kg.png', alt: 'Banana' },
  'ART-009': { file: 'detergente-750ml.png', alt: 'Detergente 750ml' },
  'ART-010': { file: 'papel-higienico-4u.png', alt: 'Papel Higiénico 4u' },
  'ART-011': { file: 'galletitas-surtidas-300g.png', alt: 'Galletitas Surtidas 300g' },
  'ART-012': { file: 'cafe-molido-500g.png', alt: 'Café Molido 500g' },
  'ART-013': { file: 'yerba-mate-1kg.png', alt: 'Yerba Mate 1kg' },
  'ART-014': { file: 'azucar-1kg.png', alt: 'Azúcar 1kg' },
  'ART-015': { file: 'aceite-girasol-1l.png', alt: 'Aceite de Girasol 1L' },
};

/**
 * Presentation-only metadata derived from the approved UI-POS-001 v3 package.
 *
 * Product artwork is preserved as individual PNG files under public/assets/products.
 * The mapping remains outside CommercialItemDto and does not imply an API media field.
 */
export const getItemVisualMetadata = (item: CommercialItemDto): ItemVisualMetadata => {
  const asset = productAssetByCode[item.code];
  if (!asset) {
    return {
      imageUrl: null,
      alt: item.kind === 'SERVICE' ? 'Servicio sin imagen de producto' : 'Sin imagen de demostración disponible',
    };
  }

  return {
    imageUrl: `/assets/products/${asset.file}`,
    // ProductVisual still renders the governed placeholder SVG before the runtime
    // adapter replaces it with a native HTML <img>. These zero coordinates keep
    // that transitional render deterministic without depending on the old sprite.
    column: 0,
    row: 0,
    alt: `Imagen de referencia aprobada para ${asset.alt}`,
  };
};
