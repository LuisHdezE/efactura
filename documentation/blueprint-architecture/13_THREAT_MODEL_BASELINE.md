# Threat Model Baseline

## Status

Architecture-time threat model for the eFactura API target. This is a living security artifact and must be revisited when the concrete DGI/provider, certificate-custody and client deployment choices are known.

Threat modeling is applicable because the platform controls fiscal numbering/documents, payments, stock, customer/supplier fiscal identity, privileged configuration, offline replay and signing credentials.

## Protected assets

- fiscal-document identity, XML, signatures and DGI acknowledgements;
- CAE ranges/allocations;
- signing certificate/private-key access;
- customer/supplier personal and fiscal data;
- sales/payment/receivable/payable records;
- inventory positions/movements;
- roles/permissions/scopes;
- audit records;
- API/JWT/integration secrets;
- offline operation/device credentials;
- database and artifact-storage credentials.

## Trust boundaries

```text
Web/Mobile Client
    |
    | HTTPS + JWT + idempotency/correlation
    v
ASP.NET Core API Boundary
    |
    +--> ApplicationCore / business authorization boundary
    |
    +--> PostgreSQL OR MySQL persistence boundary
    |
    +--> Redis/cache boundary
    |
    +--> Artifact storage boundary
    |
    +--> Background workers / inbox-outbox boundary
    |
    +--> DGI or authorized CFE provider boundary
    |
    +--> Email/accounting/other integration boundaries
```

Offline operation introduces another trust boundary between locally queued state and later server revalidation.

## STRIDE-style threat inventory

### T-001 Spoofed user/device

Attack: stolen/replayed JWT or forged device identity executes business commands.

Mitigations:
- validated JWT issuer/audience/lifetime/signature;
- device registration/revocation for sync;
- server-side permission and organization/location scope;
- short-lived/scoped offline grants;
- audit actor/device/correlation context.

### T-002 Broken object authorization

Attack: authenticated user references another company/location/customer/fiscal document ID.

Mitigations:
- ownership/scope checks inside application use case, not UI-only filtering;
- opaque IDs do not substitute authorization;
- integration tests for cross-scope 403/404 policy.

### T-003 Privilege escalation

Attack: role/permission mutation grants fiscal/admin capability or offline queued action executes after revocation.

Mitigations:
- explicit `security.manage_roles`-style permission;
- durable audit of role/permission/scope changes;
- high-risk offline command reauthorization during sync;
- separation of system administrator from automatic fiscal privileges.

### T-004 Duplicate financial/fiscal effect

Attack/failure: retry after timeout creates duplicate sale, payment, stock movement or CFE.

Mitigations:
- idempotency key/clientOperationId + material payload hash;
- durable canonical result;
- DB uniqueness constraints;
- inbox/outbox dedupe;
- concurrency tests.

### T-005 Fiscal-number race/tampering

Attack/failure: concurrent terminals allocate same/unauthorized number, manipulate next number or bypass CAE allocation.

Mitigations:
- server-only allocator;
- transactional/locked allocation;
- unique `(company,cfe_type,series,number)` constraint;
- CAE validity/range/subrange validation;
- audit allocation/config changes;
- clients cannot submit authoritative next number.

### T-006 CFE type/tax-treatment bypass

Attack/error: client forces e-Factura/export/exempt treatment not permitted by receiver/transaction facts.

Mitigations:
- `ReceiverIdentityResolver` + `CrossBorderTaxTreatmentResolver` + `FiscalDocumentSelector` server policy;
- versioned DGI rule source;
- validation errors explain rejected selection;
- acceptance tests for foreign/RUC/export combinations.

### T-007 Secret/private-key disclosure

Attack: repository/log/error/API response leaks JWT key, DB password or certificate private key.

Mitigations:
- secret/key store;
- no committed credentials;
- structured redaction;
- private key only behind `IFiscalSigner`;
- rotation procedure and least-privilege access;
- no raw configuration endpoint.

### T-008 Malicious XML/file payload

Attack: XXE/entity expansion, oversized XML, schema abuse or malicious uploaded received CFE.

