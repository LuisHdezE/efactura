# UI-SUPPLIER-001 — Suppliers Reconciliation

Status: `RECONCILED / SPECIFICATION_READY_WITH_TRACEABILITY_GAPS`

Upstream interface scope: `WEB-005` — Suppliers

Reconciled against: `main@cf895c8d292884762c0aeb235114c2f8e571a193`

## Decision

`WEB-005` is promoted to the governed UI boundary:

```text
WEB-005 -> UI-SUPPLIER-001
```

The governed route is reserved as:

```text
/proveedores
```

The first governed view covers supplier master-data search, detail, creation and controlled maintenance over the existing unified `Party` model, using the `SUPPLIER` role. It does not create a separate Supplier aggregate and it does not fabricate procurement, accounts-payable, received-CFE or source-document projections that are not currently exposed through executable contracts.

## Upstream scope preserved

The accepted Interface Scope Baseline defines `WEB-005` with:

- module: `Parties`;
- purpose: manage supplier role, fiscal identities, contacts and source-document relationships;
- requirements: `FR-010`, `FR-011`, `FR-067`, `FR-071`, `FR-081`;
- intended data: supplier role/profile and payable/source summary.

This reconciliation preserves that target scope but narrows the first executable UI to capabilities currently evidenced in the repository.

`FR-067`, `FR-071` and `FR-081` remain relevant target traceability, but their purchase-order, payable/source-evidence and received-CFE aspects are not promoted to live supplier widgets until corresponding executable read models/endpoints exist.

## Requirements trace

Current relevant requirements evidenced in the target baseline include:

- `FR-010` — customers/suppliers with normalized fiscal/document identity and contacts;
- `FR-011` — tested validation of Uruguayan identifiers while preserving allowed foreign identities;
- `FR-067` — purchase order and goods receipt workflows;
- `FR-071` — accounts payable linked to supplier/source evidence;
- `FR-081` — received CFE preserves original artifact/hash/source and duplicate detection.

The first version directly realizes the supplier master-data portion of `FR-010`/`FR-011`. It must not imply implementation of the downstream procurement/payables/CFE portions of the other requirements.

## Use-case and user-story gap

No dedicated governed `UC-SUPPLIER-*` lifecycle and no governed `US-*` supplier-master artifact are currently evidenced in the repository.

This reconciliation records that gap instead of inventing missing traceability. The UI may progress through specification, visual approval and implementation while the independent traceability gap remains recorded.

## Current implemented dependencies

Supplier master data uses the same `PartiesController` already used for customers.

| API ID | operationId | Route | Permission | UI disposition |
| --- | --- | --- | --- | --- |
| `API-PTY-001` | `listParties` | `GET /api/v1/parties` | `parties.read` | `SUPPORTED` |
| `API-PTY-002` | `createParty` | `POST /api/v1/parties` | `parties.manage` | `SUPPORTED` |
| `API-PTY-003` | `getParty` | `GET /api/v1/parties/{partyId}` | `parties.read` | `SUPPORTED` |
| `API-PTY-004` | `updateParty` | `PATCH /api/v1/parties/{partyId}` | `parties.manage` | `SUPPORTED` |
| `API-PTY-005` | `addPartyFiscalIdentity` | `POST /api/v1/parties/{partyId}/fiscal-identities` | `parties.fiscal.manage` | `SUPPORTED` |
| `API-PTY-006` | `updatePartyFiscalIdentity` | `PUT /api/v1/parties/{partyId}/fiscal-identities/{identityId}` | `parties.fiscal.manage` | `SUPPORTED` |
| `API-PTY-007` | `setPartyRoles` | `PUT /api/v1/parties/{partyId}/roles` | `parties.manage` | `SUPPORTED` |

The list contract explicitly supports `role=SUPPLIER`, and the server validates Party roles as `CUSTOMER` or `SUPPLIER`.

Addresses and contacts are part of Party create/update projections and do not require invented child routes.

## Reference-data dependencies now executable

The current API exposes authenticated reference-data endpoints relevant to supplier maintenance:

| API ID | operationId | Route | UI disposition |
| --- | --- | --- | --- |
| `API-REF-001` | `listCountries` | `GET /api/v1/reference-data/countries` | `SUPPORTED` |
| `API-REF-002` | `listUruguayDepartments` | `GET /api/v1/reference-data/uruguay-departments` | `SUPPORTED` |
| `API-REF-003` | `listFiscalIdentityTypes` | `GET /api/v1/reference-data/fiscal-identity-types` | `SUPPORTED` |
| `API-REF-004` | `listCurrencies` | `GET /api/v1/reference-data/currencies` | `SUPPORTED`, not required by supplier v1 master data |

This is an important improvement over the older Customer reconciliation baseline: country and fiscal-identity-type selectors may now be designed against real authenticated reference contracts rather than conceptual placeholders.

## Contracted but not executable enough for supplier v1

| Capability | Current evidence | UI disposition |
| --- | --- | --- |
| `API-PTY-008 getPartyAccountSummary` | `MISSING_HTTP` in Wave 2 matrix | `PENDING` |
| Supplier payable aging/balance | no executable supplier-specific projection evidenced | `PENDING` |
| Purchase order / goods receipt summary | no executable Purchase Orders HTTP surface evidenced in current `main` | `PENDING` |
| Supplier source-document relationship summary | target scope only; no supplier-specific read projection evidenced | `PENDING` |
| Received CFE/source evidence summary | separate future CFE capability; not a Party master projection | `PENDING` |

Therefore the first supplier view must not show live debt, aging, outstanding invoices, purchase-order totals, received-CFE counts or source-document history as if these were authoritative server data.

## Important design constraints

### 1. Supplier is a Party role

A supplier is a `Party` with the `SUPPLIER` role. A Party may simultaneously hold `CUSTOMER` and `SUPPLIER`.

The UI must not duplicate a Party merely because it participates in both commercial directions.

### 2. Supplier list filter

The primary list must call/read conceptually as:

```text
GET /api/v1/parties?role=SUPPLIER
```

Search, active filter, paging and detail behavior remain Party semantics.

### 3. Person and organization remain valid

`PartyKind` is `PERSON` or `ORGANIZATION`. The supplier experience may visually prioritize organizations, but it must not prohibit valid person suppliers.

### 4. Fiscal identities are multi-valued

Each Party may have multiple fiscal identities with type, number, issuing country, validity dates and active state.

The UI can now use the authenticated fiscal-identity-type and country reference endpoints, while backend validation remains authoritative.

### 5. Addresses and contacts are first-class data

Addresses support `FISCAL`, `DELIVERY` and `OTHER`; contacts preserve type/value/primary semantics.

For suppliers, fiscal and operational contact presentation may be emphasized, but no unsupported procurement contact hierarchy may be invented.

### 6. Financial/procurement summaries remain out of scope for v1

No card labelled as payable balance, aging, pending purchase orders or source-document total may be live in v1 until an executable authoritative projection exists.

A future integration area may be visually reserved only if it is explicitly marked unavailable/planned and does not display fabricated figures.

## Reconciliation outcome

`UI-SUPPLIER-001` is sufficiently bounded for functional specification and visual drafting.

The supported first version is a supplier-oriented Party master experience with:

- supplier-filtered list/search;
- master-detail inspection;
- Party kind, active state and version;
- residence and tax residence;
- roles;
- fiscal identities;
- addresses;
- contacts;
- create/update semantics;
- real country/fiscal-identity reference support at contract level.

The view intentionally excludes authoritative procurement/payable/source-document summaries until their API surface exists.
