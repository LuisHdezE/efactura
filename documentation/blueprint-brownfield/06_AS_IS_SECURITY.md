# AS-IS Security

## AuthN

`OBSERVED`: JWT bearer authentication is registered in `Program.cs` with issuer, audience, lifetime and symmetric signing-key validation. `UseAuthentication()` and `UseAuthorization()` are present.

`OBSERVED`: `RequireHttpsMetadata = false` is configured. This relaxes HTTPS enforcement for authority metadata in JwtBearer options; it does **not by itself prove** that every bearer token is transported over HTTP. `UseHttpsRedirection()` is also present.

Auth0 and Google-auth packages/references exist, but an active Auth0/Google login flow was not proven in the inspected composition/controllers.

## AuthZ

`CRITICAL OBSERVATION`: no `[Authorize]`, `[AllowAnonymous]`, role/policy attribute or configured fallback/default policy enforcing authenticated access was observed on the 69 controller actions reviewed.

Therefore:

- authentication infrastructure exists;
- authorization middleware exists;
- **protected endpoint enforcement is not observed**;
- Swagger's Bearer security declaration is documentation metadata, not enforcement.

This is a P1 authorization gap for a future financial/fiscal system.

## Versioned secrets — P0

Sensitive values are versioned in application settings. Confirmed categories include:

- PostgreSQL credentials/connection data: `<REDACTED>`
- JWT signing key: `<REDACTED>`
- Azure Blob account key: `<REDACTED>`

Because the repository is public, removal from the current file alone is insufficient. Future containment/remediation must include `ROTATE + REMOVE + HISTORY/EXPOSURE ASSESSMENT` for any value that may still be valid.

No secret value is reproduced in Brownfield evidence.

## CORS

`OBSERVED`: CORS allows any origin, method and header, and `SetIsOriginAllowed` returns true.

Risk description: this broadly exposes the API to browser cross-origin callers. Exact exploitability depends on future authentication/cookie/token handling and endpoint protections; calling it simply “CSRF” would be imprecise.

## HTTPS / transport

`OBSERVED`: `UseHttpsRedirection()` is enabled.

`UNKNOWN`: actual reverse proxy/TLS production termination, HSTS, certificate policy and network exposure are not demonstrated by repository code.

## Error disclosure

The global exception filter:

- logs exception message, stack trace, source and inner exception at fatal level;
- returns exception message/source in generic `ResultObject` error responses;
- includes stack trace in Development;
- has a NotFound handler that sets logical ErrorCode 404 but HTTP status 400.

This needs future error-contract/redaction hardening.

## SQL/data-access review

Dapper repositories inspected use parameterized values in normal CRUD operations. This is a positive observation and no direct user-value SQL concatenation was confirmed in those samples.

However:

- raw SQL is present;
- generic query builders/dynamic identifiers exist;
- complete repository coverage was not performed as a penetration test.

Status: `PARTIAL / further static and integration security QA required`.

## Security capabilities not observed

- durable business/security audit store;
- permission matrix/RBAC enforcement;
- rate limiting;
- idempotency/replay protection;
- refresh-token rotation/session revocation workflow;
- security headers/HSTS policy;
- dedicated secrets-vault abstraction;
- fiscal certificate/private-key custody;
- request/device actor context suitable for financial audit.

These are absence-of-observed-implementation statements, not claims that the surrounding deployment has none of them.
