# UI-POS-001 — D1 deployed visual-functional review

Status: `REVIEW_IN_PROGRESS / SECOND_REFINEMENT_PREPARED`

Review target:

```text
https://efactura.eliasworks.uy/pos
```

Current deployed baseline:

```text
c98b5a06bf6e8a23127ccf065a9ae95280d77c3b
```

Deployment evidence:

- workflow: `Deploy eFactura Demo`;
- run: `35037362950`;
- result: `success`;
- deployed boundary: `src/WebApp/dist/` only;
- deployed change: merge PR #115, D1 increment 01.

## Sources of truth

- `documentation/ui/specifications/UI-POS-001_POS.md`;
- `documentation/ui/inventory/UI-POS-001_RECONCILIATION.md`;
- current Web API contracts/controllers on `main`;
- deployed WebApp source at the baseline SHA above;
- saved visual-reference history in open PR #111, including `UI-POS-001 v2-mobile` as a candidate only.

The v2 mobile reference remains `NOT_VISUALLY_APPROVED`. This review does not infer visual approval.

## Runtime-review limitation

The governed deployment pipeline proves that the bundle was built and uploaded successfully. During D1.2 the assistant tool environment could not resolve `efactura.eliasworks.uy`, so no claim of direct pixel-level inspection of the deployed page is made from that environment.

The review therefore distinguishes:

- deployment evidence: verified;
- source/contract review of the exact deployed SHA: verified;
- final running desktop/tablet/mobile visual capture: still required before final D1 closure.

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

### Visual/demo maturity after increment 01 — IMPROVED / REVIEW CONTINUES

Increment 01 added:

- presentation-only product imagery with fallback;
- better loading and empty states;
- product visual continuity in sale lines;
- mobile sticky sale summary;
- clearer operator hierarchy;
- explicit local mock-draft feedback.

The product imagery remains presentation metadata outside `CommercialItemDto` and creates no media-management capability.

## D1.2 source-to-contract gap

A concrete functional gap remains after increment 01:

- the WebApp service boundary exposes only `CatalogGateway` and `PartiesGateway`;
- the backend exposes `createSale`, `updateSaleDraft`, `validateSale` and `getSaleFiscalPreview`;
- therefore the deployed POS still stops before the governed sale lifecycle that `UI-POS-001` is intended to demonstrate.

This is a source-backed gap, not a visual preference.

## D1 increment 02

Branch:

```text
feat/ui-pos-001-d1-sales-lifecycle
```

Scope:

- add TypeScript DTOs shaped from the current `SalesContracts.cs` surface;
- add a dedicated `SalesGateway`;
- preserve fail-closed behavior when `VITE_DATA_MODE=api`;
- add an in-memory mock sales adapter with versioned draft state;
- create/update a mock draft using the same commercial inputs expected by the API;
- expose `CONSUMER_FINAL`, `TAXPAYER_INVOICE` and `EXPORT` commercial intent explicitly;
- represent `validateSale` as an async mock transition;
- render an API-shaped fiscal preview;
- keep preview tax amount/total and CFE selection deliberately unresolved instead of calculating them in the browser;
- invalidate derived validation/preview state when the operator changes the draft;
- show sale version and dirty/saved/validated states;
- retain product images strictly as presentation-only metadata.

## D1.2 authority boundaries

The mock lifecycle is deliberately labeled and remains browser-memory only.

It does **not** imply:

- a real API call;
- backend persistence;
- authoritative tax calculation;
- authoritative CFE-family selection;
- DGI acceptance;
- payment-method discovery;
- durable confirmation;
- cancellation;
- post-confirm fiscalization status;
- fiscal representation download;
- authoritative offline operation.

`locationId` and `terminalId` remain `null` in the mock and the UI states that the operational bootstrap is not integrated. No fictitious `getPosBootstrap` result is invented.

## Validation semantics verified against Application

The current `ValidateSaleUseCase` obtains the fiscal preview first and returns `Valid = false` immediately when `preview.ReadyForConfirmation` is false. In that path it does not mark the sale validated and does not advance its version.

D1.2 mirrors that behavior deliberately:

- the mock preview leaves tax treatment/rate and CFE selection in `REQUIRES_REVIEW`;
- preview tax and total remain unresolved;
- the API-shaped readiness field remains false;
- `validateSale` returns `valid:false` and the current draft unchanged;
- the UI renders this as validation with findings, not as a successful server validation.

This prevents the demo from manufacturing a fiscal readiness state that the current authoritative backend would not grant under equivalent unresolved conditions.

## Confirmation boundary

`API-SAL-007 confirmSale` exists in the backend, but this increment intentionally does not enable confirmation because the current POS lane still lacks a governed source for settlement/payment-method data.

The UI explains that boundary instead of fabricating a payment method or treating an empty local gesture as a durable confirmed sale.

## CI recovery note

The first D1.2 guard attempts were cancelled during an interrupted/restarted CI sequence and by the workflow concurrency policy. They are not treated as code failures. Final readiness requires a fresh successful `Frontend Demo CI` and `Clean Architecture Guard` on the exact current PR head after the concurrency group is clear.

## Remaining review items

Before final `PASS`, `PASS_WITH_ACCEPTED_DEVIATIONS` or `FAIL`:

1. build/typecheck increment 02 in CI;
2. deploy it only after explicit merge approval;
3. capture/review running desktop, tablet and narrow-mobile states;
4. review customer-search ergonomics separately from the lifecycle work;
5. reconcile the still-unapproved visual candidate in PR #111;
6. record the required governed User Story gap before final `ACCEPTED` status.

## Review status

D1 remains open. Increment 02 is prepared to close the most important functional-demo gap without expanding the business capability beyond the current API.
