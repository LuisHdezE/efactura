# Governed UI Inventory

Status: `ACTIVE / RECONCILIATION_REQUIRED`

This area converts upstream interface scope candidates into governed UI views.

## Upstream source

The current upstream scope lives in:

- `documentation/blueprint-interface/01_INTERFACE_SCOPE_BASELINE.md`
- `documentation/blueprint-interface/interface-scope-baseline.json`

Those artifacts are scope evidence, not executable frontend inventory. Their `WEB-*` and Android items must be reconciled against the current API/application implementation before a stable `UI-*` view is committed.

## Reconciliation states

Each candidate should be classified as one of:

- `CANDIDATE`: upstream scope exists but has not been reconciled against current backend capability;
- `SPECIFICATION_READY`: authoritative requirements/use cases/contracts are sufficient to write the governed view specification;
- `SPECIFIED`: functional specification exists but no visual baseline is approved;
- `VISUAL_DRAFT`: one or more reference drafts exist;
- `VISUAL_APPROVED`: Luis explicitly approved one exact visual version;
- `IMPLEMENTED`: frontend implementation exists;
- `REVIEWED`: implementation was compared against the approved visual baseline and runtime-reviewed;
- `VISUAL_DEBT_DEFERRED`: runtime review found a known visual mismatch that Luis explicitly chose to defer without accepting it;
- `ACCEPTED`: visual/functional review is accepted and all governance prerequisites are satisfied.

These are UI-governance states and do not replace Blueprint project maturity fields.

## Identifier rule

The upstream `WEB-*` identifier is retained as traceability evidence. A governed view receives a stable `UI-*` identifier only when its boundary is clear enough to specify without fabricating backend behavior.

Example mapping format:

```text
WEB-003 -> UI-POS-001
```

The mapping must be recorded in the view specification and inventory entry. The upstream identifier is never silently discarded.

## First reconciliation candidates

| Upstream ID | Candidate | Governed UI ID | Current UI status | Evidence |
| --- | --- | --- | --- | --- |
| `WEB-001` | Login and Session Entry | `UI-AUTH-001` | `IMPLEMENTED / VISUAL_DEBT_DEFERRED` | `UI-AUTH-001_RECONCILIATION.md`, `../specifications/UI-AUTH-001_LOGIN_SESSION.md`, `../references/approved/UI-AUTH-001/v1-provider-neutral/README.md`, `../reviews/UI-AUTH-001/TECHNICAL_DEBT_DEFERRED_2026-09-19.md`, issue `#154` |
| `WEB-002` | Operational Dashboard | `UI-DASHBOARD-001` | `VISUAL_APPROVED / BINARY_PRESERVATION_PENDING` | `UI-DASHBOARD-001_RECONCILIATION.md`, `../specifications/UI-DASHBOARD-001_OPERATIONAL_DASHBOARD.md`, `../references/approved/UI-DASHBOARD-001/v1-theme-pair/README.md` |
| `WEB-003` | POS Sale | `UI-POS-001` | `REVIEWED` | `UI-POS-001_RECONCILIATION.md`, `../specifications/UI-POS-001_POS.md`, `../references/approved/UI-POS-001/v3-theme-pair/`, `../reviews/UI-POS-001/D1_5_RUNTIME_VISUAL_ACCEPTANCE.md` |
| `WEB-004` | Customers and Parties | `UI-CUSTOMER-001` | `REVIEWED` | `UI-CUSTOMER-001_RECONCILIATION.md`, `../specifications/UI-CUSTOMER-001_CUSTOMERS.md`, `../references/approved/UI-CUSTOMER-001/v2-theme-pair/README.md`, `../reviews/UI-CUSTOMER-001/RUNTIME_VISUAL_ACCEPTANCE.md` |

## UI-DASHBOARD-001 reconciliation and visual note

`WEB-002` is reconciled as `UI-DASHBOARD-001` and reserved for `/dashboard` inside the governed reusable WebApp shell.

The accepted interface/API contract maps the view to `API-DAS-001 getDashboardSummary`, `API-ALT-001 listAlerts` and `API-MON-001 getIntegrationStatus` as permitted. None is currently an executable WebApi dependency: `API-DAS-001` is `MISSING_HTTP` in API Wave 6, `API-ALT-001` is `MISSING_HTTP` in API Wave 7, and `API-MON-001` is blocked by the accepted-contract collision with `API-180` on `GET /api/v1/operations/integrations`.

Therefore the first Dashboard implementation may use explicit demo/mock fixtures to validate product hierarchy, responsive behavior and visual interaction, but it must not claim live authoritative dashboard/alert/integration data and must not resolve backend contract collisions in frontend code.

