# W2.4 Party Account Summary HTTP implementation

Status: `IMPLEMENTED_HTTP_PENDING_PRODUCTION_MIGRATION_AND_RUNTIME_ACCEPTANCE`

Baseline: `main@9019141b10b3117a2c0cad7cd1fe723053542875` after owner-approved merge of PR #236.

API ID: `API-PTY-008`

Operation ID: `getPartyAccountSummary`

Public surface:

```text
GET /api/v1/parties/{partyId}/account-summary
permission: parties.read
idempotency: NO
```

## 1. Implementation boundary

The HTTP increment introduces a dedicated `PartyAccountSummaryController` rather than adding financial projection responsibilities to `PartiesController`.

The controller:

- resolves organization scope through the existing `V1OrganizationContextResolver`;
- captures one server-controlled `DateTimeOffset.UtcNow` instant per request;
- invokes the Application-owned `IPartyAccountSummaryReadModel` exactly once with organization, Party and that instant;
- maps the authoritative Application projection into public HTTP DTOs;
- emits `Cache-Control: private, no-store` on successful responses;
- requires `parties.read`;
- defines route name / OpenAPI operationId `getPartyAccountSummary`;
- does not require or inspect an idempotency key;
- does not accept request body, organization, `asOf`, currency or FX inputs;
- does not access EF repositories or `DbContext` directly;
- does not perform financial calculations, aging calculations, FX conversion or AR/AP netting.

## 2. Public DTO boundary

The public response records are isolated under WebApi contracts:

- `PartyAccountSummaryDto`;
- `PartyAccountCurrencyDto`;
- `PartyAccountAgingDto`.

They preserve the field-level contract locked by PR #236:

```text
organizationId
partyId
asOfUtc
receivablesApplicable
receivables[]
payablesApplicable
payables[]

currencyCode
outstanding
overdue
aging.current
aging.days1To30
aging.days31To60
aging.days61To90
aging.days91Plus
```

Applicability, currency ordering, six-decimal reconciliation, scope validation and fail-closed financial invariants remain owned by the Application composer and its authoritative AR/AP read models.

## 3. Dependency injection

WebApi now calls:

```text
AddW24PartyAccountSummaryComposition()
```

immediately after `AddV1Persistence(...)`.

That extension registers the provider-backed AR/AP balance repositories/read ports plus `IPartyAccountSummaryReadModel`. No alternate financial repository path is introduced in WebApi.

## 4. Error and authorization semantics

The endpoint relies on the existing v1 boundaries already shared by the API:

- invalid/missing authentication -> `401`;
- missing `parties.read` -> `403 permission_denied`;
- invalid organization scope -> existing organization-scope rejection;
- missing Party in resolved organization, including a Party existing only in another organization -> `404 party.not_found`;
- malformed authoritative projection -> fail closed through the existing server-error Problem Details boundary.

No partial `200` is fabricated when an applicable authoritative side fails.

## 5. QA boundary

The implementation guard verifies:

- exact route and operationId;
- exact `parties.read` permission;
- declared `200/401/403/404` HTTP surface;
- server-controlled `asOfUtc`;
- composer invocation rather than controller-local finance logic;
- `Cache-Control: private, no-store`;
- absence of idempotency handling and ETag behavior;
- absence of direct `DbContext` / EF AR/AP access from the controller;
- exact public DTO field families;
- WebApi registration of `AddW24PartyAccountSummaryComposition()`;
- preservation of the existing Party master-data controller boundary;
- Wave 2 remains 25/26 until production rollout and runtime acceptance.

The repository-wide Clean Architecture guard must also remain green, including PostgreSQL and MySQL provider-real persistence integration tests.

## 6. Production prerequisites remain gated

This implementation does **not** apply or authorize the two W2.4 production migrations:

```text
20260923043000_V1ReceivableBalanceLedger
20260923143000_V1PayableBalanceFoundation
```

They remain unapplied to Neon production until separate explicit owner approval.

The implementation PR must not be merged while the production schema prerequisites are absent because merge to `main` can trigger automatic API deployment.

Required sequence remains:

1. implementation PR reaches exact-head green CI/provider-real gate;
2. owner separately authorizes the two W2.4 migrations in Neon production;
3. migrations are applied and verified;
4. implementation merge receives a separate exact-head approval;
5. automatic API deployment completes;
6. read-only production runtime acceptance validates API-PTY-008;
7. Wave 2/global completion matrices are reconciled only after runtime acceptance.

## 7. Completion accounting

This branch contains the HTTP implementation, but the governed completion matrix intentionally remains unchanged until the endpoint is merged, deployed and runtime-accepted:

```text
Wave 2: 25 / 26
Global public v1: 67 / 194
API-PTY-008: MISSING_HTTP in the production/main completion baseline
```

After successful production runtime acceptance, reconciliation may advance to:

```text
Wave 2: 26 / 26
Global public v1: 68 / 194
```
