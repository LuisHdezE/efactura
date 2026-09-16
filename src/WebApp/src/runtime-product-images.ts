type ProductAsset = { label: string; file: string; directory?: 'products' | 'services' };

const productAssets: ProductAsset[] = [
  { label: 'Papas Fritas Clásicas 100g', file: 'papas-fritas-clasicas-100g.png' },
  { label: 'Agua Mineral 500ml', file: 'agua-mineral-500ml.png' },
  { label: 'Coca-Cola 600ml', file: 'coca-cola-600ml.png' },
  { label: 'Leche Entera 1L', file: 'leche-entera-1l.png' },
  { label: 'Pan Francés', file: 'pan-frances-kg.png' },
  { label: 'Queso Mozzarella', file: 'queso-mozzarella-kg.png' },
  { label: 'Jamón Cocido', file: 'jamon-cocido-kg.png' },
  { label: 'Banana', file: 'banana-kg.png' },
  { label: 'Detergente 750ml', file: 'detergente-750ml.png' },
  { label: 'Papel Higiénico 4u', file: 'papel-higienico-4u.png' },
  { label: 'Galletitas Surtidas 300g', file: 'galletitas-surtidas-300g.png' },
  { label: 'Café Molido 500g', file: 'cafe-molido-500g.png' },
  { label: 'Yerba Mate 1kg', file: 'yerba-mate-1kg.png' },
  { label: 'Azúcar 1kg', file: 'azucar-1kg.png' },
  { label: 'Aceite de Girasol 1L', file: 'aceite-girasol-1l.png' },
  { label: 'Servicio de entrega', file: 'servicio-entrega.png', directory: 'services' },
];

const findAsset = (label: string) => productAssets.find((asset) => label.includes(asset.label));

function paintProductImages() {
  document
    .querySelectorAll<Element>('.pos-product-visual[aria-label^="Imagen de referencia aprobada"]')
    .forEach((visual) => {
      if (visual instanceof HTMLImageElement) return;

      const label = visual.getAttribute('aria-label') ?? '';
      const asset = findAsset(label);
      if (!asset) return;

      const compact = visual.classList.contains('compact');
      const image = document.createElement('img');
      image.src = `/assets/${asset.directory ?? 'products'}/${asset.file}`;
      image.alt = asset.label;
      image.draggable = false;
      image.className = visual.getAttribute('class') ?? `pos-product-visual${compact ? ' compact' : ''}`;
      image.loading = compact ? 'eager' : 'lazy';
      image.decoding = 'async';
      Object.assign(image.style, {
        objectFit: 'contain',
        objectPosition: 'center',
        padding: compact ? '2px' : '6px',
      });

      image.addEventListener('error', () => {
        console.error('UI-POS-001 visual PNG failed to load', image.src);
      });

      visual.replaceWith(image);
    });
}

let scheduled = false;
const schedulePaint = () => {
  if (scheduled) return;
  scheduled = true;
  requestAnimationFrame(() => {
    scheduled = false;
    paintProductImages();
  });
};

const observer = new MutationObserver(schedulePaint);
observer.observe(document.documentElement, { childList: true, subtree: true });

window.addEventListener('load', schedulePaint, { once: true });
queueMicrotask(schedulePaint);
