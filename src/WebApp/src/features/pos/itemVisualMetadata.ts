export interface ItemVisualMetadata {
  imageUrl: string | null;
  alt: string;
}

interface MockPackshot {
  label: string;
  detail: string;
  background: string;
  packageFill: string;
  accent: string;
}

const mockPackshot = ({ label, detail, background, packageFill, accent }: MockPackshot) => {
  const svg = `
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 640 480" role="img" aria-label="${label} ${detail}">
      <rect width="640" height="480" rx="42" fill="${background}"/>
      <ellipse cx="320" cy="398" rx="150" ry="28" fill="#0f172a" opacity="0.10"/>
      <rect x="196" y="78" width="248" height="306" rx="34" fill="${packageFill}"/>
      <rect x="222" y="111" width="196" height="82" rx="18" fill="#ffffff" opacity="0.92"/>
      <circle cx="320" cy="260" r="58" fill="${accent}" opacity="0.95"/>
      <path d="M290 260c18-34 42-34 60 0-18 34-42 34-60 0Z" fill="#ffffff" opacity="0.92"/>
      <text x="320" y="148" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="30" font-weight="700" fill="#0f172a">${label}</text>
      <text x="320" y="335" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="23" font-weight="700" fill="#ffffff">${detail}</text>
    </svg>`;

  return `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(svg)}`;
};

/**
 * Presentation-only demo metadata.
 *
 * These images intentionally do not live on CommercialItemDto. They are not
 * persisted by the API and do not imply upload, gallery, media-management or
 * any other business capability.
 */
export const itemVisualMetadata: Record<string, ItemVisualMetadata> = {
  '11111111-1111-4111-8111-111111111111': {
    imageUrl: mockPackshot({ label: 'CAFÉ', detail: '250 g', background: '#f6efe7', packageFill: '#5b3a29', accent: '#d97706' }),
    alt: 'Presentación visual mock de Café Molido 250g',
  },
  '22222222-2222-4222-8222-222222222222': {
    imageUrl: mockPackshot({ label: 'AZÚCAR', detail: '1 kg', background: '#eef6ff', packageFill: '#2563eb', accent: '#f8fafc' }),
    alt: 'Presentación visual mock de Azúcar 1kg',
  },
  '33333333-3333-4333-8333-333333333333': {
    imageUrl: mockPackshot({ label: 'LECHE', detail: '1 L', background: '#eefcf7', packageFill: '#f8fafc', accent: '#0f766e' }),
    alt: 'Presentación visual mock de Leche Entera 1L',
  },
  '44444444-4444-4444-8444-444444444444': {
    imageUrl: mockPackshot({ label: 'PAN', detail: '400 g', background: '#fff7ed', packageFill: '#b45309', accent: '#fbbf24' }),
    alt: 'Presentación visual mock de Pan Integral 400g',
  },
  '66666666-6666-4666-8666-666666666666': {
    imageUrl: mockPackshot({ label: 'AGUA', detail: '500 ml', background: '#eff6ff', packageFill: '#0ea5e9', accent: '#e0f2fe' }),
    alt: 'Presentación visual mock de Agua Mineral 500ml',
  },
};

export const getItemVisualMetadata = (itemId: string): ItemVisualMetadata =>
  itemVisualMetadata[itemId] ?? {
    imageUrl: null,
    alt: 'Sin imagen de demostración disponible',
  };
