# API Completion Master Matrix — Wave Count Ledger

Status: `FULLY_RECONCILED / W1_4_USERS_CLOSED / W1_5_IMPLEMENTED_PRE_MERGE`

Implementation reconciliation PR: **#195**.

This companion ledger prevents the public-v1 denominator, implementation count and wave ownership from drifting while the completion program proceeds.

Current public-v1 inventory denominator: **194 operation IDs**.

Current implemented HTTP operations: **64**.

Current non-implemented operation IDs: **130**.

Current distinct HTTP method/path signatures: **193** because one accepted-contract collision uses the same signature for two API IDs.

## Reconciled wave totals

| Wave | Scope | Contract operations | Implemented | Missing HTTP | Contract-collision IDs | Non-implemented | Raw operation coverage |
|---:|---|---:|---:|---:|---:|---:|---:|
| 1 | Identity + Organization + Reference Data | 30 | 30 | 0 | 0 | 0 | 100.00% |
| 2 | Parties + Catalog + Sales Completion | 26 | 22 | 4 | 0 | 4 | 84.62% |
| 3 | Payments + Cash + AR/AP | 24 | 0 | 24 | 0 | 24 | 0.00% |
| 4 | Inventory + Transfers + Procurement + Receiving | 23 | 4 | 19 | 0 | 19 | 17.39% |
| 5 | Fiscal Completion + CAE + CFE Lifecycle | 40 | 8 | 32 | 0 | 32 | 20.00% |
| 6 | Reporting + Audit + Sync | 21 | 0 | 21 | 0 | 21 | 0.00% |
| 7 | Technical Operations Console | 30 | 0 | 28 | 2 | 30 | 0.00% |
| **Total** |  | **194** | **64** | **128** | **2** | **130** | **32.99%** |

The totals reconcile exactly:

`30 + 26 + 24 + 23 + 40 + 21 + 30 = 194`

`30 + 22 + 0 + 4 + 8 + 0 + 0 = 64`

`0 + 4 + 24 + 19 + 32 + 21 + 30 = 130`

`128 MISSING_HTTP + 2 CONTRACT_COLLISION = 130 non-implemented IDs`

## Family-to-wave allocation

| Wave | API families | Count | Current implemented families/operations |
|---:|---|---:|---|
| 1 | `IAM-001..011`, `ORG-001..010`, `REF-001..008`, `CAT-009` | 30 | `IAM-001..011`, `ORG-001..010`, `REF-001..008`, `CAT-009` |
| 2 | `PTY-001..008`, `CAT-001..008`, `POS-001`, `SAL-001..009` | 26 | `PTY-001..007`, `CAT-001..008`, `SAL-001..007` |
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

W1.1 Reference Data is complete. W1.2 implements `API-IAM-001 getCurrentActor` plus `API-IAM-011 listPermissions`. W1.3 implements and closes `API-IAM-006..009` roles read/write. W1.4 implements and closes `API-IAM-002..005` plus `API-IAM-010` users and role assignment after governed merge, deployment, explicitly approved Neon production migration, HTTP runtime acceptance and independent durable-evidence verification.

W1.5 now implements `API-ORG-007..010` terminals in PR #195, including the accepted `API-ORG-006` active-terminal dependency protection. The four public HTTP surfaces exist in the implementation branch and provider-real PostgreSQL/MySQL QA has passed on the implementation code. The remaining W1.5 gates are final exact-head CI, governed merge, separately approved production schema promotion, Cloud Run deployment and HTTP runtime acceptance. These counts describe HTTP implementation presence and do not imply that W1.5 is formally closed or production-accepted.
