# UI-AUTH-001 — Login and Session Entry Reconciliation

Status: `RECONCILED / SPECIFICATION_READY_WITH_INTEGRATION_GAPS`

Upstream interface scope: `WEB-001` — Login and Session Entry

Reconciled against: `main@bfbc0810fc69d28b1fa46e9830e93be7ee307dcd`

## Decision

`WEB-001` is promoted to the governed UI boundary:

```text
WEB-001 -> UI-AUTH-001
```

The governed view is the web session-entry boundary. It may present the authentication handoff and, once an authenticated bearer context exists, resolve safe application/session context.

It must **not** turn the business API into a credential issuer.

## Requirements trace

Primary accepted target requirements:

- `FR-002` — authenticate users and enforce permission/policy authorization for every protected operation;
- `FR-003` — support users associated with allowed organization/location/terminal contexts;
- `NFR-001` — secrets/credentials remain outside committed configuration and privileged operations follow least privilege;
- `NFR-015` — return only the minimum required personal/fiscal data by permission and redact secrets/sensitive configuration.

Related target use case:

- `UC-IAM-001 — Authenticate and establish session`.

## API-contract resolution

The accepted API contract deliberately separates credential acquisition from the business API.

Authoritative contract statement:

- token acquisition, refresh and logout are identity-provider/deployment lifecycle concerns;
- the business API does not invent `/login` or `/refresh`;
- `API-IAM-001 getCurrentActor` is contracted as `GET /api/v1/me`, authenticated;
- `getCurrentActor` returns safe actor/application context: identity/display metadata, effective permissions and allowed company/location/terminal scopes.

Therefore the UI must not invent API-owned username/password issuance, refresh-token storage, password reset or logout endpoints.

## Current executable evidence

### WebApp

The current WebApp does **not** implement a session-entry route or authentication gateway.

Observed current state:

- `src/WebApp/src/app/App.tsx` exposes only `/pos` and `/clientes`;
- the default route redirects directly to `/pos`;
- no route guard is present;
- `src/WebApp/src/services/contracts.ts` contains catalog, parties and sales gateways only;
- `src/WebApp/src/services/index.ts` uses mock gateways and explicitly leaves API mode unavailable;
- `AppShell.tsx` renders hardcoded demo identity/context labels: `Admin`, `Demo Uruguay`, `Caja demo`, `Mock local`.

Those labels are demo presentation data, not authenticated session evidence.

### API runtime

`API-IAM-001 getCurrentActor` exists in the governed API contract, but no executable `/api/v1/me` controller/route is currently evidenced in the WebApi controller inventory at this baseline.

Disposition:

| Capability | Contract | Executable evidence | UI disposition |
| --- | --- | --- | --- |
| External authentication handoff | deployment/IdP-owned | identity provider not selected in this lane | `INTEGRATION_PENDING` |
| `getCurrentActor` | `API-IAM-001 GET /api/v1/me` | not evidenced in current WebApi controllers | `CONTRACTED_NOT_EXECUTABLE` |
| API-owned login | explicitly not contracted | none | `MUST_NOT_INVENT` |
| API-owned refresh | explicitly not contracted | none | `MUST_NOT_INVENT` |
| API-owned logout | explicitly not contracted | none | `MUST_NOT_INVENT` |

## Governed UI boundary

`UI-AUTH-001` may govern:

1. unauthenticated session-entry presentation;
2. a generic external authentication handoff action;
3. authentication-in-progress state;
4. authentication return/failure presentation;
5. authenticated context-resolution state;
6. permission/scope-safe session summary after context is available;
7. access-denied/session-expired/too-many-requests states;
8. responsive and accessible behavior.

It does not govern identity-provider administration or credentials.

## Visual/interaction constraint before IdP selection

Until an identity provider and browser-flow integration are explicitly selected, the UI must remain provider-neutral.

The first visual draft should therefore **not** contain:

- local email/password fields presented as authoritative login;
- password-reset links that imply an owned credential store;
- social/provider logos that have not been selected;
- refresh-token controls;
- invented company/location selectors that are not backed by resolved actor context.

A provider-neutral primary action such as `Continuar al acceso seguro` may be represented in a visual mockup, provided it is documented as a handoff placeholder rather than an implemented credential flow.

## Session/context principle

The view must distinguish:

- authentication: possession of a valid externally acquired identity/token;
- application context: effective permissions and allowed organization/location/terminal scopes resolved authoritatively;
- UI preference: non-authoritative local preferences such as light/dark theme.

The current hardcoded `Admin / Demo Uruguay / Caja demo` shell must not be reclassified as authoritative session context.

## States

Minimum governed states:

- `entry`;
- `redirecting_to_identity_provider`;
- `authentication_return`;
- `resolving_actor_context`;
- `ready`;
- `unauthorized_401`;
- `forbidden_403`;
- `rate_limited_429`;
- `session_expired`;
- `backend_error`;
- `network_unavailable`.

## Responsive and accessibility baseline

The upstream scope requires authentication to work on desktop, tablet and mobile web.

Minimum accessibility requirements:

- keyboard-operable primary action;
- visible focus;
- programmatic status and validation/error messages;
- no color-only authentication status;
- logical heading structure;
- focus restoration after error/return flows.

## Reconciliation outcome

`UI-AUTH-001` is sufficiently bounded for a governed visual/functional specification, but it is **not ready for real authentication implementation**.

The next frontend lane may create and approve a provider-neutral visual baseline and may implement only an explicitly labeled mock/demo session-entry experience.

Real authentication integration remains blocked until:

1. an identity-provider/deployment browser flow is selected;
2. token acquisition/storage/return handling is governed for the WebApp;
3. `getCurrentActor` becomes executable or an equivalent accepted session-context contract is implemented.

No API/backend change is authorized by this reconciliation.
