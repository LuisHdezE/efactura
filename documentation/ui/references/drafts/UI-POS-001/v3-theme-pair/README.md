# UI-POS-001 v3 theme pair

Status: `VISUAL_DRAFT / PROMOTED_TO_VISUAL_APPROVED`

This package preserves the visual direction selected during D1 visual-fidelity recovery before any corresponding React implementation is changed.

## Included references

- `UI-POS-001_v3-dark-baseline.webp` — dark-mode master reference.
- `UI-POS-001_v3-light-baseline.webp` — light-mode counterpart using the same layout and visual density.
- `UI-POS-001_v3-product-sprite.webp` — presentation-only reusable product imagery extracted from the light reference for closer implementation fidelity.
- `product-sprite-manifest.json` — stable product-to-sprite coordinates.

The repository copies are 1280×720 WEBP derivatives of the reviewed 1672×941 visual references. They preserve the composition while keeping repository weight reasonable.

## Visual direction

The implementation target represented here is intentionally more compact than the currently deployed D1.2 POS:

- deep navy identity in dark mode;
- white/light-neutral surfaces with navy accents in light mode;
- restrained typography;
- tighter spacing and higher information density;
- wider productive catalog area;
- compact current-sale panel;
- consistent product imagery between catalog and sale lines;
- explicit `REQUIERE REVISIÓN` state for unresolved fiscal preview;
- dark/light theme toggle as presentation behavior only.

## Theme behavior intended for implementation

The approved WebApp implementation may add:

- dark/light theme tokens;
- an operator-visible theme toggle;
- local persistence of the preference;
- `prefers-color-scheme` only as the initial default when no local choice exists.

Theme selection is presentation state. It does not require or imply any backend/API capability.

## Product-image boundary

The product sprite is presentation-only mock metadata. It must not be added to `CommercialItemDto`, must not imply a media-management API, and must not create upload/gallery/persistence behavior.

The sprite exists solely so the implemented demo can reuse the same product visuals and reduce drift from the governed reference.

## Functional authority boundaries remain unchanged

This package does not authorize:

- catalog selling prices as authoritative backend data;
- invented stock quantities;
- browser-authoritative tax calculation;
- automatic CFE-family resolution;
- DGI acceptance claims;
- payment-method discovery;
- durable confirmation without governed settlement inputs;
- cancellation;
- fiscal representation download;
- authoritative offline operation.

## Governance note

Luis explicitly approved the exact governed identifier:

`UI-POS-001 v3-theme-pair`

The authoritative promoted package now lives at:

`documentation/ui/references/approved/UI-POS-001/v3-theme-pair/`

The approved dark baseline, light baseline, product sprite and manifest are preserved with the same Git blob identities as this draft package.
