# UI-RCV-001 — CFE recibidos / Validación XML: reconciliación

Status: `GOVERNED_DRAFT / VISUAL_BASELINE_PENDING / ROUTE_DISABLED`

Mapping: `WEB-016 -> UI-RCV-001`

Candidate route: `/cfe-recibidos`; navigation remains `PLANNED_DISABLED`.

## Product authority

`WEB-016 — Received CFE and XML Validation` belongs to the Fiscal navigation group. Its accepted scope is to import received fiscal XML individually or in bounded batches, present schema/signature/rule findings, detect duplicates, and preserve original artifact, hash and source. Requirements: `FR-080`, `FR-081`; traceability: `UC-RCV-001`, `AC-022`. Candidate roles: accountant, purchaser and auditor. Role names are product scope, not proof of an authenticated WebApp permission.

## Contract mapping

| Operation | Purpose | Permission |
| --- | --- | --- |
| `API-RCV-001` `listReceivedFiscalDocuments` | Query received documents and status | `received_fiscal.read` |
| `API-RCV-002` `importReceivedFiscalDocument` | Single artifact import and duplicate detection | `received_fiscal.import` |
| `API-RCV-003` `importReceivedFiscalDocumentsBatch` | Bounded batch with per-file results | `received_fiscal.import` |
| `API-RCV-004` `getReceivedFiscalDocument` | Document metadata, validation and linkage | `received_fiscal.read` |
| `API-RCV-005` `downloadReceivedFiscalArtifact` | Authorized original artifact download | `received_fiscal.read` |
| `API-RCV-006` `listReceivedFiscalValidationFindings` | Structured findings | `received_fiscal.read` |
| `API-XML-001` `validateFiscalXml` | Validate bounded XML without canonical import | `received_fiscal.validate` |

Source: accepted operation inventory and WEB-016 interface reconciliation. All seven operations are currently `MISSING_HTTP` in `documentation/api-completion-matrix/WAVE_5.md`. This WebApp lane does not implement them or change their contracts.

## Semantics to preserve

- Validation without import cannot be presented as an imported canonical received document.
- Single import outcomes are `IMPORTED`, `DUPLICATE`, `INVALID`, `REVIEW_REQUIRED`; the UI needs textual equivalents and distinct explanations.
- Duplicate detection must not create a second canonical fiscal record; original artifact/hash/source remain preserved according to `AC-022`.
- Batch results are per file; a failed file cannot silently erase successful results without an accepted all-or-nothing mode.
- Schema/spec version, signature status and rule findings are server evidence, never a client-side fiscal verdict.
- Download is an authorized read of the original artifact, not a local fixture pretending to be a fiscal file.

## Frontend boundary and next gates

There is no `UI-RCV-001` React route, capability binding, HTTP adapter or approved visual baseline yet. The Sidebar entry stays visible and disabled. A future visual preview may use clearly labelled deterministic local fixtures, local search/filter/detail interaction and disabled placements for upload, validation, import, batch and download. It must keep `operations: []`, avoid direct HTTP and never imply that a file was validated, imported or downloaded by the server.

Before activating a preview: approve an exact responsive visual baseline; implement the page and QA guard; verify desktop light/dark and 390×844 mobile; then review CI and merge under the normal PR gate. Real upload/import/validation require the separate API lane, authoritative WebApp session/permissions, file size and type bounds, idempotency and HTTP error handling.
