# API Completion Master Matrix — Wave Count Ledger

Status: `RECONCILED`

This companion ledger prevents the public-v1 denominator and wave ownership from drifting while implementation proceeds.

Current public-v1 inventory denominator: **194 operations**.

Current implemented HTTP operations: **41**.

Current missing HTTP operations: **153**.

## Reconciled wave totals

| Wave | Scope | Contract operations | Implemented | Missing | Raw operation coverage |
|---:|---|---:|---:|---:|---:|
| 1 | Identity + Organization + Reference Data | 30 | 7 | 23 | 23.33% |
| 2 | Parties + Catalog + Sales Completion | 26 | 22 | 4 | 84.62% |
| 3 | Payments + Cash + AR/AP | 24 | 0 | 24 | 0.00% |
| 4 | Inventory + Transfers + Procurement + Receiving | 23 | 4 | 19 | 17.39% |
| 5 | Fiscal Completion + CAE + CFE Lifecycle | 40 | 8 | 32 | 20.00% |
| 6 | Reporting + Audit + Sync | 21 | 0 | 21 | 0.00% |
| 7 | Technical Operations Console | 30 | 0 | 30 | 0.00% |
| **Total** |  | **194** | **41** | **153** | **21.13%** |

The totals reconcile exactly:

`30 + 26 + 24 + 23 + 40 + 21 + 30 = 194`

and:

`7 + 22 + 0 + 4 + 8 + 0 + 0 = 41`

## Family-to-wave allocation

| Wave | API families | Count | Current implemented families/operations |
|---:|---|---:|---|
| 1 | `IAM-001..011`, `ORG-001..010`, `REF-001..008`, `CAT-009` | 30 | `ORG-001..006`, `CAT-009` |
| 2 | `PTY-001..008`, `CAT-001..008`, `POS-001`, `SAL-001..009` | 26 | `PTY-001..007`, `CAT-001..008`, `SAL-001..007` |
| 3 | `PMT-001..003`, `AR-001..004`, `COL-001..003`, `AP-001..004`, `PAY-001..003`, `CSH-001..007` | 24 | none |
| 4 | `INV-001..004`, `TRF-001..007`, `RPL-001..002`, `PRC-001..006`, `GRC-001..004` | 23 | `INV-001..004` |
| 5 | `FIS-001..010`, `FDL-001..002`, `CAE-001..007`, `CNT-001..007`, `RCV-001..006`, `XML-001`, `DFR-001..004`, `CAL-001`, `CFG-001..002` | 40 | `FIS-010`, `CAE-001..007` |
| 6 | `DEV-001..003`, `SYN-001..004`, `REP-001..006`, `AUD-001..004`, `DAS-001`, `AEX-001..003` | 21 | none |
| 7 | `SYS-001..002`, `ALT-001..002`, `MON-001`, `INT-001..003`, `API-172..API-193` | 30 | none |

## Allocation rationale

Family assignment follows dominant operational ownership, not inventory file location:

- `CAT-009` belongs to Wave 1 because tax profiles are reference/master metadata already consumed by catalog and sales.
- payment-method administration remains in Wave 3 because it is part of the payments capability boundary.
- fiscal configuration, daily fiscal reports and fiscal calendar stay in Wave 5 because they govern fiscal operation/readiness rather than generic analytics.
- technical alerts, integration monitoring, health/version and the complete `API-172..API-193` Technical Operations Console remain in Wave 7.
- received fiscal artifacts and XML validation remain in Wave 5 because they are part of the fiscal-document lifecycle rather than generic reporting.

No API family is double-counted. No operation is omitted from the 194-operation inventory.

## Next implementation gate

Wave 1 may begin only after PR approval/merge of this baseline and after its exact-head validation requirements are satisfied. Implementation then proceeds by the bounded W1.1..W1.6 sequence defined in `API_COMPLETION_MASTER_MATRIX.md`.
