export interface ItemVisualMetadata {
  imageUrl: string | null;
  alt: string;
  spritePosition?: string;
}

const approvedSpriteUrl = '/assets/ui-pos-001/v3/product-sprite.webp';

/**
 * Presentation-only metadata derived from the approved UI-POS-001 v3 theme pair.
 *
 * The sprite is copied byte-for-byte from the governed visual package. These
 * mappings remain outside CommercialItemDto and do not imply media-management,
 * upload, gallery or persistence capabilities.
 */
export const itemVisualMetadata: Record<string, ItemVisualMetadata> = {
  '11111111-1111-4111-8111-111111111111': {
    imageUrl: approvedSpriteUrl,
    spritePosition: '25% 100%',
    alt: 'Imagen de referencia aprobada para Café Molido',
  },
  '22222222-2222-4222-8222-222222222222': {
    imageUrl: approvedSpriteUrl,
    spritePosition: '75% 100%',
    alt: 'Imagen de referencia aprobada para Azúcar 1kg',
  },
  '33333333-3333-4333-8333-333333333333': {
    imageUrl: approvedSpriteUrl,
    spritePosition: '75% 0%',
    alt: 'Imagen de referencia aprobada para Leche Entera 1L',
  },
  '44444444-4444-4444-8444-444444444444': {
    imageUrl: approvedSpriteUrl,
    spritePosition: '100% 0%',
    alt: 'Imagen de referencia aprobada para producto de panadería',
  },
  '66666666-6666-4666-8666-666666666666': {
    imageUrl: approvedSpriteUrl,
    spritePosition: '25% 0%',
    alt: 'Imagen de referencia aprobada para Agua Mineral 500ml',
  },
};

export const getItemVisualMetadata = (itemId: string): ItemVisualMetadata =>
  itemVisualMetadata[itemId] ?? {
    imageUrl: null,
    alt: 'Sin imagen de demostración disponible',
  };
