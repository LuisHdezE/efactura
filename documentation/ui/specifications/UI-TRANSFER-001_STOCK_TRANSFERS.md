# UI-TRANSFER-001 - Stock Transfers

Status: `FUNCTIONAL_SPECIFICATION / VISUAL_APPROVED / EXECUTION_BLOCKED_BY_API / BINARY_PRESERVATION_PENDING`

Mapping:

```text
WEB-008 -> UI-TRANSFER-001
```

Reserved route:

```text
/transferencias
```

Navigation state: `PLANNED_DISABLED`

## 1. Purpose

Provide the governed WebApp surface for inter-location stock transfers, covering the target lifecycle from transfer request through approval, dispatch, receipt and explicit discrepancy reconciliation.

This specification is authoritative for visual design while `API-TRF-001..007` remain non-executable through WebApi. The frontend must not activate `/transferencias` or present mock data/actions as live server behavior before that gate.

## 2. Functional authority

The view is derived from:

- `WEB-008 - Stock Transfers`;
- `FR-061` - transfer is an explicit inventory movement source;
- `FR-063` - transfer/dispatch/receipt workflow with discrepancy handling;
- `UC-INV-002 - Transfer stock between locations`;
- accepted `API-TRF-001..007` contracts;
- `UI-TRANSFER-001_RECONCILIATION.md`.

The API completion matrix remains the execution authority. Contract acceptance alone is not implementation evidence.

## 3. Intended operators

Primary roles from the interface baseline:

- inventory operator;
- administrator.

Read authority requires `inventory.read`.

Transfer-changing actions require `inventory.transfer`.

The UI must eventually respect organization/location scope from the authenticated actor context. No visual design may imply unrestricted cross-location authority.

## 4. Information architecture

### 4.1 Page header

Show:

- title: `Transferencias`;
- concise operational subtitle;
- lifecycle/API availability indicator only when needed for the demo/governance state;
- primary `Nueva transferencia` action location reserved visually.

Until API execution exists, any rendered prototype button is illustrative/disabled and must not imply a working command.

### 4.2 Transfer list

The approved visual baseline provides an operational list/table with columns or responsive equivalents for:

- transfer reference/ID;
- source location;
- destination location;
- lifecycle state;
- line count or compact item summary only when present in visual/mock data;
- last relevant lifecycle timestamp only as illustrative presentation unless the executable DTO later exposes it.

The UI must avoid inventing contractual fields such as carrier, ETA, freight cost or monetary transfer value.

### 4.3 Detail panel / detail surface

Selecting a transfer should reveal:

- transfer identity;
- source and destination;
- lifecycle progression;
- line items with requested quantities;
- received quantities/discrepancy presentation when the lifecycle reaches receipt/reconciliation;
- reason/reference context when available;
- version/concurrency metadata only if exposed by the eventual HTTP contract.

The visual may reserve a place for metadata without manufacturing values that look authoritative.

### 4.4 Lifecycle rail

The visual language should make the target progression legible:

1. requested;
2. approved;
3. dispatched;
4. received;
5. discrepancy reconciliation when applicable.

These labels represent governed lifecycle concepts, not final API wire enum guarantees.

### 4.5 Action area

The layout may reserve contextual actions for:

- create transfer;
- approve;
- dispatch;
- receive;
- reconcile discrepancy.

Action availability must be state- and permission-aware once executable API evidence exists.

For the current visual-only phase, all such controls are non-authoritative and should be visually marked as prototype/disabled if shown in a running WebApp context.

## 5. Create-transfer visual flow

The approved visual direction may be extended with a drawer/modal/page composition containing:

- source location;
- destination location;
- one or more transfer lines;
- item identifier/label enrichment;
- requested quantity;
- reason/reference.

Source and destination must be visibly distinct.

The design must not invent a server rule such as source != destination validation text unless later confirmed by executable backend behavior, even though the target workflow logically represents an inter-location transfer.

## 6. Dispatch and receipt visual flow

Dispatch should communicate that source stock effects belong to the server transition boundary.

Receipt should support visual comparison between requested and actually received quantities, because the accepted contract permits receipt discrepancy evidence.

The UI must not silently normalize mismatches. A mismatch should remain visible and feed the explicit reconciliation concept.

