# API Completion Master Matrix — Wave Count Ledger

Status: `FULLY_RECONCILED / WAVE_1_CLOSED / W2.1_CLOSED`

Implementation reconciliation PR: **#208**.

W2.1 operational closure: `documentation/api-completion-matrix/W2_1_SALE_FISCALIZATION_STATUS_RUNTIME_CLOSURE.md`.

This companion ledger prevents the public-v1 denominator, implementation count and wave ownership from drifting while the completion program proceeds.

Current public-v1 inventory denominator: **194 operation IDs**.

Current implemented HTTP operations: **65**.

Current non-implemented operation IDs: **129**.

Current distinct HTTP method/path signatures: **193** because one accepted-contract collision uses the same signature for two API IDs.

## Reconciled wave totals

| Wave | Scope | Contract operations | Implemented | Missing HTTP | Contract-collision IDs | Non-implemented | Raw operation coverage |
|---:|---|---:|---:|---:|---:|---:|---:|
| 1 | Identity + Organization + Reference Data | 30 | 30 | 0 | 0 | 0 | 100.00% |
| 2 | Parties + Catalog + Sales Completion | 26 | 23 | 3 | 0 | 3 | 88.46% |
| 3 | Payments + Cash + AR/AP | 24 | 0 | 24 | 0 | 24 | 0.00% |
| 4 | Inventory + Transfers + Procurement + Receiving | 23 | 4 | 19 | 0 | 19 | 17.39% |
| 5 | Fiscal Completion + CAE + CFE Lifecycle | 40 | 8 | 32 | 0 | 32 | 20.00% |
| 6 | Reporting + Audit + Sync | 21 | 0 | 21 | 0 | 21 | 0.00% |
| 7 | Technical Operations Console | 30 | 0 | 28 | 2 | 30 | 0.00% |
| **Total** |  | **194** | **65** | **127** | **2** | **129** | **33.51%** |

The totals reconcile exactly:

`30 + 26 + 24 + 23 + 40 + 21 + 30 = 194`

`30 + 23 + 0 + 4 + 8 + 0 + 0 = 65`

`0 + 3 + 24 + 19 + 32 + 21 + 30 = 129`

`127 MISSING_HTTP + 2 CONTRACT_COLLISION = 129 non-implemented IDs`

## Family-to-wave allocation

| Wave | API families | Count | Current implemented families/operations |
|---:|---|---:|---|
| 1 | `IAM-001..011`, `ORG-001..010`, `REF-001..008`, `CAT-009` | 30 | `IAM-001..011`, `ORG-001..010`, `REF-001..008`, `CAT-009` |
| 2 | `PTY-001..008`, `CAT-001..008`, `POS-001`, `SAL-001..009` | 26 | `PTY-001..007`, `CAT-001..008`, `SAL-001..007`, `SAL-009` |
| 3 | `PMT-001..003`, `AR-001..004`, `COL-001..003`, `AP-001..004`, `PAY-001..003`, `CSH-001..007` | 24 | none |
| 4 | `INV-001..004`, `TRF-001..007`, `RPL-001..002`, `PRC-001..006`, `GRC-001..004` | 23 | `INV-001..004` |
| 5 | `FIS-001..010`, `FDL-001..002`, `CAE-001..007`, `CNT-001..007`, `RCV-001..006`, `XML-001`, `DFR-001..004`, `CAL-001`, `CFG-001..002` | 40 | `FIS-010`, `CAE-001..007` |
| 6 | `DEV-001..003`, `SYN-001..004`, `REP-001..006`, `AUD-001..004`, `DAS-001`, `AEX-001..003` | 21 | none |
| 7 | `SYS-001..002`, `ALT-001..002`, `MON-001`, `INT-001..003`, `API-172..API-193` | 30 | none |

Family assignment follows dominant operational ownership, not inventory-file location.

## Known contract collision

`API-MON-001 getIntegrationStatus` and `API-180 listOperationalIntegrations` both define:

`GET /api/v1/operations/integrations`

but with different permissions (`operations.read` versus `operations.integrations.read`) and different operationIds. They remain two accepted operation IDs, so the denominator stays **194**, but the inventory currently has only **193 distinct HTTP signatures**.

Both rows are represented in `documentation/api-completion-matrix/WAVE_7.md` as `CONTRACT_COLLISION / BLOCKED_BY_CONTRACT`. No implementation may silently choose one interpretation.

## Operation-level sources

The logical matrix consists of:

- `documentation/api-completion-matrix/WAVE_1.md`
- `documentation/api-completion-matrix/WAVE_2.md`
- `documentation/api-completion-matrix/WAVE_3.md`
- `documentation/api-completion-matrix/WAVE_4.md`
- `documentation/api-completion-matrix/WAVE_5.md`
- `documentation/api-completion-matrix/WAVE_6.md`
- `documentation/api-completion-matrix/WAVE_7.md`

Architecture tests require those seven shards to contain the exact 194 inventory IDs exactly once and to preserve the source contract operationId, HTTP method, route and permission.

## Next implementation gate

Wave 1 is formally closed at `30 / 30` after the W1.5 Terminals merge, deployment, explicitly approved production migration, production runtime acceptance and independent Neon verification.

Wave 2 opened with a governed readiness audit. W2.1 owner-locked the field-level contract for `API-SAL-009 getSaleFiscalizationStatus` in PR #207 and implemented the exact bounded read-only surface in PR #208 using existing Sale, FiscalizationRequest and FiscalDocument persistence.

W2.1 is now closed after pre-merge Guard #695, owner-approved PR #208 merge, post-merge Guard #696, Deploy API Demo #55, promotion of Cloud Run revision `efactura-api-d22-9989c0f-55-1`, and read-only production runtime acceptance run `35643849728`. Production had zero Sale rows, so no artificial fixture was created; successful 200 state projections remain covered by accepted automated QA.

The three remaining Wave 2 gaps stay unchanged: `API-SAL-008 cancelSale`, `API-POS-001 getPosBootstrap`, and `API-PTY-008 getPartyAccountSummary` each retain their explicit prerequisites from `W2_READINESS_AUDIT.md`.

The next governed frontier is W2.2 `API-SAL-008 cancelSale`, beginning with lifecycle/irreversible-boundary contract closure before any implementation.
