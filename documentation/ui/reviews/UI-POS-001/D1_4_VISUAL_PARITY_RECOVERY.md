# UI-POS-001 — D1.4 Visual Parity Recovery

Status: `IMPLEMENTATION_IN_PROGRESS / VISUAL_REVIEW_REQUIRED`

Approved visual authority:

`documentation/ui/references/approved/UI-POS-001/v3-theme-pair/`

## Why this increment exists

Runtime review after PR #118 showed two material deviations from the approved reference pair:

1. the light mode did not reproduce the approved composition closely enough;
2. the governed product visuals were present in Git but did not render in the deployed catalog or sale lines.

These deviations are treated as implementation defects, not accepted design changes.

## Recovery scope

- move the global header above the sidebar/workspace to match the approved composition;
- make the light sidebar light and the dark sidebar deep navy;
- add a functional global POS search bound to the existing catalog query state;
- tune desktop proportions toward the approved catalog/sale split;
- allow five product columns when viewport width permits;
- expand mock catalog data to the 15 products represented by the approved visual sprite, while keeping the existing service item for the supported mixed-sale capability;
- map presentation visuals by stable mock catalog code;
- render sprite cells through an actual SVG `image` element instead of CSS `background-image`;
- reference the exact approved sprite blob from `src/assets` so Vite fingerprints and bundles it;
- keep prices absent from catalog cards because `CommercialItemDto` still exposes no authoritative selling-price field.

## Exact asset integrity

Approved sprite Git blob:

`8e0523a24a43bad000d16057cf2404294c4921a0`

The same blob is reused at:

`src/WebApp/src/assets/ui-pos-001-v3-product-sprite.webp`

No product image was regenerated.

## Functional boundaries retained

This increment does not add catalog price authority, stock authority, tax calculation, automatic CFE selection, DGI acceptance, payment discovery, cancellation, durable confirmation, fiscal representation download, or authoritative offline behavior.

## Visual gate

This PR must not be merged solely because CI is green. Before merge, the D1.4 implementation must be visually reviewed against the approved light and dark baselines, with special attention to product-image visibility and light-mode composition.