## 7. Discrepancy reconciliation

When the visual represents a discrepancy, show:

- requested quantity;
- received quantity;
- difference;
- reconciliation action location;
- reason/evidence area.

Do not define final discrepancy categories, auto-resolution policy or adjustment rules until the executable API exposes them.

## 8. Concurrency and idempotency

Mutation transitions are idempotent and version-aware by contract.

The future executable UI must:

- generate/preserve an idempotency key per command attempt according to the shared client infrastructure;
- submit expected version where required;
- surface `409`-style conflict outcomes rather than silently overwriting newer state;
- refresh/reconcile server state before retrying a transition.

The approved visual grammar should leave room for non-destructive conflict/error feedback.

## 9. States

The governed UI should ultimately cover at least:

- populated list;
- selected transfer detail;
- empty state;
- loading skeleton/state;
- generic error;
- forbidden/permission state;
- conflict/version state concept;
- validation error concept;
- discrepancy state;
- narrow responsive layout.

Because the HTTP implementation is missing, exact error payload copy remains non-authoritative.

## 10. Responsive behavior

Desktop:

- list + detail/master-detail composition preferred;
- lifecycle rail visible without overwhelming line data.

Tablet:

- preserve operational actions and lifecycle clarity;
- stack detail sections when horizontal space is constrained.

Mobile:

- transfer list becomes cards/compact rows;
- detail becomes a single-column inspection flow;
- lifecycle becomes vertical or horizontally scrollable without losing textual labels;
- actions remain reachable without relying on hover.

## 11. Light/dark requirements

The approved baseline provides light and dark parity with the established eFactura shell across desktop, tablet and mobile representations.

Status/lifecycle meaning must not depend on color alone. Use labels, icons and/or shape in addition to color.

## 12. Accessibility

Required design properties:

- keyboard-operable list/detail/actions;
- programmatic labels for quantities and locations;
- lifecycle transitions announced with textual state;
- discrepancies conveyed numerically and textually, not color-only;
- visible focus states;
- validation and conflict feedback associated with the relevant control/context.

## 13. Explicit exclusions for v1 visual authority

The visual baseline does not authorize:

- procurement or goods-receipt workflow;
- EOQ/ROP/replenishment;
- inventory valuation/cost layers;
- carrier/logistics tracking;
- arbitrary cancellation/deletion/edit-history rewrite;
- export functionality;
- automatic discrepancy resolution;
- reserved/available-to-sell calculations;
- unrestricted location access;
- any live API claim while `API-TRF-*` remains `MISSING_HTTP`.

## 14. Approved visual baseline

Luis explicitly approved the exact visual candidate on 2026-09-20 with:

> Apruebo UI-TRANSFER-001 v1-responsive-suite como baseline visual

Approved artifact:

`documentation/ui/references/approved/UI-TRANSFER-001/v1-responsive-suite/`

Source identity:

- generation id: `1015b786-078d-4aec-b0af-b560e26352ea`;
- source filename: `a_clean_ui_ux_dashboard_screenshot_collage_showing.png`;
- dimensions: `1312 x 1199`;
- source bytes: `1586620`;
- SHA-256: `1c6826ce147e7fe4a4dd009c67ebdbd41275ed43bc6040be2cd81ac1dd8272ed`.

The prior candidate record under `references/drafts/UI-TRANSFER-001/v1-responsive-suite/` is preserved as governance history.

The generated source is fingerprinted, but physical byte-identical storage in Git remains `BINARY_PRESERVATION_PENDING` because the current repository connector cannot directly insert the generated PNG from the chat runtime.

This approval authorizes the visual direction only. It does not authorize React route activation or live integration.

## 15. Implementation gate

`/transferencias` remains `PLANNED_DISABLED` while `API-TRF-001..007` are `MISSING_HTTP`.

The next executable implementation gate requires:

- transfer HTTP operations to become executable;
- a fresh reconciliation against the implemented DTO/state/transition surface;
- permission/location-scope handling;
- shared-shell React implementation;
- normal merge, deployment and runtime-review gates.

Any disagreement between illustrative baseline details and later executable API truth is resolved in favor of the functional/API contract while preserving the approved visual grammar where compatible.
