# UI-SUPPLIER-001 — Suppliers Reconciliation

Status: `RECONCILED / SPECIFICATION_READY_WITH_DEPENDENCY_GAPS`

Upstream interface scope: `WEB-005` — Suppliers

Reconciled against: `main@c59e053d40a78704035e559368f4857f587c04e7`

## Decision

`WEB-005` is promoted to:

```text
WEB-005 -> UI-SUPPLIER-001
```

The first governed supplier view covers the Party master profile with `SUPPLIER` role, identities, addresses and contacts. Procurement, payable and received-document relationships remain future integrations unless backed by an implemented projection.

## Requirements trace

Upstream requirements:

- `FR-010` — customers and suppliers with normalized fiscal/document identity and contacts;
- `FR-011` — tested Uruguayan identifier validation while preserving allowed foreign identities;
- `FR-067` — purchase-order and goods-receipt workflows;
- `FR-071` — accounts payable linked to supplier/source evidence;
- `FR-081` — received CFE preserves original artifact/hash/source and duplicate detection.

For v1, only the Party-master subset is executable from current public API evidence. Requirements `FR-067`, `FR-071` and `FR-081` remain traceable dependencies, not fabricated supplier-summary widgets.

## Current implemented dependencies

`PartiesController` provides the real supplier-master surface:

| API ID | operationId | Route | Permission | UI disposition |
| --- | --- | --- | --- | --- |
| `API-PTY-001` | `listParties` | `GET /api/v1/parties?role=SUPPLIER` | `parties.read` | `SUPPORTED` |
| `API-PTY-002` | `createParty` | `POST /api/v1/parties` | `parties.manage` | `SUPPORTED` |
| `API-PTY-003` | `getParty` | `GET /api/v1/parties/{partyId}` | `parties.read` | `SUPPORTED` |
| `API-PTY-004` | `updateParty` | `PATCH /api/v1/parties/{partyId}` | `parties.manage` | `SUPPORTED` |
| `API-PTY-005` | `addPartyFiscalIdentity` | `POST /api/v1/parties/{partyId}/fiscal-identities` | `parties.fiscal.manage` | `SUPPORTED` |
| `API-PTY-006` | `updatePartyFiscalIdentity` | `PUT /api/v1/parties/{partyId}/fiscal-identities/{identityId}` | `parties.fiscal.manage` | `SUPPORTED` |
| `API-PTY-007` | `setPartyRoles` | `PUT /api/v1/parties/{partyId}/roles` | `parties.manage` | `SUPPORTED` |

Addresses and contacts travel within Party create/update.

## Contracted or target dependencies not available as one executable supplier summary

- `API-PTY-008 getPartyAccountSummary` remains deferred;
- purchase-order/goods-receipt workflow belongs to `WEB-009` and must use its own governed API/view when ready;
- accounts-payable obligations belong to `WEB-011` and must not be synthesized from Party master data;
- received-CFE/source-evidence relationships belong to the received-fiscal-document domain and are not exposed here as a supplier-centric aggregate;
- reference-data catalogs such as country/fiscal-identity-type endpoints remain contracted but not evidenced as implemented public routes in this baseline.

## Design constraints

### 1. Supplier is a Party role

A Party may be both `CUSTOMER` and `SUPPLIER`. The UI must preserve this without duplicate masters.

### 2. Supplier v1 is not a procurement dashboard

No purchase-order count, pending receipt, outstanding payable, aging or last invoice card is live in v1 unless an authoritative projection exists.

### 3. Source-document relationships stay visible as future navigation

The visual design may reserve future destinations such as `Compras`, `Cuentas por pagar` or `Documentos recibidos`, but must not populate fake metrics.

### 4. Fiscal identity and contact semantics reuse Party rules

Person/organization, multiple identities, validity, issuing country, address kinds and primary contacts behave exactly under the Party contract.

## Traceability gap

No dedicated governed supplier-master `US-*` or `UC-SUPPLIER-*` lifecycle is currently evidenced. This reconciliation records the gap instead of inventing IDs.

## Outcome

`UI-SUPPLIER-001` is ready for a functional specification and a first visual draft focused on supplier master data. Procurement/payables/source-document summary remains explicitly outside the executable v1 boundary.
