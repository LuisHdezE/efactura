# UI-AUTH-001 — Deferred runtime visual debt

Status: `IMPLEMENTED / RUNTIME_REVIEWED / VISUAL_DEBT_DEFERRED`

Decision date: `2026-09-19`

Technical-debt issue: `#154`

## Decision

The provider-neutral `/acceso` view is implemented and deployed, but the approved scenic waterfront/city composition still does not achieve acceptable runtime parity in the target desktop viewport.

After repeated scoped frontend attempts, Luis explicitly decided on 2026-09-19 to stop further iteration on this defect, record it as technical debt, and continue with the next governed UI lane.

This decision is **not** visual runtime acceptance and must not be represented as `VISUAL_RUNTIME_ACCEPTED` or final `ACCEPTED`.

## Runtime evidence

Runtime was reviewed after deployment of PR `#151` from:

`main@7c046f18a32cf373577d7338c2e26a9aae142144`

Observed in both light and dark modes:

- the `eFactura` branding, provider-neutral entry copy, CTA, access card, theme control and footer render;
- no local credential fields or provider-specific identity behavior were introduced;
- the approved waterfront/city scenic layer remains effectively absent from the visible desktop composition;
- therefore visual parity with `UI-AUTH-001 v1-provider-neutral` remains unresolved.

## Implementation trail

The frontend-only implementation/fix trail includes:

- PR `#143` — initial provider-neutral WebApp implementation;
- PR `#144` — scenic visual parity attempt;
- PR `#145` — physical scenery asset;
- PR `#146` — scenic aspect-ratio correction;
- PR `#149` — full-frame scenery attempt;
- PR `#151` — viewport-anchor correction.

All changes remained within the WebApp presentation lane. No API/backend/auth-contract behavior was authorized by this visual work.

## Deferred debt boundary

Future work for issue `#154` is limited to restoring runtime scenic parity while preserving:

1. the approved `UI-AUTH-001 v1-provider-neutral` authority;
2. provider-neutral authentication semantics;
3. current content/card/footer behavior;
4. light/dark theme support;
5. aspect ratio without geometric stretching;
6. representative desktop/laptop viewport compatibility.

Fresh deployed screenshots in both themes and explicit Luis approval are required before this visual debt can be closed.

## Independent governance debt

The exact original approved PNG remains source-fingerprinted but is not physically preserved byte-identical in Git. That `BINARY_PRESERVATION_PENDING` condition is independent from runtime scenic parity and must not be silently resolved by regeneration or substitution.

## Progression decision

`UI-AUTH-001` no longer blocks continuation of the frontend roadmap.

The next unreconciled web candidate in the governed inventory is `WEB-002 — Operational Dashboard`. It may proceed through the standard reconciliation flow while issue `#154` remains open.