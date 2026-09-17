# Binary asset integrity

`UI-POS-001 v3-theme-pair` was visually approved before the repository binary-preservation step. Runtime investigation later proved that the committed WebP baselines and product sprite were truncated during that preservation step, so those blobs are not valid renderable assets.

## Recovered governed product assets

The product artwork was recovered from the healthy retained visual source and stored as **15 individual 128×128 PNG files**. Each product file is preserved twice using the **same Git blob**:

- governed reference: `documentation/ui/references/approved/UI-POS-001/v3-theme-pair/products/`
- runtime asset: `src/WebApp/public/assets/products/`

| Asset | Git blob SHA | Bytes |
| --- | --- | ---: |
| `papas-fritas-clasicas-100g.png` | `cf9881ebec12b0c6cf9a0db5c9e5177b64885787` | 3226 |
| `agua-mineral-500ml.png` | `f4f880fcce35e6b70f22526ac47752d241e06250` | 2244 |
| `coca-cola-600ml.png` | `fb2ede9b411c800d4ff6ba63e726f5701814d7c2` | 1745 |
| `leche-entera-1l.png` | `25bfe56dcba2044c9fc8a13bdf0bc1d335ed5da2` | 2636 |
| `pan-frances-kg.png` | `d89e12c6d745f262fb277b597a07e682424b1562` | 2575 |
| `queso-mozzarella-kg.png` | `c6cc2647f923ffb263556c4d1aa759434efbbc2d` | 2477 |
| `jamon-cocido-kg.png` | `15c46ee752997db05e1a215a239c70434f60a78f` | 2877 |
| `banana-kg.png` | `e7e740d26f09aae50098c74ddec8d0e78b94597a` | 1870 |
| `detergente-750ml.png` | `e49a82847255298f09c1143834025e5259a34031` | 1806 |
| `papel-higienico-4u.png` | `86b5b58c2bfc6034b61073f041098970a16bdb4d` | 3321 |
| `galletitas-surtidas-300g.png` | `25e6dcf24e9e52014b5babd45e6e5c1660c2e7a4` | 2723 |
| `cafe-molido-500g.png` | `de3f4bb0ae731a31104ea7890b34d61d84e8f41e` | 2529 |
| `yerba-mate-1kg.png` | `f75f9fd41946adf0504c673acc2825dce97e905f` | 2295 |
| `azucar-1kg.png` | `954576d9453e36020cff0109e16f7213c5cb7a60` | 2601 |
| `aceite-girasol-1l.png` | `0fd865a9c5848a01d9fe170e803b9f2a53c79190` | 2349 |

The exact runtime mapping and integrity metadata are recorded in `product-sprite-manifest.json` (historical filename retained for compatibility).

## Governed service asset

`SERV-001 — Servicio de entrega` now uses a presentation-only SVG stored twice with the **same Git blob**:

- governed reference: `documentation/ui/references/approved/UI-POS-001/v3-theme-pair/services/servicio-entrega.svg`
- runtime asset: `src/WebApp/public/assets/services/servicio-entrega.svg`

| Asset | Git blob SHA | Bytes | SHA-256 |
| --- | --- | ---: | --- |
| `servicio-entrega.svg` | `edbb78996c36adc7bb72dc01298c4d75023a0175` | 1430 | `ff8ddeb2502f402b2c39d53081a6f7a2cf5aaae81873127615885993eeeb5adc` |

The SVG uses only basic vector primitives, a fixed `128×128` viewBox and no external resources, filters, embedded raster content or browser-specific features. This removes the binary-decoding ambiguity observed with the previous service PNG while retaining the existing `object-fit: contain` renderer and safe visual margin.

The change is presentation-only. It does not add or imply logistics, fleet, delivery-tracking or media capabilities to the API.

## Corrupted or superseded historical blobs

| Asset | Historical Git blob SHA | Repository bytes | Integrity/runtime status |
| --- | --- | ---: | --- |
| `UI-POS-001_v3-dark-baseline.webp` | `3025b9eabbc759ad31909c6e3702bf0b8593a522` | 14368 | TRUNCATED |
| `UI-POS-001_v3-light-baseline.webp` | `f52be432e90a7fd273ea595508bd6e6e4086a701` | 15009 | TRUNCATED |
| `UI-POS-001_v3-product-sprite.webp` | `8e0523a24a43bad000d16057cf2404294c4921a0` | 15018 | TRUNCATED |
| `servicio-entrega.png` (first superseded PNG) | `a34c4d68066ded906338fc4484e0cdc7e019ec98` | 3336 | VISUALLY CORRUPTED |
| `servicio-entrega.png` (PR #131 replacement) | `05daeee32694626326dcb5803e150a9493233db0` | 12408 | CROSS-BROWSER RUNTIME FAILURE / VISUAL ARTIFACTS |

Runtime evidence after PR #131 showed two distinct failures for the 12,408-byte PNG: Firefox rendered the alt text instead of the image, while a clean Chrome incognito session decoded the file but displayed visible pixel/color corruption. That evidence rules out ordinary browser cache as the primary explanation and the PNG is no longer a runtime source.

These historical fingerprints are retained only as forensic evidence of failed or superseded binary assets. They must not be used by the WebApp as runtime image sources.
