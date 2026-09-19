# API Completion Master Matrix — Wave 6

Status: `FULL_OPERATION_LEVEL_RECONCILED`

Parent index: `documentation/API_COMPLETION_MASTER_MATRIX.md`.

Scope: **Reporting + Audit + Sync**.

Baseline: **21 operation IDs**, **0 implemented**, **21 non-implemented**.

| API ID | operationId | Method / path | Permission | Contract | Implementation | Current WebApi evidence | Deep readiness | Wave | Gap / blocker |
|---|---|---|---|---|---|---|---|---:|---|
| `API-DEV-001` | `registerDevice` | POST `/api/v1/devices` | `sync.device.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Device/offline registration boundary required |
| `API-DEV-002` | `getDevice` | GET `/api/v1/devices/{deviceId}` | `sync.use` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Device status projection required |
| `API-DEV-003` | `revokeDevice` | POST `/api/v1/devices/{deviceId}/revoke` | `sync.device.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Device/offline revocation boundary required |
| `API-SYN-001` | `createSyncBatch` | POST `/api/v1/sync/batches` | `sync.use` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Ordered offline batch submission required |
| `API-SYN-002` | `getSyncBatch` | GET `/api/v1/sync/batches/{batchId}` | `sync.use` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Sync batch progress/results projection required |
| `API-SYN-003` | `getSyncOperation` | GET `/api/v1/sync/operations/{clientOperationId}` | `sync.use` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Canonical replay-result projection required |
| `API-SYN-004` | `getSyncChanges` | GET `/api/v1/sync/changes` | `sync.use` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Scoped cursor-based delta feed required |
| `API-REP-001` | `getSalesReport` | GET `/api/v1/reports/sales` | `reports.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Sales reporting projection required |
| `API-REP-002` | `getTaxReport` | GET `/api/v1/reports/tax` | `reports.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Tax reporting projection required |
| `API-REP-003` | `getInventoryReport` | GET `/api/v1/reports/inventory` | `reports.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Inventory reporting projection required |
| `API-REP-004` | `getReceivablesReport` | GET `/api/v1/reports/receivables-aging` | `reports.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Receivables report projection required |
| `API-REP-005` | `getPayablesReport` | GET `/api/v1/reports/payables-aging` | `reports.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Payables report projection required |
| `API-REP-006` | `getCashFlowReport` | GET `/api/v1/reports/cash-flow` | `reports.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Cash-flow reporting projection required |
| `API-AUD-001` | `listAuditEvents` | GET `/api/v1/audit-events` | `audit.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Durable audit query surface required |
| `API-AUD-002` | `getAuditEvent` | GET `/api/v1/audit-events/{auditEventId}` | `audit.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Durable audit detail surface required |
| `API-AUD-003` | `createAuditExport` | POST `/api/v1/audit-exports` | `audit.export` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Bounded auditable export job required |
| `API-AUD-004` | `getAuditExport` | GET `/api/v1/audit-exports/{exportId}` | `audit.export` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Audit export status/download metadata required |
| `API-DAS-001` | `getDashboardSummary` | GET `/api/v1/dashboard` | `dashboard.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Permission-aware dashboard composition required |
| `API-AEX-001` | `listAccountingExportFormats` | GET `/api/v1/accounting-exports/formats` | `accounting.export` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Accounting-format catalog required |
| `API-AEX-002` | `createAccountingExport` | POST `/api/v1/accounting-exports` | `accounting.export` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Bounded accounting export job required |
| `API-AEX-003` | `getAccountingExport` | GET `/api/v1/accounting-exports/{exportId}` | `accounting.export` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 6 | Accounting export status/download metadata required |
