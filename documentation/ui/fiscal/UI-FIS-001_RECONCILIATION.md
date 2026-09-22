# UI-FIS-001 — Fiscal Documents Reconciliation

Status: `ACTIVE_VISUAL_PREVIEW / API_PENDING / RUNTIME_REVIEW_PENDING`

Mapping:

```text
WEB-013 -> UI-FIS-001
```

Route: `/documentos-fiscales`

## Upstream product authority

`WEB-013 — Fiscal Documents` belongs to the Fiscal product area and is defined to search/view the CFE lifecycle, artifacts, DGI result, references and authorized correction/regularization actions.

Accepted source requirements:

- `FR-030`, `FR-031`;
- `FR-034`, `FR-035`, `FR-036`, `FR-037`, `FR-038`, `FR-039`, `FR-040`, `FR-041`;
- `FR-044`, `FR-045`, `FR-046`, `FR-047`, `FR-048`, `FR-049`.

Accepted roles:

- cashier;
- seller;
- accountant;
- fiscal administrator;
- auditor.

Source data authority is server-owned: fiscal document snapshot, transport/result state and artifacts are authoritative API data when exposed.

## Accepted WEB-013 API dependency

`documentation/blueprint-api-contract/09_INTERFACE_SCOPE_RECONCILIATION.md` maps `WEB-013` to:

- `API-FIS-001` `listFiscalDocuments`;
- `API-FIS-002` `getFiscalDocument`;
- `API-FIS-003` `downloadFiscalXml`;
- `API-FIS-004` `downloadFiscalRepresentation`;
- `API-FIS-005` `listFiscalDocumentEvents`;
- `API-FIS-006` `createFiscalCorrection`;
- `API-FIS-007` `listRegularizationCases`;
- `API-FIS-008` `getRegularizationCase`;
- `API-FIS-009` `resolveRegularizationCase`;
- `API-FDL-001` `listFiscalDocumentDeliveries`;
- `API-FDL-002` `requestFiscalDocumentDelivery`.

Current Wave 5 evidence marks all eleven of those operations `MISSING_HTTP` and `NOT_YET_AUDITED` for executable WebApi coverage.

`API-FIS-010` (`collectFiscalEnvelopeDocumentResponseEvidence`) is separately implemented in Wave 5, but it is not a substitute for the `WEB-013` list/detail/artifact/event/correction/regularization/delivery surface above.

## Visual authority

Approved baseline: `UI-FIS-001 / v1-responsive-composite`.

Approved artifact gen_id: `f7f7b77c-d6c9-4366-9c1d-7767990c4746`.

The artifact contains desktop and mobile responsive compositions within one approved image.

## Visual-to-domain reconciliation

The approved artifact governs composition, density, hierarchy, responsive behavior and visual language. It does not override source-backed fiscal semantics.

The React preview uses local fixtures to demonstrate:

- document search/filtering;
- local selection of a document;
- visual accepted/in-process/rejected states clearly labeled as demonstration data;
- document identity and snapshot-oriented detail;
- local event timeline presentation;
- responsive document cards and artifact placeholders.

The preview does not present local fixtures as live DGI evidence, canonical fiscal state or immutable server artifacts.

## Active preview boundary

The route is permitted as `ACTIVE_VISUAL_PREVIEW` under `PREVIEW_ROUTE_POLICY_AMENDMENT.md` because:

- the approved baseline is preserved;
- a responsive React page exists in the shared shell;
- every displayed business value is explicitly local demonstration data;
- XML/representation, correction, regularization and delivery commands remain disabled;
- bulk export remains disabled;
- `capabilities.ts` registers `operations: []`;
- `verify-fiscal-documents-preview.mjs` guards against accidental FIS/FDL HTTP integration.

The preview must not:

- claim canonical fiscal state from DGI or transport services;
- synthesize authoritative XML or printable fiscal representations;
- create corrections;
- create or resolve regularization cases;
- claim real delivery attempts or send delivery requests;
- mutate local fixtures as authoritative fiscal history;
- expose a bulk-export operation not present in the accepted WEB-013 contract.

## Required states

The governed interface baseline requires support for:

- `default`;
- `loading`;
- `success`;
- `error`;
- `403`;
- `409`;
- `422`.

The local visual preview demonstrates deterministic presentation states only. It does not fake live backend failures or authorization outcomes as if they came from production. Those states remain requirements for later API integration.

## Neighboring module boundaries

`WEB-013` does not absorb:

- `WEB-014 — CAE Administration`;
- `WEB-015 — Contingency and Synchronization Supervision`;
- `WEB-016 — Received CFE and XML Validation`;
- `WEB-017 — Reports and Fiscal Calendar`.

No dead navigation into those planned modules is introduced by this preview.

## Runtime gate

Repository CI must pass before merge. After deployment, desktop/mobile runtime review remains a separate acceptance checkpoint. Live API integration requires fresh executable evidence and a separate reconciliation.
