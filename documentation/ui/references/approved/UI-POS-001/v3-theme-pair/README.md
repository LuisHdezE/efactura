# UI-POS-001 v3 theme pair

Status: `VISUAL_APPROVED`

Governed identifier: `UI-POS-001 v3-theme-pair`

Approved by Luis on 2026-09-16 with the explicit instruction:

`Apruebo la vista UI-POS-001 v3-theme-pair`

This package is the authoritative visual implementation target for the POS theme pair. The approved binary assets are promoted unchanged from the previously preserved draft package.

## Approved references

- `UI-POS-001_v3-dark-baseline.webp` — dark-mode master reference.
- `UI-POS-001_v3-light-baseline.webp` — light-mode counterpart with the same layout, density and hierarchy.
- `UI-POS-001_v3-product-sprite.webp` — presentation-only reusable product imagery preserved from the reviewed baseline.
- `product-sprite-manifest.json` — stable product-to-sprite mapping.

## Required implementation fidelity

The React implementation must target the approved composition rather than the earlier spacious D1.2 styling:

- deep navy identity in dark mode;
- white/light-neutral surfaces with navy accents in light mode;
- restrained typography;
- tighter spacing and higher information density;
- wider productive catalog area;
- compact current-sale panel;
- consistent product imagery between catalog and sale lines;
- explicit `REQUIERE REVISIÓN` state for unresolved fiscal preview;
- operator-visible dark/light theme toggle.

## Theme behavior

The implementation may add presentation-only theme state with:

- dark/light design tokens;
- a visible theme toggle;
- local persistence of the operator preference;
- `prefers-color-scheme` only as the initial default when no local choice exists.

Theme selection remains presentation state and requires no backend capability.

## Product-image boundary

The approved product imagery is presentation-only mock metadata. It must remain outside `CommercialItemDto` and must not imply media upload, gallery, storage or product-media APIs.

The approved sprite/manifest exists specifically to prevent visual drift and allow the implemented demo to reuse the same product visuals.

## Functional authority boundaries remain unchanged

This approval does not authorize:

- authoritative catalog selling prices;
- invented stock quantities;
- browser-authoritative tax calculation;
- automatic CFE-family resolution;
- DGI acceptance claims;
- payment-method discovery;
- durable confirmation without governed settlement inputs;
- cancellation;
- fiscal representation download;
- authoritative offline operation.

## Immutability rule

The approved baseline must not be silently overwritten. Any material visual change creates a new governed version and requires a new explicit visual approval.
