# Acceptance Criteria and Traceability Draft

## Purpose

Provide measurable examples that prevent the target requirements from becoming aspirational prose. Final `requirements_ready` still requires complete review of this matrix.

## Core acceptance criteria

| ID | Requirement(s) | Acceptance criterion |
|---|---|---|
| AC-001 | FR-012..014 | A service-only installation can create/fiscalize a sale with zero inventory configuration and no stock movement. |
| AC-002 | FR-014, FR-060 | A mixed sale with product + service creates stock effect only for the stock-tracked line. |
| AC-003 | FR-024, NFR-003 | Replaying identical sale-confirm command 10 times with same idempotency identity results in one canonical sale/payment/fiscalization effect. |
| AC-004 | FR-032, NFR-004/005 | Concurrent fiscal-number reservation tests on PostgreSQL and MySQL never produce duplicate company+CFE-type+series+number. |
| AC-005 | FR-033 | Expired/exhausted CAE cannot reserve a new normal CFE number and generates actionable status/alert. |
| AC-006 | FR-037/038 | Envelope transport acknowledgement can be stored while contained CFE remains awaiting individual result; API never reports that state as accepted. |
| AC-007 | FR-039 | Attempt to edit/delete an accepted/non-rejected CFE is rejected; authorized correction creates a referenced correction document. |
| AC-008 | FR-040 | Rejected CFE preserves original number/XML/response and transitions only through configured regularization disposition. |
| AC-009 | FR-050/051 | Simulated DGI/provider outage and client/API outage enter distinct operational states/workflows. |
| AC-010 | FR-051/053 | Offline client cannot reserve a normal CFE/CAE number. CFC synchronization preserves CFC identity and does not create an unrelated duplicate normal CFE. |
| AC-011 | FR-052..055 | Interrupted sync batch can resume; already-applied operations return canonical prior result. Altered payload with reused operation ID returns conflict. |
| AC-012 | FR-056 | A queued high-risk offline operation whose permission is no longer valid is rejected or routed to review, never silently applied. |
| AC-013 | FR-060..063 | Every stock-changing command creates a traceable immutable movement and current position reconciles to movement history under test fixtures. |
| AC-014 | FR-065/066 | EOQ simulation uses persisted/configured demand inputs and saving a simulation causes no stock mutation. |
| AC-015 | FR-067 | Receiving an approved PO produces receipt/movement records; direct UI “apply EOQ to stock” is not an API capability. |
| AC-016 | FR-070..073 | Two partial collections update allocations and derived open balance exactly; overpayment follows explicit policy rather than silent min/cap. |
| AC-017 | FR-004, NFR-007 | Fiscal number reservation, CFE response, stock adjustment, payment allocation and permission change are queryable in durable audit with actor/correlation/context. |
| AC-018 | FR-005, NFR-005/012 | Same required persistence/API integration suite passes against configured PostgreSQL and MySQL environments. |
| AC-019 | FR-035 | XML with structural tag strings but invalid official schema fails validation, proving validation is XSD/rule based rather than `string.includes`. |
| AC-020 | FR-036 | API never returns certificate private key/password; signing test uses configured secret/certificate abstraction. |
| AC-021 | FR-042 | Daily fiscal report is generated from authoritative CFE/CFC consumption independently of management sales report endpoint. |
| AC-022 | FR-080/081 | Duplicate received CFE XML import is detected without creating a second canonical fiscal record; original artifact/hash retained. |
| AC-023 | FR-030/031 | Document selector returns rule/version/source evidence for chosen/eligible CFE family and rejects incompatible client-requested code. |
| AC-024 | FR-041 | Specialized CFE family cannot be issued unless issuer/profile and required regulatory configuration are enabled. |
| AC-025 | NFR-010 | Money/tax regression cases prove deterministic decimal/rounding behavior and no binary floating-point persistence. |

## Use-case traceability

| Use case | Main requirements |
|---|---|
| UC-ORG-001 | FR-001, FR-005, NFR-001/011 |
| UC-IAM-001 | FR-002/003/004, NFR-001/015 |
| UC-CAE-001 | FR-031..033, NFR-003/004/007 |
| UC-CASH-001/002 | FR-025..027, NFR-003/007 |
| UC-SALE-001 | FR-012..015, FR-020/021 |
| UC-SALE-002 | FR-020..024, FR-030..038, FR-060, NFR-003/004 |
| UC-SALE-003 | FR-023, FR-070..073 |
| UC-SALE-004/005 | FR-012..014, FR-020..023, FR-060 |
| UC-FISC-001 | FR-037/038/040/084 |
| UC-FISC-002 | FR-039..041 |
| UC-FISC-003 | FR-040 |
| UC-CONT-001 / CFC-002/003 | FR-050..056, FR-042 |
| UC-SYNC-001 | FR-052..056, NFR-003/009 |
| UC-INV-001/002 | FR-060..064 |
| UC-PROC-001/002 | FR-064..068, FR-071 |
| UC-AR-001 | FR-026/027, FR-070/072/073 |
| UC-AP-001 | FR-026/027, FR-071..073 |
| UC-RCV-001 | FR-080/081, FR-071 |
| UC-XML-001 | FR-035/036/080 |
| UC-REP-001/002 | FR-031/042, NFR-003/007/011 |
| UC-AUD-001 | FR-004, NFR-007/015 |
| UC-MON-001 | FR-033/043/084/085, NFR-006 |
| UC-REM-001 | FR-041, FR-060..063 |
| UC-EXP-001/002 | FR-030/031/041 |
| UC-RSG-001 | FR-030/031/041 |
| UC-BE-001 | FR-030/031/041, FR-067/071/081 |
| UC-CTA-001 | FR-030/031/041 |

## Evidence traceability classes

Final requirement evidence must retain source provenance:

- current eFactura AS-IS;
- reference capability/file;
- explicit product-owner requirement;
- official DGI source/version for regulated rules;
- proposed target decision/ADR when architectural.

## Human decisions needed before `requirements_ready`

1. Approve target product scope and Release-1 vs later/conditional capabilities.
2. Select initial fiscal families to fully implement/certify.
3. Decide direct-DGI vs provider strategy, or explicitly approve adapter-first implementation with fake/test gateway until provider decision.
4. Approve inventory negative-stock/backorder policy.
5. Approve customer credit/overpayment policy.
6. Approve initial costing scope (PPP/FIFO).
7. Approve certificate custody model when deployment environment is known.

Until then, this document remains `DRAFT`; no Blueprint requirements gate is marked PASS.
