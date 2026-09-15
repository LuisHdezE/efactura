# UI-POS-001 — D1 deployed visual-functional review

Status: `REVIEW_IN_PROGRESS / FIRST_REFINEMENT_PREPARED`

Review target:

```text
https://efactura.eliasworks.uy/pos
```

Deployed baseline reviewed:

```text
cc98bc8fde17b73bed4972e8ba8e0f70afcb7578
```

Deployment evidence:

- workflow: `Deploy eFactura Demo`;
- run: `35032840184`;
- attempt: `2`;
- result: `success`;
- deployed boundary: `src/WebApp/dist/` only.

## Sources of truth

- `documentation/ui/specifications/UI-POS-001_POS.md`;
- `documentation/ui/inventory/UI-POS-001_RECONCILIATION.md`;
- current Web API contracts/controllers on `main`;
- deployed WebApp source at the baseline SHA above;
- saved visual-reference history in open PR #111, including `UI-POS-001 v2-mobile` as a candidate only.

The v2 mobile reference remains `NOT_VISUALLY_APPROVED`. This review does not infer visual approval.

## Baseline assessment

### Contract integrity — PASS

The deployed baseline preserves the most important current backend boundaries:

- item lookup is based on the current commercial-item projection;
- no authoritative selling price is invented in catalog cards;
- `UnitPrice` is captured on the sale line;
- no stock figure is invented;
- no client-side tax calculation is presented as authoritative;
- no DGI acceptance is claimed;
- no cancellation, fiscal representation download, payment-method discovery or post-confirm fiscalization status is exposed as live capability.

### Visual/demo maturity — REFINEMENT REQUIRED

The baseline is structurally correct but still reads as a technical contract demo rather than a portfolio-ready POS.

Observed gaps from source and responsive composition:

1. product cards have no visual media, despite the approved demo policy allowing presentation-only product imagery;
2. the mobile layout places the sale panel after the entire catalog flow, increasing friction once several products are visible;
3. an empty search result renders no explicit explanatory state;
4. loading is a single text state rather than preserving page structure;
5. line items do not reuse product visual context;
6. the draft action has no visible local feedback after interaction;
7. customer selection is functional but basic and does not yet provide dedicated search behavior;
8. the current header gives governance/technical detail more visual weight than the operator task.

## D1 increment 01

Branch:

```text
feat/ui-pos-001-d1-refinement
```

Scope:

- add presentation-only product visual metadata outside `CommercialItemDto`;
- render product thumbnails in catalog and current-sale lines;
- provide an elegant fallback when visual metadata is absent;
- preserve the rule that catalog cards do not expose selling price;
- add structured loading and explicit no-results states;
- improve operator hierarchy and card density;
- add a mobile sticky sale summary that navigates to the current sale without adding business capability;
- add explicit local feedback when the mock draft is prepared;
- keep tax/fiscal authority out of the frontend;
- avoid a stricter invented rule that requires `UnitPrice > 0`; this increment only checks that quantity is positive and unit price is an explicit finite non-negative number before the local mock-preparation step.

## Presentation-image boundary

The increment deliberately keeps visual metadata separate from the API contract:

```text
CommercialItemDto   -> authoritative API-shaped data
itemVisualMetadata  -> demo-only visual metadata
```

This does **not** create or imply:

- image upload;
- image editing;
- gallery/media management;
- image persistence;
- image API endpoints.

## Explicitly deferred

The following remain outside this first refinement and require a subsequent governed adapter increment:

- persisted `createSale` mock/API flow;
- `validateSale` state machine;
- fiscal preview rendering;
- durable confirmation rendering;
- settlement/payment-method selection;
- post-confirm fiscalization status;
- printing/download;
- authoritative offline mode;
- location/terminal bootstrap behavior while no executable `getPosBootstrap` surface exists.

## Review status

This is not yet a final `PASS`, `PASS_WITH_ACCEPTED_DEVIATIONS` or `FAIL` review.

Final D1 evidence must be recorded after the refinement branch is built, deployed through the governed pipeline, and the running desktop/tablet/mobile result is captured and compared.
