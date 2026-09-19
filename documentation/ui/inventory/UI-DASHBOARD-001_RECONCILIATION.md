# UI-DASHBOARD-001 — Operational Dashboard Reconciliation

Status: `RECONCILED / SPECIFICATION_READY_WITH_API_GAPS`

Upstream interface scope: `WEB-002` — Operational Dashboard

Reconciled against: `main@bd51dd53c7d44644fd0e5bcdee4f8de7cb89eea4`

## Decision

`WEB-002` is promoted to the governed UI boundary:

```text
WEB-002 -> UI-DASHBOARD-001
```

The governed view is the shell-hosted operational landing page for permission-aware summaries and alerts. It may be visually designed and implemented with explicit demo/mock data before the aggregate dashboard endpoints exist, but it must not present unavailable API data as live or authoritative.

The target route is:

```text
/dashboard
```

Once visually approved and implemented, it must be registered in the reusable `shellRoutes` registry so desktop Sidebar, mobile navigation and React Router expose the same route.

## Requirements trace

Primary requirements evidenced by the target baseline:

- `FR-033` — manage CAE validity/range/consumption with alerts;
- `FR-064` — provide stock-minimum/replenishment alerts;
- `FR-074` — provide projected cash-flow data from receivables/payables and approved planned events;
- `FR-082` — expose structured report data for sales, tax, inventory, financial and fiscal views;
- `FR-083` — fiscal-calendar data includes authoritative source/provenance and update lifecycle.

The upstream interface purpose is broader than a single KPI surface: summarize sales, fiscal, cash, stock and obligation alerts from authoritative server data.

## Use-case evidence and traceability gap

`UC-MON-001 — Monitor CAE/certificate/fiscal integration health` explicitly requires scheduled checks, durable severity/deduplicated alerts, acknowledgement, future dashboard/notification API data and protection of certificate/private-key secrets.

No dedicated governed `UC-DAS-*` dashboard lifecycle and no governed `US-*` dashboard user-story artifact are currently evidenced. This reconciliation does not invent them. The gap must be recorded until the repository establishes that traceability.

## Contracted dependencies

The accepted interface reconciliation assigns these authoritative operations to `WEB-002`:

| API ID | operationId | Contracted route | Permission | Current implementation | UI disposition |
| --- | --- | --- | --- | --- | --- |
| `API-DAS-001` | `getDashboardSummary` | `GET /api/v1/dashboard` | `dashboard.read` | `MISSING_HTTP` | `MOCK_ONLY_PENDING_API` |
| `API-ALT-001` | `listAlerts` | `GET /api/v1/alerts` | `alerts.read` | `MISSING_HTTP` | `MOCK_ONLY_PENDING_API` |
| `API-MON-001` | `getIntegrationStatus` | `GET /api/v1/operations/integrations` | `operations.read` | `CONTRACT_COLLISION` | `DO_NOT_BIND` |

The current API completion matrix places `API-DAS-001` in Wave 6 with no executable WebApi surface and `API-ALT-001`/`API-MON-001` in Wave 7.

`API-MON-001` currently collides with `API-180 listOperationalIntegrations` on the same `GET /api/v1/operations/integrations` path with a different operationId/permission. The WebApp must not silently choose one contract or implement a fake integration-status gateway while that accepted-contract collision remains unresolved.

## Current WebApp state

The reusable application shell is governed by `documentation/ui/WEBAPP_SHELL_POLICY.md`.

Current executable shell routes are POS and Customers. `/dashboard` does not yet exist. `defaultShellRoute` remains `/pos` until the Dashboard completes visual approval, implementation, deployment and explicit product approval.

No current dashboard gateway exists in `src/WebApp`.

## Functional boundary for the first visual/implementation increment

The first increment may present a coherent demo dashboard using fixture data shaped around the accepted business concepts below:

1. commercial/sales operational summary;
2. fiscal/CAE status summary;
3. stock/replenishment alerts;
4. cash-flow/financial-obligation summary;
5. actionable alerts feed;
6. fiscal-calendar/obligation preview where represented as demo data;
7. safe integration/operational-status placeholder only if clearly marked unavailable/pending, not simulated as authoritative live health.

The exact server response DTO for `getDashboardSummary` is not currently evidenced. Therefore visual drafting must avoid claiming undocumented field names as public API contract.

## Important design constraints

### 1. Demo data must be visibly non-authoritative

Until the contracted endpoints are executable, values shown by the deployed demo are presentation fixtures. The UI may demonstrate hierarchy, density, states and interactions, but must not label them as live server metrics.

### 2. Permission-aware composition

The target dashboard is permission-aware. The first implementation must be designed so cards/sections can later be hidden or omitted based on effective permissions. Frontend visibility is not an authorization boundary; the backend remains authoritative.

### 3. Alerts are actionable domain evidence, not decoration

Alerts may represent CAE exhaustion/expiry, stock replenishment, fiscal/integration conditions and other governed operational conditions. Severity must not rely on color alone.

The first WebApp increment may navigate to already implemented routes where meaningful, but must not fabricate destination pages that do not exist yet.

### 4. No secret operational material

Certificate secrets, private keys, secret integration values, credentials and internal diagnostics are never dashboard content.

### 5. Shell reuse is mandatory

`Topbar`, `Sidebar`, `BottomBar` and `MobileNavigation` remain reusable platform components. `UI-DASHBOARD-001` owns only its feature content area.

### 6. Navigation completeness

When implemented, `/dashboard` must be added through the single governed `shellRoutes` registry. A Dashboard page that exists by direct URL but is missing from Sidebar/mobile navigation is incomplete.

### 7. Default landing-page switch is separate

Implementation of `/dashboard` does not automatically authorize changing the default shell route. Promoting `/dashboard` from a Sidebar destination to the default landing page requires explicit product approval after runtime review.

## Visual-reference audit

Before this reconciliation, repository evidence was audited in the required order:

1. `documentation/ui/references/approved/` — no Dashboard reference exists;
2. `documentation/ui/references/drafts/` — no Dashboard draft exists;
3. historical repository search/PR evidence — no previously approved Operational Dashboard mock was found.

Therefore a new visual draft is permitted after this reconciliation/specification is accepted. It must use the current reusable shell rather than redesigning global chrome.

## Reconciliation outcome

`UI-DASHBOARD-001` is sufficiently bounded for a governed functional specification and visual drafting.

Its first implementation is intentionally `API_MOCK_DATA_ONLY` for the dashboard-specific data plane because the authoritative aggregate endpoints are not executable yet. That does not block visual/product progression, but it does block any claim of live dashboard integration.
