# Requirements & Domain Baseline

## Status

Draft target requirements derived after Reference Capability Ingestion and DGI validation. This is the start of Blueprint `requirements_domain` evidence. It does **not** mark `requirements_ready` PASS until human review and acceptance criteria/traceability are complete.

## Actors

| Actor | Responsibilities / allowed intent |
|---|---|
| Cashier | POS sales, permitted collections, cash shift operations, receipt/comprobante retrieval. |
| Seller/Operator | sales workflows outside strict cash-terminal mode. |
| Inventory Operator | stock queries, approved movements/transfers/receipts. |
| Purchaser | replenishment proposals, purchase orders and receipts within permissions. |
| Treasury | collections, supplier payments, cash/bank reconciliation. |
| Accountant | fiscal archive, received CFE, XML validation, fiscal reports and applicable configuration review. |
| Fiscal Administrator | CAE, fiscal configuration, contingency supervision, correction/regularization workflows. |
| System Administrator | users/permissions, organization/integration configuration; not automatically entitled to all fiscal actions without permissions. |
| Auditor | read/export durable audit and fiscal evidence under restricted permission. |
| Background Worker | outbox, fiscal transport, acknowledgements, alerts, reports, notifications. |
| External CFE Provider / DGI Gateway | external fiscal integration boundary, never a direct frontend dependency. |
| Future Web Client | API consumer; may support offline queue/sync. |
| Future Mobile Client | API consumer; may support offline queue/sync. |

## Functional requirements

### Foundation / organization / identity

- **FR-001** System shall maintain company fiscal identity and operational locations without rewriting historical transaction snapshots when master data changes.
- **FR-002** System shall authenticate users and enforce permission/policy authorization for every protected operation.
- **FR-003** System shall support users associated with allowed organization/location/terminal contexts.
- **FR-004** System shall record durable security/business audit events separately from technical logs.
- **FR-005** System shall support deployment configuration using PostgreSQL or MySQL with equivalent business behavior.

### Parties and catalog

- **FR-010** System shall manage customers and suppliers with normalized fiscal/document identity and contacts.
- **FR-011** System shall validate Uruguayan identifiers when applicable using tested domain rules and preserve foreign/other identities where allowed.
- **FR-012** System shall manage sellable products and services through a common commercial concept while preserving type-specific behavior.
- **FR-013** Stock tracking shall be optional by item; service-only companies must not require inventory configuration.
- **FR-014** A sale may mix stock-tracked products and non-stock services.
- **FR-015** Catalog tax assignment shall reference versioned tax/fiscal configuration rather than magic percentages embedded in controllers.

### Sales / POS / payments

- **FR-020** System shall support sale draft, validation, confirmation and immutable commercial history.
- **FR-021** System shall calculate line/discount/recargo/tax/total amounts using exact decimal arithmetic and active fiscal rules.
- **FR-022** System shall support cash and credit sale conditions and extensible payment media.
- **FR-023** Credit sale shall create/link a receivable according to approved terms after the proper commercial/fiscal boundary.
- **FR-024** Retry-sensitive sale confirmation shall be idempotent.
- **FR-025** POS mode shall support cash-shift opening, expected totals, counted closing values, reconciliation and variances.
- **FR-026** Payment shall be an independent durable record capable of allocation to one or more obligations according to policy.
- **FR-027** Partial payments/collections shall preserve allocation history and derived balances.

### Fiscal CFE

- **FR-030** System shall determine eligible/required fiscal-document family through a versioned fiscal selection policy based on issuer, receiver and transaction context.
- **FR-031** System shall maintain the official DGI document catalog and effective specification versions with source provenance.
- **FR-032** Normal CFE number reservation shall be atomic, unique and server-authoritative across concurrent terminals.
- **FR-033** CAE shall be managed at company/fiscal-type scope according to current DGI rules, with validity/range/consumption alerts.
- **FR-034** Fiscal documents shall preserve issuer/receiver/item/tax/rule snapshots used at issuance time.
- **FR-035** Fiscal XML shall be generated and validated against the applicable official DGI schema/business rules.
- **FR-036** Fiscal XML shall be signed using the approved certificate/key-custody strategy; private key material shall never be exposed to clients.
- **FR-037** System shall persist fiscal generation, signature, transport, envelope and individual acknowledgement states separately.
- **FR-038** System shall not equate envelope receipt with final CFE acceptance.
- **FR-039** Accepted/non-rejected fiscal documents shall not be destructively edited/deleted; corrections shall use permitted referenced fiscal documents.
- **FR-040** Rejected documents shall preserve number/artifact/response and follow a versioned regularization policy.
- **FR-041** System shall support enabled specialized families such as export, remito, resguardo, account-on-behalf and boleta-entry only after their applicability configuration is accepted.
- **FR-042** System shall generate/submit/store daily fiscal reports independently from management sales reports.
- **FR-043** System shall retain fiscal artifacts and response evidence according to an approved retention policy.

### Contingency / offline

- **FR-050** System shall distinguish client/API offline state from DGI/provider transport outage.
- **FR-051** System shall support formal CFC contingency registration/recovery and shall not assign a normal CFE number to a CFC.
- **FR-052** Future offline clients shall use globally unique client operation IDs and server-side idempotent replay handling.
- **FR-053** Offline clients shall not reserve arbitrary normal CAE/CFE numbers.
- **FR-054** Batch synchronization shall return deterministic per-operation applied/already-applied/rejected/conflict/review/dependency status.
- **FR-055** The same client operation ID with materially different payload shall be rejected/audited as an idempotency conflict.
- **FR-056** Offline authorization shall support narrower/expiring permission scope and server revalidation on synchronization.

