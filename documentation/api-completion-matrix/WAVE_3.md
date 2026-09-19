# API Completion Master Matrix — Wave 3

Status: `FULL_OPERATION_LEVEL_RECONCILED`

Parent index: `documentation/API_COMPLETION_MASTER_MATRIX.md`.

Scope: **Payments + Cash + AR/AP**.

Baseline: **24 operation IDs**, **0 implemented**, **24 non-implemented**.

| API ID | operationId | Method / path | Permission | Contract | Implementation | Current WebApi evidence | Deep readiness | Wave | Gap / blocker |
|---|---|---|---|---|---|---|---|---:|---|
| `API-PMT-001` | `listPaymentMethods` | GET `/api/v1/payment-methods` | `payments.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Payment-method administration surface required |
| `API-PMT-002` | `createPaymentMethod` | POST `/api/v1/payment-methods` | `payments.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Payment-method administration surface required |
| `API-PMT-003` | `updatePaymentMethod` | PATCH `/api/v1/payment-methods/{paymentMethodId}` | `payments.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Payment-method administration surface required |
| `API-AR-001` | `listReceivables` | GET `/api/v1/receivables` | `receivables.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Receivables read model/API required |
| `API-AR-002` | `getReceivable` | GET `/api/v1/receivables/{receivableId}` | `receivables.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Receivables read model/API required |
| `API-AR-003` | `getReceivablesAging` | GET `/api/v1/receivables/aging` | `receivables.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Authoritative receivables aging projection required |
| `API-AR-004` | `createReceivableAdjustment` | POST `/api/v1/receivables/{receivableId}/adjustments` | `receivables.adjust` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Receivable adjustment command required |
| `API-COL-001` | `createCollection` | POST `/api/v1/collections` | `receivables.collect` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Collection lifecycle required |
| `API-COL-002` | `getCollection` | GET `/api/v1/collections/{collectionId}` | `receivables.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Collection read model required |
| `API-COL-003` | `reverseCollection` | POST `/api/v1/collections/{collectionId}/reverse` | `receivables.collect` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Compensating collection reversal required |
| `API-AP-001` | `listPayables` | GET `/api/v1/payables` | `payables.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Payables read model/API required |
| `API-AP-002` | `getPayable` | GET `/api/v1/payables/{payableId}` | `payables.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Payables read model/API required |
| `API-AP-003` | `getPayablesAging` | GET `/api/v1/payables/aging` | `payables.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Authoritative payables aging projection required |
| `API-AP-004` | `createPayableAdjustment` | POST `/api/v1/payables/{payableId}/adjustments` | `payables.adjust` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Payable adjustment command required |
| `API-PAY-001` | `createSupplierPayment` | POST `/api/v1/supplier-payments` | `payables.pay` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Supplier-payment lifecycle required |
| `API-PAY-002` | `getSupplierPayment` | GET `/api/v1/supplier-payments/{paymentId}` | `payables.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Supplier-payment read model required |
| `API-PAY-003` | `reverseSupplierPayment` | POST `/api/v1/supplier-payments/{paymentId}/reverse` | `payables.pay` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Compensating supplier-payment reversal required |
| `API-CSH-001` | `getCurrentCashShift` | GET `/api/v1/cash-shifts/current` | `cash.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Cash-shift lifecycle required |
| `API-CSH-002` | `openCashShift` | POST `/api/v1/cash-shifts` | `cash.open` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Cash-shift lifecycle required |
| `API-CSH-003` | `getCashShift` | GET `/api/v1/cash-shifts/{cashShiftId}` | `cash.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Cash-shift lifecycle required |
| `API-CSH-004` | `listCashMovements` | GET `/api/v1/cash-shifts/{cashShiftId}/movements` | `cash.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Cash-movement ledger projection required |
| `API-CSH-005` | `createCashMovement` | POST `/api/v1/cash-shifts/{cashShiftId}/movements` | `cash.move` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Manual cash movement command required |
| `API-CSH-006` | `closeCashShift` | POST `/api/v1/cash-shifts/{cashShiftId}/close` | `cash.close` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Cash close/reconciliation calculation required |
| `API-CSH-007` | `reconcileCashShift` | POST `/api/v1/cash-shifts/{cashShiftId}/reconcile` | `cash.reconcile` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 3 | Variance reconciliation command required |
