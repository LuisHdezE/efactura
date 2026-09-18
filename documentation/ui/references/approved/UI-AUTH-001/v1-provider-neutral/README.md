# UI-AUTH-001 — v1-provider-neutral

Status: `VISUAL_APPROVED / SOURCE_FINGERPRINTED / BINARY_PRESERVATION_PENDING`

This directory records the exact visual version approved by Luis for the provider-neutral web session-entry experience.

## Approval

- UI ID: `UI-AUTH-001`
- Approved version: `v1-provider-neutral`
- Approval date: `2026-09-17`
- Approval statement: `aprobado`
- Visual intent: paired light/dark access screen, provider-neutral authentication handoff
- Primary CTA: `Continuar al acceso seguro`

## Approved source fingerprint

The approved source was generated as a PNG in the visual-review conversation.

- source filename: `a_clean_split_panel_ui_presentation_mockup_high_r.png`
- dimensions: `1536 × 1024`
- bytes: `1831942`
- SHA-256: `ed2a9039dd60c8eff2398197f48b6cc1d71d8c3457630fedb0998d2615584834`
- image generation id: `53dc6741-8d31-4c0d-8dd6-7d001c5cdbb7`

The SHA-256 above is the authority for the exact approved source bytes. A visually similar regeneration is **not** equivalent to the approved version.

## Composition authority

The approved visual establishes:

- split light/dark theme pair;
- eFactura branding and Uruguay context;
- hero message `Tu facturación, más simple`;
- provider-neutral card titled `Acceso seguro`;
- primary CTA `Continuar al acceso seguro`;
- explicit notice that the user is redirected to a secure identity provider;
- no local email/password fields;
- no password-recovery UI;
- no provider-specific logo or provider claim;
- supporting benefits for simple operation, security and maintained fiscal alignment;
- responsive-friendly two-column desktop composition that can collapse to a single-column mobile flow;
- footer with product identity and secondary informational links.

## Functional boundary

Visual approval does not authorize implementation of an identity provider, token acquisition, refresh, logout, password handling or API-owned credentials.

The real authentication integration remains pending the accepted identity-provider/deployment decision and an executable application-context path.

## Binary preservation note

The exact source bytes are fingerprinted above. Direct binary insertion through the current repository connector is still pending; this record prevents silent regeneration or substitution and must be updated when the byte-identical PNG is physically added to Git.

Until then, implementation must treat the approved conversation visual plus this fingerprint/composition record as the visual authority.
