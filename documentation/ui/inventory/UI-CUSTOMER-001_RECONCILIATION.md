# UI-CUSTOMER-001 — Customers and Parties Reconciliation

Status: `RECONCILED / SPECIFICATION_READY_WITH_TRACEABILITY_GAPS`

Upstream interface scope: `WEB-004` — Customers and Parties

Reconciled against: `main@c59e053d40a78704035e559368f4857f587c04e7`

## Decision

`WEB-004` is promoted to the governed UI boundary:

```text
WEB-004 -> UI-CUSTOMER-001
```

The first governed view covers customer master-data search, detail, creation and controlled maintenance of identities, addresses, contacts and roles. It does not fabricate account-balance projections or reference-data catalogs that are only contracted.

## Requirements trace

Primary requirements evidenced in the accepted target baseline:

- `FR-010` — customers/suppliers with normalized fiscal/document identity and contacts;
- `FR-011` — tested validation of Uruguayan identifiers while preserving allowed foreign identities;
- `FR-016` — identity type, number and issuing country are independent from residence/tax-residence country;
- `FR-017` — multiple fiscal identities where valid;
- `FR-018` — identity type/country combinations governed by versioned metadata/rules;
- `FR-019` — issued documents preserve the exact receiver identity snapshot and are not rewritten by later customer-master edits.

Related security/non-functional concerns include least privilege, auditability, privacy and version-safe mutation.

## Use-case and user-story gap

No dedicated governed `UC-PTY-*`/`UC-CUSTOMER-*` lifecycle and no governed `US-*` customer-master artifact are currently evidenced in the repository.

This reconciliation does not invent them. Existing sale use cases consume customer/receiver context, but they are not substitutes for a dedicated master-data lifecycle. The gap must be closed before `UI-CUSTOMER-001` reaches final `ACCEPTED` status.

## Current implemented dependencies

The following operations are implemented in `PartiesController` and usable by the UI:

| API ID | operationId | Route | Permission | UI disposition |
| --- | --- | --- | --- | --- |
| `API-PTY-001` | `listParties` | `GET /api/v1/parties` | `parties.read` | `SUPPORTED` |
| `API-PTY-002` | `createParty` | `POST /api/v1/parties` | `parties.manage` | `SUPPORTED` |
| `API-PTY-003` | `getParty` | `GET /api/v1/parties/{partyId}` | `parties.read` | `SUPPORTED` |
| `API-PTY-004` | `updateParty` | `PATCH /api/v1/parties/{partyId}` | `parties.manage` | `SUPPORTED` |
| `API-PTY-005` | `addPartyFiscalIdentity` | `POST /api/v1/parties/{partyId}/fiscal-identities` | `parties.fiscal.manage` | `SUPPORTED` |
| `API-PTY-006` | `updatePartyFiscalIdentity` | `PUT /api/v1/parties/{partyId}/fiscal-identities/{identityId}` | `parties.fiscal.manage` | `SUPPORTED` |
| `API-PTY-007` | `setPartyRoles` | `PUT /api/v1/parties/{partyId}/roles` | `parties.manage` | `SUPPORTED` |

Addresses and contacts are currently part of Party create/update projections and therefore do not require invented child routes.

## Contracted but not currently evidenced as executable

| API ID | operationId | Contracted route | UI disposition |
| --- | --- | --- | --- |
| `API-PTY-008` | `getPartyAccountSummary` | `GET /api/v1/parties/{partyId}/account-summary` | `PENDING` |
| `API-REF-001` | `listCountries` | `GET /api/v1/reference-data/countries` | `PENDING` |
| `API-REF-003` | `listFiscalIdentityTypes` | `GET /api/v1/reference-data/fiscal-identity-types` | `PENDING` |

`API-PTY-008` is explicitly documented as deferred until receivables/payables projections exist. The UI must not fabricate balances, aging, credit exposure or outstanding-account cards from Party master data.

## Important design constraints

### 1. Customer is a Party role, not a separate destructive entity

The list should normally filter `role=CUSTOMER`. A Party may also have the `SUPPLIER` role. The UI must not imply duplicated independent customer/supplier records when one Party legitimately has both roles.

### 2. Person and organization are both supported

`PartyKind` is `PERSON` or `ORGANIZATION`. The view must not assume every customer is a person or every customer has a national ID.

### 3. Fiscal identity is multi-valued and version-sensitive

A Party may hold multiple fiscal identities. Each identity carries type code, number, issuing country, validity dates and active state. Editing customer master data must never imply rewriting historical fiscal-document receiver snapshots.

### 4. Addresses and contacts are first-class visible data

Addresses support `FISCAL`, `DELIVERY` and `OTHER`; contacts preserve type/value/primary semantics.

### 5. Reference catalogs are not yet live dependencies

The first visual draft must not show an authoritative country or fiscal-identity-type dropdown populated by an API that is not implemented. A form may reserve the area conceptually, but live implementation must use only supported data sources or explicit textual/code inputs until reference-data endpoints exist.

### 6. Account balance is out of scope for v1

No receivable/payable/account-summary widget may appear as live data in the first draft.

## Reconciliation outcome

`UI-CUSTOMER-001` is sufficiently bounded for functional specification and visual drafting because the complete Party CRUD/identity/role core is exposed. Its first visual version must be narrower than the target `WEB-004` scope where account summary and reference catalogs are still pending.
