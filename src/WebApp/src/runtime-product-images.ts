import approvedSpriteUrl from './assets/ui-pos-001-v3-product-sprite.webp';

type SpriteCell = { label: string; column: number; row: number };

const cells: SpriteCell[] = [
  { label: 'Papas Fritas Clásicas 100g', column: 0, row: 0 },
  { label: 'Agua Mineral 500ml', column: 1, row: 0 },
  { label: 'Coca-Cola 600ml', column: 2, row: 0 },
  { label: 'Leche Entera 1L', column: 3, row: 0 },
  { label: 'Pan Francés', column: 4, row: 0 },
  { label: 'Queso Mozzarella', column: 0, row: 1 },
  { label: 'Jamón Cocido', column: 1, row: 1 },
  { label: 'Banana', column: 2, row: 1 },
  { label: 'Detergente 750ml', column: 3, row: 1 },
  { label: 'Papel Higiénico 4u', column: 4, row: 1 },
  { label: 'Galletitas Surtidas 300g', column: 0, row: 2 },
  { label: 'Café Molido 500g', column: 1, row: 2 },
  { label: 'Yerba Mate 1kg', column: 2, row: 2 },
  { label: 'Azúcar 1kg', column: 3, row: 2 },
  { label: 'Aceite de Girasol 1L', column: 4, row: 2 },
];

const findCell = (label: string) => cells.find((cell) => label.includes(cell.label));

function buildLayer(cell: SpriteCell, compact: boolean) {
  const layer = document.createElement('div');
  layer.className = 'pos-runtime-product-image';
  layer.setAttribute('aria-hidden', 'true');
  Object.assign(layer.style, {
    position: 'absolute',
    overflow: 'hidden',
    pointerEvents: 'none',
    zIndex: '1',
    background: 'var(--surface)',
  });

  if (compact) {
    Object.assign(layer.style, {
      width: '36px',
      height: '36px',
      left: '0',
      top: '50%',
      transform: 'translateY(-50%)',
      border: '1px solid var(--border)',
      borderRadius: '5px',
    });
  } else {
    Object.assign(layer.style, {
      left: '0',
      right: '0',
      top: '0',
      height: '104px',
      borderBottom: '1px solid var(--border)',
    });
  }

  const image = document.createElement('img');
  image.src = approvedSpriteUrl;
  image.alt = '';
  image.draggable = false;
  Object.assign(image.style, {
    position: 'absolute',
    display: 'block',
    width: '500%',
    height: '300%',
    maxWidth: 'none',
    left: `${-cell.column * 100}%`,
    top: `${-cell.row * 100}%`,
  });

  image.addEventListener('error', () => {
    console.error('UI-POS-001 approved product sprite failed to load', approvedSpriteUrl);
  });

  layer.append(image);
  return layer;
}

function paintProductImages() {
  document
    .querySelectorAll<HTMLElement>('.pos-product-visual[aria-label^="Imagen de referencia aprobada"]')
    .forEach((visual) => {
      const label = visual.getAttribute('aria-label') ?? '';
      const cell = findCell(label);
      if (!cell) return;

      const compact = visual.classList.contains('compact');
      const host = compact
        ? visual.closest<HTMLElement>('.pos-line-product')
        : visual.closest<HTMLElement>('article.pos-product-card');

      if (!host || host.querySelector('.pos-runtime-product-image')) return;

      host.style.position = 'relative';
      visual.style.opacity = '0';
      visual.setAttribute('aria-hidden', 'true');

      if (!compact) {
        const body = host.querySelector<HTMLElement>('.pos-product-body');
        if (body) {
          body.style.position = 'relative';
          body.style.zIndex = '2';
          body.style.background = 'var(--surface)';
        }
      }

      host.prepend(buildLayer(cell, compact));
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