Mitigations:
- bounded request/file sizes;
- secure XML parser with DTD/external entities disabled;
- official XSD + business/signature validation;
- quarantine/reject invalid artifacts;
- never execute embedded content.

### T-009 Forged/replayed provider callback

Attack: attacker sends fake DGI/provider acceptance/rejection or replays legitimate result.

Mitigations:
- authenticated/mTLS/signature/provider-contract validation where supported;
- inbox unique external-message identity/hash;
- response interpreter whitelist/state-machine validation;
- callback invokes application use case, not direct SQL.

### T-010 Outbox worker duplication/loss

Failure: worker crashes after external submission but before local acknowledgement; repeated dispatch duplicates submission or missing work becomes stuck.

Mitigations:
- durable outbox state;
- provider/business idempotency correlation;
- lease/retry/backoff/dead-letter or review state;
- stuck-message metrics/alerts;
- recovery runbook.

### T-011 Offline queue tampering

Attack: local storage changes amount/customer/items/operation IDs or reorders dependencies.

Mitigations:
- treat client payload as untrusted on sync;
- server recomputes/validates authoritative rules;
- operation ID + payload hash conflict detection;
- scoped/expiring offline grant;
- dependency validation;
- audit anomalies.

### T-012 CFC misuse

Attack/error: ordinary offline queue is presented as formal contingency, or normal CFE number is assigned to CFC.

Mitigations:
- separate contingency aggregate/permissions;
- registered CFC stock/range/custody;
- explicit enter/exit contingency workflow;
- no normal CAE allocation for CFC;
- reconciliation/daily-report evidence.

### T-013 Inventory/payment race

Failure: concurrent sale/payment/receipt writes cause lost updates, negative stock or over-allocation.

Mitigations:
- application-managed version token;
- transaction/locking adapter;
- immutable movement/allocation facts;
- configurable stock/overpayment policy;
- 409/conflict path instead of silent overwrite.

### T-014 Audit tampering/data exfiltration

Attack: privileged user modifies/deletes audit records or exports excessive PII.

Mitigations:
- append-oriented restricted audit repository;
- separate `audit.read` permission;
- no ordinary update/delete API;
- export logged/audited;
- minimal payload/PII policy;
- retention/backup controls.

### T-015 Cross-tenant/company data leakage

Even if Release 1 is single-company per deployment, company identity is explicit and queries/commands must scope accordingly so future multi-company operation cannot rely on accidental global filters.

Mitigations:
- company ID in authorization/context and owned aggregates;
- repository/application scope tests;
- unique keys include company where appropriate;
- no client-selected company override without permission.

### T-016 Denial of service / expensive fiscal operations

Attack: repeated XML validation, reports, search or issuance attempts exhaust CPU/DB/provider quota.

Mitigations:
- rate limiting;
- pagination;
- request limits;
- background jobs for expensive work;
- circuit breaker/backoff for external services;
- bounded retries;
- metrics/alerts.

## Security abuse cases that must become tests

- authenticated cashier attempts `fiscal.manage_cae`;
- user from location A reads/modifies location B scoped resource;
- same idempotency key replayed with changed amount;
- two concurrent terminals reserve same last CAE number;
- foreign customer is forced into export CFE without qualifying treatment;
- XML containing DTD/external entity is uploaded;
- provider callback replayed twice;
- revoked user syncs queued privileged operation;
- accepted CFE edit/delete is attempted;
- audit event update/delete endpoint is attempted and does not exist;
- secret-bearing configuration is serialized/logged and redaction test fails the build.

## Residual/open risks

- exact DGI/provider authentication/callback controls depend on chosen integration;
- certificate custody/HSM/key-vault design remains OPEN;
- offline local secure storage depends on future web/mobile client architecture;
- retention/RPO/RTO and backup tamper-resistance values remain OPEN;
- single-company vs multi-company deployment policy requires explicit product decision before broad hosted SaaS use.

These OPEN items block only the implementation slices that require them; they are not permission to weaken the accepted trust boundaries.