The reusable-shell policy is mandatory: `UI-DASHBOARD-001` may own feature content only. Once implemented it must be registered in `shellRoutes`, which provides React Router, Sidebar and mobile navigation from the same registry. `/pos` remains the default shell route until a separate explicit product decision after Dashboard runtime review.

The required pre-mock audit found no approved/draft/historical Dashboard baseline. A new `UI-DASHBOARD-001 v1-theme-pair` was therefore produced and Luis explicitly approved it on 2026-09-19 with the statement `aprobado`.

The visual authority is registered under `../references/approved/UI-DASHBOARD-001/v1-theme-pair/` with source fingerprint SHA-256 `2b2c90f70a4e66055e7a7881af0932708e9288143cf7a34c6dc361cc06bd2a08`. The exact PNG binary is not yet physically preserved in Git through the current repository channel, so `BINARY_PRESERVATION_PENDING` remains explicit and regeneration/substitution is forbidden.

The approved composition includes future navigation entries and sample KPI/alert/integration values for visual context. Those elements do not authorize executable routes or live API claims. Implementation must expose only routes actually present in `shellRoutes`, reuse the existing shared shell components and keep Dashboard-specific data explicitly demo/mock until backend authority exists.

## UI-AUTH-001 reconciliation note

`WEB-001` is reconciled as `UI-AUTH-001` with a provider-neutral session-entry boundary. The accepted API contract keeps token acquisition/refresh/logout outside the business API and contracts `getCurrentActor` as the safe application-context operation.

Luis explicitly approved `UI-AUTH-001 v1-provider-neutral` on 2026-09-17. The approved composition is a paired light/dark session-entry experience with the provider-neutral CTA `Continuar al acceso seguro`, and it intentionally omits local username/password fields and provider-specific branding.

The approved source PNG is fingerprinted by SHA-256 under `../references/approved/UI-AUTH-001/v1-provider-neutral/`. Direct byte-identical binary insertion remains pending through the current repository channel, so regeneration/substitution is forbidden until that source is physically preserved.

The provider-neutral `/acceso` WebApp view is now implemented. The frontend implementation trail includes PRs `#143`, `#144`, `#145`, `#146`, `#149` and `#151`. Runtime review after PR `#151` on `main@7c046f18a32cf373577d7338c2e26a9aae142144` confirmed that the primary layout renders but the approved scenic waterfront/city layer still does not achieve acceptable visible parity in the target desktop viewport in either theme.

On 2026-09-19 Luis explicitly chose to defer that unresolved scenic mismatch as technical debt and continue the roadmap. The debt is tracked in issue `#154` and `../reviews/UI-AUTH-001/TECHNICAL_DEBT_DEFERRED_2026-09-19.md`.

This is not runtime visual acceptance. `UI-AUTH-001` must not be promoted to `REVIEWED / VISUAL_RUNTIME_ACCEPTED` or final `ACCEPTED` until fresh deployed light/dark runtime evidence is explicitly accepted.

Real authentication integration also remains independently pending because an identity-provider flow and executable application-context integration are not yet governed in this frontend lane.

## UI-POS-001 closure note

`UI-POS-001` has an approved `v3-theme-pair`, a deployed React implementation, and explicit cross-browser runtime acceptance by Luis on 2026-09-17 after successful rendering in Firefox and Chrome Incognito.

The visual/runtime implementation lane is therefore closed as `REVIEWED / VISUAL_RUNTIME_ACCEPTED`.

The inventory intentionally does not promote the row to final `ACCEPTED` yet because `UI-POS-001_POS.md` records an independent governance prerequisite: a governed `US-*` user-story artifact for this flow is still missing. That traceability gap must be resolved in its proper governance lane and must not be fabricated by frontend work.

## UI-CUSTOMER-001 closure note

`UI-CUSTOMER-001 v2-theme-pair` was explicitly approved by Luis on 2026-09-16 and its exact visual authority remains preserved under `documentation/ui/references/approved/UI-CUSTOMER-001/v2-theme-pair/`.

The corresponding React implementation was merged through PR `#135`, deployed from `main@b38e7b2ca57ba2262a85786b12af5d1917b3c1d2`, and runtime-reviewed in both light and dark themes. On 2026-09-17 Luis explicitly approved the deployed result with the statement `Apruebo UI-CUSTOMER-001 runtime visual`.

The visual/runtime implementation lane is therefore closed as `REVIEWED / VISUAL_RUNTIME_ACCEPTED`.

The inventory intentionally does not promote the row to final `ACCEPTED` because `UI-CUSTOMER-001_CUSTOMERS.md` records an independent governance prerequisite: no governed `US-*` customer-master artifact and no dedicated `UC-PTY-*`/`UC-CUSTOMER-*` lifecycle are currently evidenced. That traceability gap remains outside the frontend lane and must not be fabricated here.

Reconciliation and future views must continue to use current repository evidence rather than historical unresolved-API notes alone.
