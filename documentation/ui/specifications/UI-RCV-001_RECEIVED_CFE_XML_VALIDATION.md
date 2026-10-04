# UI-RCV-001 — CFE recibidos / Validación XML

Status: `VISUALLY_APPROVED / IMPLEMENTED_VISUAL_PREVIEW / API_PENDING`

Upstream interface: `WEB-016`; candidate route: `/cfe-recibidos`; navigation group: Fiscal.

## 1. Objective and users

Let authorized accountants, purchasers and auditors review received fiscal artifacts, validation findings, duplicate outcomes and their links. Governed requirements: `FR-080`, `FR-081`; traceability: `UC-RCV-001`, `AC-022`. No client-side fiscal result is authoritative.

## 2. Entry and layout proposal

The disabled `CFE recibidos` item already occupies its place in Fiscal navigation. After approval and implementation, the shared AppShell hosts:

1. Breadcrumb, title and an explicit `Vista de demostración · API pendiente` label while in preview mode.
2. A compact summary of local demonstration items, with clear provenance; no invented live totals or DGI status.
3. Search and status filters over deterministic sample rows.
4. A list of received documents with document identity, source, received date, import outcome and validation status.
5. A selected-document detail pane with original artifact metadata, hash availability, signature/schema context and structured findings.
6. Distinct placement for single import, batch import and validate-without-import; all disabled in preview with an explanation.

The list and detail must remain readable without requiring a page-wide horizontal pan. The visual proposal was approved by the user on 2026-09-30; generated copy remains subordinate to this specification.

## 3. Data and authority

| Datum | Authority when integrated | Preview rule |
| --- | --- | --- |
| Identity, source, received date and linkage | `API-RCV-001/004` | Explicit local fixture |
| Original artifact and hash | `API-RCV-004/005` | Metadata may be illustrated; no fake download |
| Schema/spec version and signature status | Server validation projection | Label as demo, never claim verification |
| Structured findings | `API-RCV-006` | Local examples with text and location where supported |
| Import/duplicate outcome | `API-RCV-002/003` | Static examples only; no import side effect |
| XML validation without import | `API-XML-001` | Disabled placement only |

## 4. Actions and boundary

| Action | Permission | HTTP state | Preview behavior |
| --- | --- | --- | --- |
| List, search and inspect | `received_fiscal.read` | `MISSING_HTTP` | Local search/filter/selection |
| Import one XML | `received_fiscal.import` | `MISSING_HTTP` | Disabled |
| Import bounded batch | `received_fiscal.import` | `MISSING_HTTP` | Disabled |
| Validate XML without importing | `received_fiscal.validate` | `MISSING_HTTP` | Disabled |
| Download original artifact | `received_fiscal.read` | `MISSING_HTTP` | Disabled |

No preview file picker should accept a user file or suggest that its contents were validated. The demo shell's `Admin` label is not an authoritative permission check.

## 5. Outcome and state semantics

Textual outcome labels: `IMPORTED` → Importado, `DUPLICATE` → Duplicado, `INVALID` → Inválido, `REVIEW_REQUIRED` → Requiere revisión. Never equate an XML parse, a schema result, a signature check and final fiscal acceptance. A duplicate cannot appear as a newly imported canonical record.

The eventual HTTP integration must handle default, loading, populated, empty, filtered empty, per-file batch results, validation errors, forbidden (`403`), conflict (`409`), unprocessable XML (`422`), rate limit (`429`) and unavailable/offline states. Preview fixtures may show semantic examples but may not fabricate HTTP responses.

## 6. Responsive and accessible behavior

Desktop: list and selected detail side by side where space permits. Tablet: stack summary and detail without losing filter or finding context. Mobile 390×844: single-column cards, readable identity/outcome/finding text and detail below the list; no page-wide horizontal pan. Actions remain explicitly disabled until real authority exists. Label controls, preserve keyboard order and focus, expose finding severity with text and associate field/location text with each finding; never rely on color alone.

## 7. Dependencies and evidence

The view may relate to `UI-SUPPLIER-001`, `UI-PROCUREMENT-001` and `UI-FIS-001` only when accepted cross-view identifiers and server linkage exist. Contract mapping: `API-RCV-001..006`, `API-XML-001`; Wave 5 marks all seven `MISSING_HTTP`. The frontend has a visual-preview route and capability with `operations: []`, but no HTTP adapter.

Visual baseline: desktop proposal approved on 2026-09-30, artifact SHA-256 `fa87ea7b0bc4d1c780b1ad62fa9702803a7b47c4a2c1eacb82851995bfb02794`. Any suggested validation or import success in generated copy is illustrative only. Implementation PR and deployed runtime acceptance remain separate gates; exact mobile 390×844 visual acceptance is pending.
