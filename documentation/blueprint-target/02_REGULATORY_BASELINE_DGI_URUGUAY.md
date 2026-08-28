# Regulatory Baseline — DGI Uruguay CFE

## Scope and status

This is the initial regulatory baseline for requirements design. It is not tax/legal advice and it does not replace final homologation, provider documentation or current DGI technical specifications. Every fiscal rule implemented later must be traceable to an official source/version.

Baseline date: **2026-08-28**.

## Authoritative sources reviewed

- DGI e-Factura portal: https://www.efactura.dgi.gub.uy/
- `Formato de los CFE v25.2`, dated 2026-04-28 and enabled in Production from 2026-06-30.
- Current DGI functional definitions for CFE, including numbering, CAE, daily report, correction and contingency.
- Resolution 798/2012 as updated by subsequent resolutions, including August 2025 updates.
- DGI CAE format/instructions and CFE FAQs/instructions.

Reference code and documents from `FacturacionElectronicaBases` are **non-authoritative**.

## Validated design constraints

### REG-001 — Versioned fiscal specification

DGI changes CFE format/XSD/message specifications over time. Production is currently on Format CFE **25.2**. Fiscal rules, schemas and response interpretation therefore cannot be hard-coded as an eternal enum/serializer.

Target requirement:
- persist/use `specification_version`, `effective_from`, `effective_to`, source reference and schema artifact identity;
- preserve the fiscal representation that was valid when a document was emitted;
- support parallel historical validation of older documents.

### REG-002 — CFE type catalog is broader than the demo

The reference enum contains 101/102/103, 111/112/113, 121/122/123, 181 and 182. Official DGI formats also cover additional families/variants such as account-on-behalf and other document types. The target must use a versioned fiscal document catalog and enable only the types applicable to a customer profile.

### REG-003 — CAE numbering is company-wide by CFE type

Official DGI functional definitions state that CFE numbering is unique **by CFE type for the entire company**. CAE is issued for the main fiscal domicile; a separate numbering sequence is not created merely because there are multiple branches.

Consequences:
- do not model independent CAE numbering per branch as the default;
- fiscal number reservation must be globally concurrency-safe for company + CFE type + authorized range/series;
- the branch/terminal that originates the operation is still recorded as business/audit context.

### REG-004 — CAE lifetime and exhaustion

Official material establishes a two-year validity for authorized CFE numbering ranges. Unused numbers at expiry must be annulled/reported and cannot be reused. The downloaded CAE is signed by DGI and contains authorization/range/type/expiry data.

Target controls:
- import/validate CAE metadata and signature where technically applicable;
- active/expired/exhausted state;
- low-range and expiry alerts;
- atomic next-number reservation;
- audit all CAE changes/consumption;
- reconcile unused/annulled numbering with daily reporting.

### REG-005 — Correction after a non-rejected CFE

DGI functional definitions state that once a CFE was issued, sent to DGI and not rejected, correction is through a correction note (credit or debit note), not destructive editing/deletion.

Target rule:
- accepted/non-rejected fiscal documents are immutable business evidence;
- corrections create referenced fiscal documents;
- the original remains queryable.

### REG-006 — Rejected CFE handling is not a simple delete/retry

DGI defines explicit treatment for rejected documents and exceptions such as specific rejection codes. Numbers already sent to DGI generally cannot simply be recycled.

Target requirement:
- maintain rejection code/message and response artifact;
- use a versioned rejection-resolution policy;
- separate `REJECTED`, `ANNULMENT_REQUIRED`, `REGULARIZATION_REQUIRED`, `CORRECTION_REQUIRED` and resolved states as needed;
- never implement “delete CFE and reuse number”.

### REG-007 — Envelope and asynchronous acknowledgement

Official documentation describes envelopes containing 1 to 250 CFE/CFC and synchronous receipt of the envelope followed by asynchronous individual validation/acknowledgement in the web-service flow.

Target consequence:
- transport lifecycle and fiscal-document lifecycle are separate;
- store envelope/transaction/token/status and each document acknowledgement;
- retries are idempotent and auditable;
- a transport ACK is not equivalent to final CFE acceptance.

