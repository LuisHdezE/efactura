# Security, Authorization and Durable Audit Architecture

## Applicability

Threat modeling is **APPLICABLE** because the system processes fiscal documents, payments, customer/supplier data, credentials, signing material, offline operations and privileged administrative actions.

## Authentication

Target HTTP authentication remains ASP.NET Core JWT Bearer compatible.

Rules:
- signing keys/client secrets/passwords/private certificates are never committed;
- secret material comes from environment/User Secrets for development or approved production secret/key store;
- symmetric JWT may remain only if approved and managed with rotation; asymmetric/provider-issued tokens are supported behind configuration;
- Auth0/package presence does not make Auth0 mandatory;
- token issuer/audience/lifetime validation is explicit;
- HTTPS is required for production transport.

## Authorization

Authorization is server-authoritative and permission/policy based.

A role is a convenient permission composition, not the enforcement primitive.

Example permission namespace:

- `sales.read`
- `sales.create`
- `sales.confirm`
- `sales.correct`
- `fiscal.read`
- `fiscal.issue`
- `fiscal.correct`
- `fiscal.manage_cae`
- `fiscal.manage_contingency`
- `inventory.read`
- `inventory.adjust`
- `inventory.transfer`
- `procurement.manage`
- `receivables.collect`
- `payables.pay`
- `cash.open`
- `cash.close`
- `audit.read`
- `security.manage_roles`

Permissions may also carry company/location/terminal scope.

Default rule: all business endpoints require authentication and an explicit permission/policy. Only intentionally public/operational endpoints such as selected health/version endpoints are anonymous.

## Offline authorization

Offline client capability is narrower than normal online authorization:

- cached/offline grants are signed or tamper-evident application state;
- grant has expiration and scoped allowed operations;
- no offline privilege escalation;
- high-risk queued operations are re-authorized on synchronization;
- server may reject/route to review if role/permission/configuration changed while offline.

## Secret/certificate custody

`IFiscalSigner` isolates signing from the application. Private key material never enters API response, audit payload or ordinary log.

Supported deployment strategies may include:
- protected certificate store;
- cloud/remote key vault/HSM adapter;
- provider-managed signing when contractually appropriate.

Final custody strategy is an explicit ADR before production fiscal implementation.

## Durable audit

Audit is separate from Serilog/Application Insights.

Required fields where applicable:

- audit event ID;
- occurred/recorded timestamps;
- actor type/user ID;
- company/location/device/terminal;
- event category/action;
- aggregate/entity type and ID;
- request/correlation/idempotency/client-operation IDs;
- reason/authorization context;
- before/after or immutable operation snapshot/reference;
- source channel/API/worker/sync;
- success/failure outcome where meaningful.

Critical audited categories:

- login/security events when available to the app;
- role/permission/scope changes;
- company/fiscal configuration changes;
- CAE import/allocation/status changes;
- fiscal issuance/transmission/acceptance/rejection/correction;
- contingency entry/document/recovery;
- payment/collection/allocation/reversal;
- receivable/payable adjustment;
- stock adjustment/transfer discrepancy;
- cash shift open/close/variance override;
- certificate/signing configuration changes;
- external integration administrative changes;
- idempotency conflicts/replay anomalies.

## Logging/redaction

Technical logs contain request/correlation identifiers and operational diagnostics but must redact:

- passwords;
- JWT/signing secrets;
- private keys/certificate passwords;
- full connection strings;
- unnecessary full fiscal/customer payloads;
- sensitive external tokens.

Exception responses never expose raw provider/database exception messages in production.

## API security controls

Architecture requires during API contract/implementation:

- RFC 9457 Problem Details;
- request body/size limits where relevant;
- rate limiting for authentication/high-risk/public endpoints;
- explicit CORS allow-list by environment;
- validation of uploaded XML/file size/content;
- anti-replay/idempotency for high-impact commands;
- authorization tests for 401/403 and organization-scope isolation.

## Threat baseline

| Threat | Architectural mitigation |
|---|---|
| duplicate fiscal/payment effect after timeout | idempotency + durable canonical result |
| fiscal number race | atomic allocator + DB uniqueness |
| unauthorized branch data/action | scoped policy authorization |
| stolen offline state | expiring scoped offline grant + revalidation |
| provider callback spoof/replay | authenticated adapter + inbox/dedupe/signature validation |
| XML malicious payload | bounded parser, DTD/external entity disabled, schema validation |
| secret leakage | external secret store + redaction + rotation |
| audit tampering | append-oriented restricted audit store and access control |
| broken object authorization | permission + company/location ownership checks in use case |
| business-rule bypass via client CFE/state | server selectors/state machines |
