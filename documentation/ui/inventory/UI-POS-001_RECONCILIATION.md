# UI-POS-001 — POS Sale Reconciliation

Status: `RECONCILED / SPECIFICATION_READY`

Upstream interface scope: `WEB-003` — POS Sale

Reconciled against: `main@31d29d951d575f9d0129b47f33785a5c1a4f0297`

## Decision

`WEB-003` is promoted as a governed UI boundary with stable identifier:

```text
WEB-003 -> UI-POS-001
```

This promotion defines the view boundary and traceability only. It does not approve any visual design and does not claim that every contracted POS dependency is implemented.

## Authoritative target trace

The upstream interface baseline relates `WEB-003` to:

- `FR-012`, `FR-013`, `FR-014`;
- `FR-020`, `FR-021`, `FR-022`, `FR-024`;
- `FR-030`;
- `FR-050`, `FR-052`.

Current target use-case evidence additionally binds the workflow to:

- `UC-SALE-001` — Create and validate sale draft;
- `UC-SALE-002` — Confirm cash sale and initiate fiscalization;
- `UC-SALE-003` — Confirm credit sale and create receivable, when credit terms apply;
- `UC-SALE-004` — Sell services only;
- `UC-SALE-005` — Mixed product + service sale.

`FR-023` is therefore relevant to the optional credit-settlement branch because the current confirmation API accepts credit terms and may return a receivable id. This is an implementation-era reconciliation addition to the original `WEB-003` scope trace, not a silent rewrite of the upstream baseline.

## User-story traceability gap

No governed `US-*` user-story identifier for the POS workflow is currently evidenced in the repository.

Per UI governance, this reconciliation does not invent one. The functional specification records the gap explicitly. A governed user-story artifact must be added before `UI-POS-001` can reach final UI `ACCEPTED` status.

## Current implemented dependencies

The following operations are evidenced in current WebApi controllers and are usable as real backend dependencies:

| API ID | operationId | Route | Permission | Current UI support |
| --- | --- | --- | --- | --- |
| `API-CAT-001` | `listItems` | `GET /api/v1/items` | `catalog.read` | `SUPPORTED` |
| `API-PTY-001` | `listParties` | `GET /api/v1/parties` | `parties.read` | `SUPPORTED` |
| `API-SAL-002` | `createSale` | `POST /api/v1/sales` | `sales.create` | `SUPPORTED` |
| `API-SAL-003` | `getSale` | `GET /api/v1/sales/{saleId}` | `sales.read` | `SUPPORTED` |
| `API-SAL-004` | `updateSaleDraft` | `PATCH /api/v1/sales/{saleId}` | `sales.create` | `SUPPORTED` |
| `API-SAL-005` | `validateSale` | `POST /api/v1/sales/{saleId}/validate` | `sales.create` | `SUPPORTED` |
| `API-SAL-006` | `getSaleFiscalPreview` | `GET /api/v1/sales/{saleId}/fiscal-preview` | `sales.read` | `SUPPORTED` |
| `API-SAL-007` | `confirmSale` | `POST /api/v1/sales/{saleId}/confirm` | `sales.confirm` | `SUPPORTED_WITH_SETTLEMENT_LIMITS` |

Sale create/update/validate/confirm commands use the shared idempotency contract where specified by the API inventory.

## Contracted but not currently exposed for this UI

The API contract already defines these dependencies, but current repository implementation evidence does not expose them as usable public routes for this view:

| API ID | operationId | Contracted route | UI disposition |
| --- | --- | --- | --- |
| `API-POS-001` | `getPosBootstrap` | `GET /api/v1/pos/bootstrap` | `PENDING` |
| `API-PMT-001` | `listPaymentMethods` | `GET /api/v1/payment-methods` | `PENDING` |
| `API-SAL-008` | `cancelSale` | `POST /api/v1/sales/{saleId}/cancel` | `PENDING` |
| `API-SAL-009` | `getSaleFiscalizationStatus` | `GET /api/v1/sales/{saleId}/fiscalization` | `PENDING` |
| `API-FIS-004` | `downloadFiscalRepresentation` | `GET /api/v1/fiscal-documents/{fiscalDocumentId}/representation` | `PENDING_FOR_POS_FLOW` |

A visual draft must not present these actions as operational.

## Important design constraints discovered during reconciliation

### 1. Catalog lookup does not supply an authoritative sale price

The current `CommercialItemDto` exposes item identity, code, name, kind, unit, inventory behavior, tax-profile id and category id, but no selling price.

The Sales API accepts `UnitPrice` as part of the sale line request.

Therefore the first governed POS specification MUST treat unit price as explicit operator input unless/until an authoritative pricing capability is introduced. The UI must not fabricate a catalog price or imply that `listItems` supplies one.

### 2. Immediate-payment discovery is incomplete

`confirmSale` accepts immediate-payment intents containing a `paymentMethodId`, but the contracted `listPaymentMethods` operation is not currently evidenced as an implemented public controller route.

Therefore a cash/immediate-payment selector cannot be treated as fully executable from current public API discovery. The visual design may reserve the conceptual settlement area, but unsupported payment-method choices must not be presented as live controls.

### 3. Confirmation is not fiscal completion

The current `confirmSale` response is a durable local transaction receipt ending at a `FiscalizationRequestId`. It is intentionally not a fiscal-document response and does not prove DGI acceptance.

The UI must distinguish:

```text
SALE CONFIRMED
```

from:

```text
FISCAL DOCUMENT ACCEPTED BY DGI
```

The latter cannot be shown authoritatively until the fiscalization-status path is available.

### 4. Cancellation and printing are not current POS actions

No live cancel-sale route or printable fiscal-representation route is currently available to this POS flow. They must be absent, disabled with explicit pending semantics, or deferred to a later visual version. They must not look functional.

### 5. POS bootstrap/context remains incomplete

Sale creation requires location and terminal identifiers, while the dedicated contracted `getPosBootstrap` endpoint is not currently exposed. Location listing exists elsewhere in the API, but a complete POS bootstrap/session/terminal selection flow is not yet evidenced as one executable POS dependency.

The first visual draft must therefore avoid implying invisible automatic terminal/bootstrap behavior that the current API does not establish.

### 6. Offline is not automatically enabled by the view

`FR-050` and `FR-052` require separation of client/API offline from fiscal-provider outage and idempotent future replay. They do not mean the current web POS may invent local authoritative offline sales.

No offline mutation control is approved in `UI-POS-001` v1 without a separately evidenced Client Architecture/synchronization capability.

## Reconciliation outcome

`UI-POS-001` is sufficiently bounded to receive a functional specification because the core draft/validation/preview path and local confirmation transaction exist.

Its first visual draft must be intentionally narrower than the final target POS. Pending settlement discovery, cancellation, post-confirmation fiscal status, printing and offline execution remain visible in documentation rather than being disguised in the UI.