### REG-008 — Daily report is a first-class fiscal process

DGI requires a consolidated daily report of CFE/CFC usage and documentation consumption. Official functional definitions describe automatic generation and submission from the emitter's effective date, including days with no operations, and submission in the defined time window.

Target consequence:
- daily report is a scheduled aggregate process, not an on-demand dashboard query;
- report state, generation, signature, submission, acknowledgement and corrections/reliquidations are persisted;
- amounts/FX conversion follow the report specification, not the POS display calculation.

### REG-009 — CFC contingency is distinct from normal offline queuing

Official DGI material requires software to contain a `Contingencia` module. CFC numbering is the numbering of the authorized/preprinted contingency document, and the system must **not** assign normal CFE authorized numbering to the CFC. After recovery, CFC information is sent following the rules of the CFE type it substitutes. The CFC remains the fiscally valid document for that operation.

This invalidates the simplistic idea “store offline sale and later replace it with a new normal CFE”.

Target consequence:
- separate `OfflineClientOperation` from `FiscalContingencyDocument`;
- local web/mobile clients do not reserve normal CAE/CFE numbers by default;
- if a sale must be completed while the electronic issuance system is unavailable, the fiscal workflow uses the applicable CFC contingency procedure;
- on synchronization, server records/linkage preserve the CFC identity and transmit its information as required.

### REG-010 — Client offline and DGI/provider outage are different failures

Three operational modes must exist:
1. normal online: client -> API -> fiscal engine -> transport;
2. API available but DGI/provider transport unavailable: fiscal operation can enter a controlled transport/outbox state according to current document rules;
3. client/API issuance system unavailable: offline business queue plus formal CFC contingency when the fiscal operation must be completed.

A generic `offline=true` flag is insufficient.

### REG-011 — Fiscal XML validation must use official schemas/rules

The reference demo's string checks for tags/namespaces are demonstration logic, not XSD validation. Target validation must support the official schema version, business validations, digital signature verification, CAE/range validation and arithmetic rules applicable to the document type/version.

### REG-012 — Preservation and retrieval

DGI requires electronic CFE to be stored and retained for the applicable documentation-retention period. Some publication/reprint rules specify minimum online availability periods for certain printed representations. Retention must be configurable from a documented legal/fiscal policy rather than hard-coded from the demo.

## Reference implementation findings

| Reference behavior | Classification | Target treatment |
|---|---|---|
| `estadoDgi = Aceptado` immediately when demo CFE is created | SIMULATED/INVALID_AS_AUTHORITY | Separate generated/submitted/envelope-accepted/document-accepted states. |
| `/enviar-dgi` always changes status to accepted | SIMULATED | Replace with real gateway + response interpreter. |
| `validar-xml` uses `string.includes(...)` | SIMULATED | Real XSD + XMLDSig + fiscal-rule validation. |
| CAE number increment in mutable memory | DEMO_ONLY | Transactional durable number reservation. |
| Branch-independent CAE suggested in one reference architecture section | CONFLICT_WITH_DGI | Company-wide CFE-type numbering baseline. |
| Offline queue later “re-emits definitive CFE” | PARTIAL/CONFLICTING | Model CFC contingency separately and preserve fiscal identity. |
| Simplified daily report always `ACEPTADO` | SIMULATED | Scheduled signed report + actual transport/ack lifecycle. |
| Demo CFE type enum | PARTIAL | Versioned complete catalog. |

## Regulatory decisions still requiring deeper validation before code

The following are intentionally `OPEN` until the corresponding implementation slice:

- exact document-selection matrix for each taxpayer/customer scenario;
- current thresholds/conditional fields by CFE type/version;
- e-Resguardo applicability and retention/perception rules;
- export/remito/boleta-entry flows when enabled;
- exact provider-vs-direct-DGI transport contract;
- certificate/key custody model for deployment;
- current response-code and regularization matrix;
- final daily-report/reliquidation rules and FX source implementation;
- final printed representation/QR requirements for every enabled CFE type.

No fiscal-sensitive endpoint may be marked production-ready while its applicable rule remains `OPEN`.
