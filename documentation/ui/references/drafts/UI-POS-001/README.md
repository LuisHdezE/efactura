# UI-POS-001 v1 visual drafts

Status: `DRAFT / NOT_VISUALLY_APPROVED / NOT_APPROVAL_READY`

View: `UI-POS-001` — Punto de Venta

Stored: `2026-09-15`

Functional specification: `documentation/ui/specifications/UI-POS-001_POS.md`

These files preserve the first generated visual exploration for the governed POS view. They are repository-optimized WebP derivatives of the generated references. They are **draft evidence only** and are not approved visual baselines.

## Stored references

| File | Role | Repository derivative |
| --- | --- | --- |
| `v1-desktop.webp` | Initial desktop POS composition | 1536×1024 WebP |
| `v1-tablet.webp` | Responsive tablet exploration | 1536×1024 WebP |
| `v1-mobile.webp` | Narrow/mobile responsive exploration | 941×1672 WebP |
| `v1-confirmed-local.webp` | `confirmed_local` state, fiscalization initiated and final DGI acceptance pending | 640×480 WebP preview derived from the generated 1448×1086 reference |

## Integrity

SHA-256 of the repository derivatives:

- `v1-desktop.webp`: `ac8780aa8ecf7bfbdaa2dfc0e7a72762edab1fe263c244da8d907e527dfd59f6`
- `v1-tablet.webp`: `088a33b9978e3abed05191f0beb7eccc5a0309bff141e02d9a0e7e2a2986d52e`
- `v1-mobile.webp`: `fdf9476e4eb8db9fd0cf654dcdd22142a6290612b78f234649e5077a3c74155a`
- `v1-confirmed-local.webp`: `6a14d94730d796a09804900480a4dc3f9748a5e5f7f22ba3253e857bde97c4d1`

## Review findings before approval

The generated v1 family is useful for composition and responsive direction, but it contains visual assumptions that conflict with the current authoritative backend boundary. These are recorded instead of being silently normalized.

1. **Catalog selling price is not authoritative yet.** Some product cards/rows render prices as if `listItems` supplied them. The current catalog DTO does not expose a selling price; `UnitPrice` is entered on the Sale line. An approval-ready version must not imply a catalog price source that does not exist.
2. **Exact stock quantities are not supplied by the current catalog projection.** Some drafts render numeric stock such as `Stock: 120` or a `Stock` column. `listItems` exposes inventory-tracking metadata, not the authoritative current stock quantity used in these mockups. Exact stock figures must be removed or bound to a separately evidenced inventory API before approval.
3. **Payment discovery is incomplete.** Some generated copy implies that the POS directly manages payment selection. `listPaymentMethods` remains a contracted but pending POS dependency, so no payment-method selector may appear operational in the approval-ready reference.
4. **`Agregar nota a la venta` is not part of the current Sale contract/specification.** That action must be removed unless a later governed requirement/API contract introduces it.
5. **Confirmation must stop at local confirmation semantics.** The `confirmed_local` exploration correctly distinguishes `Venta confirmada / Fiscalización iniciada` from final DGI acceptance. It must continue to avoid presenting the local confirmation receipt as a final CFE acceptance.
6. **Confirmation detail labels must match the actual response.** The current response exposes Sale id/version, confirmation fingerprint, settlement fingerprint, `FiscalizationRequestId`, payment count, optional receivable id, timestamp and replay indicator. Any synthetic `ID de cobro` label must be removed or mapped only when backed by an actual response field.
7. **POS context must not look magically bootstrapped.** Location/terminal context is required while the dedicated `getPosBootstrap` dependency remains pending. The approval-ready reference must make the context source honest and explicit.

## Approval state

- Approved version: `NONE`
- Approved artifact: `NONE`
- Approval date: `NONE`
- Approval statement: `NONE`

There is intentionally no artifact under `documentation/ui/references/approved/UI-POS-001/` yet.

The next visual iteration should be a corrected `v2` family aligned strictly with current backend capability. Only an exact human approval identifying the view and version can later promote a reference to the approved baseline.