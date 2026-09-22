# UI-FIS-001 — Fiscal Documents Reconciliation

Status: `VISUAL_BASELINE_APPROVED / IMPLEMENTATION_NOT_STARTED`

Mapping:

```text
WEB-013 -> UI-FIS-001
```

Candidate route: `/documentos-fiscales`

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

The future React implementation may use local fixtures to demonstrate:

- document search/filtering;
- local selection of a document;
- visual accepted/in-process/rejected statuses clearly labeled as demonstration data;
- document identity and snapshot-oriented detail;
- tabs for events, representation/XML and references/delivery context.

The preview must not present local fixtures as live DGI evidence, canonical fiscal state or immutable server artifacts.

## Execution boundary while APIs remain missing

A future `ACTIVE_VISUAL_PREVIEW` is eligible only if all of the following remain true:

- all displayed business rows/counts/states are explicitly local demonstration data;
- no `fetch`/HTTP registration targets `API-FIS-001..009` or `API-FDL-001..002` until executable evidence exists;
- XML/representation actions are disabled or explicitly non-executable demonstration affordances;
- correction and regularization mutations are disabled;
- delivery requests are disabled;
- no bulk export behavior is invented;
- no local state mutation is presented as authoritative fiscal lifecycle change;
- `operations: []` remains mandatory in `capabilities.ts` for the preview.

## Required states

The governed interface baseline requires support for:

- `default`;
- `loading`;
- `success`;
- `error`;
- `403`;
- `409`;
- `422`.

A local visual preview must not fake live backend failures or authorization outcomes as if they came from production. Those states remain implementation requirements for later API integration.

## Neighboring module boundaries

`WEB-013` does not absorb:

- `WEB-014 — CAE Administration`;
- `WEB-015 — Contingency and Synchronization Supervision`;
- `WEB-016 — Received CFE and XML Validation`;
- `WEB-017 — Reports and Fiscal Calendar`.

Links or affordances toward those areas must remain non-navigable until the destination has its own governed route.

## Next gate

1. Preserve the approved baseline.
2. Keep `/documentos-fiscales` `PLANNED_DISABLED` while no responsive React preview exists.
3. Reconcile preview-route policy.
4. Implement the responsive local-demo preview in a dedicated branch.
5. Keep every server-owned fiscal action disabled.
6. Run repository gates.
7. Merge only after explicit human approval and review deployed runtime separately.