### Inventory / procurement

- **FR-060** System shall maintain immutable stock movements and derived/current positions by location for stock-tracked items.
- **FR-061** Sale, purchase receipt, transfer and adjustment shall be explicit movement sources.
- **FR-062** Manual adjustment shall require permission and auditable reason/before-after context.
- **FR-063** System shall support transfer/dispatch/receipt workflow with discrepancy handling.
- **FR-064** System shall provide stock-minimum/replenishment alerts.
- **FR-065** System shall provide EOQ/ROP advisory simulation using real historical/configured inputs, not hardcoded mock demand assumptions.
- **FR-066** EOQ/ROP result shall not directly mutate stock; stock changes through an approved receipt/adjustment use case.
- **FR-067** System shall support purchase order and goods receipt workflows.
- **FR-068** Costing shall support approved PPP/FIFO policies when enabled, with policy/version traceability.

### Receivables / payables / treasury

- **FR-070** System shall manage accounts receivable with due dates, aging, original amounts, allocations and derived balances.
- **FR-071** System shall manage accounts payable similarly and link obligations to supplier/source evidence.
- **FR-072** System shall support partial/full customer collections and supplier payments without silently truncating overpayments.
- **FR-073** Overpayment/advance handling shall follow explicit configured business policy.
- **FR-074** System shall provide projected cash-flow data from receivables/payables and approved planned events.

### Validation / reporting / integrations

- **FR-080** System shall ingest/validate received fiscal XML individually or in batches using schema, signature and rule findings.
- **FR-081** Received CFE shall preserve original artifact/hash/source and duplicate detection.
- **FR-082** System shall expose structured report data for sales, tax, inventory, financial and fiscal views; UI rendering is deferred.
- **FR-083** Fiscal calendar data shall include authoritative source/provenance and update lifecycle; dates shall not be hardcoded indefinitely.
- **FR-084** External CFE provider/direct-DGI integration shall be behind replaceable application ports/adapters.
- **FR-085** Email/other delivery integrations shall keep delivery state/retry evidence separate from fiscal acceptance.
- **FR-086** Accounting exports shall use isolated adapters for each supported external format.

## Non-functional requirements

- **NFR-001 Security:** secrets/credentials/private keys outside committed config; least privilege; protected admin/fiscal operations.
- **NFR-002 Integrity:** fiscal/financial/inventory history uses append/correction semantics where destructive mutation would destroy evidence.
- **NFR-003 Idempotency:** all externally retryable commands that create financial/fiscal/stock effects have deterministic replay behavior.
- **NFR-004 Concurrency:** fiscal numbering, payment allocation, stock mutation and other contested state are transaction/concurrency safe.
- **NFR-005 Portability:** same application behavior and required constraints on PostgreSQL and MySQL.
- **NFR-006 Observability:** request/correlation IDs, structured technical logs, integration metrics, stuck-outbox/transport visibility.
- **NFR-007 Auditability:** durable audit supports actor/time/source/entity/action/reason and before-after or immutable transaction context when applicable.
- **NFR-008 Availability:** ordinary commercial work must degrade safely during external fiscal-provider/DGI issues; legally permitted contingency path remains distinct.
- **NFR-009 Offline-ready contract:** API supports resumable/deduplicated synchronization for future offline clients.
- **NFR-010 Exact amounts:** fiscal/financial amounts use decimal/exact DB types with explicit rounding policies per rule.
- **NFR-011 Versioning:** regulatory schemas/rules/document catalogs and public API contracts have explicit version/effective lifecycle.
- **NFR-012 Testing:** domain/application/API/security/persistence tests plus equivalent PostgreSQL/MySQL contract suites.
- **NFR-013 Performance:** POS read/command paths must have explicit later SLOs; slow external fiscal calls must not produce duplicate effects on timeout/retry.
- **NFR-014 Retention/backup:** fiscal/audit/financial artifacts follow documented retention and recoverability policy.
- **NFR-015 Privacy:** API returns minimum required personal/fiscal data by permission and redacts secrets/sensitive configuration.

## Core business rules

- **BR-001** Reference demo behavior never overrides current official DGI rule.
- **BR-002** Fiscal rule used by an operation is identifiable by version/source/effective date.
- **BR-003** Normal CFE numbering is server-authoritative and company-wide by CFE type under current DGI baseline.
- **BR-004** CFC identity and normal CFE identity are separate numbering/process concepts.
- **BR-005** One idempotency/business operation identity cannot create duplicate money/fiscal/stock effect.
- **BR-006** Accepted historical fiscal document snapshots are immutable.
- **BR-007** Product/service distinction changes inventory behavior, not whether an item can be sold/fiscalized.
- **BR-008** Inventory history is corrected with explicit compensating/adjustment movement, not deletion.
- **BR-009** Receivable/payable balance is derived from original obligation plus valid allocations/adjustments.
- **BR-010** Technical log entries are not a substitute for durable business/security audit.
- **BR-011** External integration acknowledgement state is not collapsed into business/fiscal acceptance state.
- **BR-012** Provider-specific DB/API logic cannot leak into Domain/Application contracts.

## Requirements intentionally OPEN

These require further business/regulatory decision before their final acceptance criteria:

- exact Release-1 CFE family enablement;
- direct DGI vs provider integration;
- certificate/key custody;
- detailed negative-stock/backorder policy;
- customer credit limits/approval;
- overpayment/advances policy;
- PPP/FIFO initial scope;
- accounting export formats;
- WhatsApp/provider selection;
- detailed e-Resguardo tax-rule catalog;
- detailed remito correction workflow;
- final SLOs/retention/backup RPO/RTO.
