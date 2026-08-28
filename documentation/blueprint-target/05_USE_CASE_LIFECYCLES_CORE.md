# Core Use Case Lifecycles

## Purpose

This document defines the first target business lifecycles before database/API implementation. They are derived from:

- the existing eFactura brownfield system;
- `FacturacionElectronicaBases` reference behavior and UI workflows;
- the user's new product requirements;
- the DGI regulatory baseline documented in `02_REGULATORY_BASELINE_DGI_URUGUAY.md`.

These are **target use cases**, not AS-IS claims.

## Common execution rules

Every command use case records:

- actor and permission;
- organization/branch/terminal context where applicable;
- `Idempotency-Key` or equivalent business operation identifier when replay is possible;
- request/correlation ID;
- durable audit event for fiscal, financial, inventory, security and administrative changes;
- optimistic/concurrency controls when master/state changes can race;
- source timestamps (`occurredAt`) separately from server receipt timestamps when operations can synchronize later.

No controller owns domain rules. External DGI/provider calls occur through application ports/adapters.

---

# UC-ORG-001 — Configure company fiscal profile

**Actors:** Administrator.

**Preconditions:** authenticated user with configuration permission.

**Flow:**
1. Register legal/fiscal identity, commercial name, addresses and relevant issuer profile data.
2. Register branches/locations and POS terminals as operational origins.
3. Select business profile: `GOODS`, `SERVICES` or `MIXED`.
4. Select database provider at deployment/configuration level, not per request.
5. Configure fiscal integration mode/provider without exposing secrets through read APIs.
6. Validate mandatory configuration for enabled capabilities.
7. Persist configuration version and audit before/after values.

**Invariant:** changing company master data never mutates snapshots already embedded in historical fiscal documents.

**Audit:** `ORGANIZATION_FISCAL_PROFILE_CHANGED`, `INTEGRATION_CONFIG_CHANGED`.

---

# UC-IAM-001 — Authenticate and establish session

**Actors:** User.

**Flow:**
1. Validate credentials/identity provider.
2. Verify user is active and allowed for the organization/branch context.
3. Issue short-lived access token and refresh-session record.
4. Return user/role/permission/branch context without secrets.
5. Audit successful and failed security-relevant events according to policy.

**Related:** refresh, logout/revocation, forced password reset, device/session invalidation.

**Invariant:** role names are presentation/grouping; authorization is ultimately permission/policy based.

---

# UC-CAE-001 — Import/register and activate CAE

**Actors:** Administrator/authorized fiscal operator.

**Preconditions:** issuer is configured.

**Flow:**
1. Receive CAE file/metadata from authorized source.
2. Parse authorization number, CFE type, series/range, validity and source metadata.
3. Validate integrity/signature when applicable to the official CAE artifact.
4. Verify range does not conflict with existing ranges/consumption.
5. Persist as inactive/validated.
6. Activate according to numbering policy.
7. Schedule low-range and expiry monitoring.
8. Audit import/activation.

**Critical rule:** numbering is company-wide by CFE type; branch is operational context, not an independent fiscal sequence.

**Concurrency:** all number reservations must serialize correctly across terminals and both supported databases.

---

# UC-CASH-001 — Open POS/cash shift

**Actors:** Cashier/administrator.

**Flow:**
1. Select terminal/branch.
2. Ensure no conflicting open shift exists under configured policy.
3. Record opening float by currency/payment bucket as applicable.
4. Persist shift identity, operator and opening timestamp.
5. Audit opening.

**Offline:** a cached/offline client may display last known status, but server authority is required to open a new canonical shift unless an approved offline-terminal strategy is later introduced.

---

# UC-SALE-001 — Create and validate sale draft

**Actors:** Cashier/authorized seller.

**Flow:**
1. Create draft with client operation ID.
2. Select optional customer or consumer-final context according to fiscal rule applicability.
3. Add `CommercialItem` lines. A line may represent a stock-tracked product or a service.
4. Apply quantity, unit, price, discounts/recargos and tax assignment.
5. Calculate commercial/fiscal preview with decimal arithmetic.
6. Validate required receiver data and enabled business/fiscal rules.
7. For stock-tracked lines, verify availability policy without yet deleting/mutating historical stock.
8. Return validation findings and totals.

**State:** `DRAFT -> VALIDATED`.

**Invariant:** a service line never requires inventory merely because product inventory exists elsewhere in the system.

---

# UC-SALE-002 — Confirm cash sale and initiate fiscalization

**Actors:** Cashier/administrator.

**Preconditions:** validated sale, open terminal/shift when POS mode requires it.

**Flow:**
1. Receive idempotent confirmation command.
2. Revalidate prices/taxes/customer/permissions and relevant stock conditions.
3. Determine applicable fiscal-document rule/profile through the versioned fiscal policy engine.
4. Record payment intent/record for the chosen payment medium.
5. Confirm sale and create durable fiscalization work in the same local consistency boundary where appropriate.
6. Reserve stock for stock-tracked lines or produce the corresponding stock movement at the defined completion boundary.
7. Fiscal module atomically reserves the appropriate authorized number only when normal CFE issuance is valid.
8. Generate fiscal snapshot and official-format XML for the active specification version.
9. Validate schema/business rules and sign with the configured certificate.
10. Persist signed fiscal artifact and outbox/transport work.
11. Attempt/queue DGI/provider transmission according to the document-specific rule.
12. Persist envelope/transport acknowledgement separately from document acceptance.
13. When final DGI/provider response arrives, transition CFE to accepted/rejected/regularization state.
14. Return a stable operation representation so the client can poll/resume after interruption.

**Sale state:** `VALIDATED -> CONFIRMED -> COMPLETED` when commercial completion criteria are met.

**Fiscal state:** separate lifecycle; sale does not become “nonexistent” if transport is delayed.

**Audit:** sale confirmation, payment, fiscal number reservation, CFE generation/signing/submission/response, stock movement.

**Idempotency:** repeating the command with the same key cannot create another sale, payment or CFE.

---

# UC-SALE-003 — Confirm credit sale and create receivable

Same fiscal flow as UC-SALE-002 with these additions:

1. Validate customer and credit terms.
2. Persist receivable from the confirmed commercial transaction/fiscal evidence.
3. Set due date/terms and original amount/currency.
4. Keep receivable balance derived from allocations, not from destructive amount edits.
5. Expose aging/status (`OPEN`, `PARTIAL`, `SETTLED`, `OVERDUE`, etc.).

**Audit:** `RECEIVABLE_CREATED`.

---

# UC-SALE-004 — Sell services only

**Purpose:** support businesses such as barber shops/consultancies.

**Flow:** same sales/fiscal/payment path, but no inventory reservation/movement for non-stock items.

**Invariant:** inventory module can be disabled while POS, customers, CFE, payments, cash management and receivables remain functional.

---

# UC-SALE-005 — Mixed product + service sale

**Flow:**
1. One sale may contain both stock-tracked and non-stock lines.
2. Fiscal totals aggregate all lines under the applicable tax rules.
3. Only stock-tracked lines create inventory effects.
4. Correction notes preserve line/type/tax/reference history.

---

# UC-FISC-001 — Process asynchronous CFE transmission result

**Actors:** background worker/webhook/polling adapter.

**Flow:**
1. Correlate provider/DGI transaction to persisted envelope/document.
2. Authenticate/validate callback or polling response source.
3. Interpret transport acknowledgement separately from individual CFE response.
4. Persist original response artifact/code/message.
5. Transition fiscal document through allowed state machine.
6. Create alert/work item for rejection/regularization states.
7. Audit transition.
8. Notify interested client channels without requiring the original request connection.

**Invariant:** `ENVELOPE_ACKNOWLEDGED` does not equal `CFE_ACCEPTED`.

---

# UC-FISC-002 — Correct accepted/non-rejected CFE with credit/debit note

**Actors:** administrator/authorized fiscal role.

**Preconditions:** original CFE is eligible for correction under current fiscal policy.

**Flow:**
1. Load immutable original fiscal snapshot.
2. Require reason and correction scope.
3. Determine permitted correction-note type and reference rules.
4. Validate amounts/lines and prevent correction beyond remaining correctable balance/quantity when applicable.
5. Reserve new authorized number for the correction document.
6. Generate, validate, sign and transmit the new referenced CFE.
7. Never modify/delete the original accepted document.
8. Update commercial/accounting consequences through explicit adjustments once policy conditions are met.
9. Audit before/reference/reason/new-document relationship.

**Idempotency:** mandatory.

---

# UC-FISC-003 — Handle rejected CFE

**Actors:** fiscal operator/background process.

**Flow:**
1. Persist rejection response/code and immutable generated document.
2. Evaluate the versioned rejection-resolution matrix.
3. Classify as `ANNULMENT_REQUIRED`, `REGULARIZATION_REQUIRED`, `RETRY_TRANSPORT`, `HUMAN_REVIEW` or another approved disposition.
4. Never simply delete/reuse the sent number.
5. Execute only permitted follow-up workflow.
6. Maintain links to replacement/correction/regularization artifacts if any.
7. Audit all decisions and actor actions.

**Open rule:** exact DGI response-code matrix must be loaded/validated for the implementation slice.

---

# UC-CONT-001 — Issue/register fiscal contingency document (CFC)

**Actors:** cashier/fiscal operator according to policy.

**Trigger:** electronic issuance system cannot be used and the operation must continue under the official contingency regime.

**Flow:**
1. Enter contingency mode with explicit reason/start time/affected component.
2. Use the authorized contingency document process and its own preprinted/authorized numbering.
3. Do **not** reserve/consume a normal CFE CAE number for the CFC.
4. Record the CFC in the system contingency registry when connectivity/system access permits; if client itself is offline, capture minimum local linkage and synchronize later.
5. Preserve CFC number/type substituted/customer/amount/lines/operator/terminal/time.
6. Treat the issued CFC as the fiscally valid document for the operation.
7. After recovery, complete the mandatory electronic information/transmission for that CFC according to the substituted CFE type.
8. Include CFC in daily-report consumption/reporting as required.
9. Close/reconcile contingency episode only after outstanding CFCs are processed.

**Critical invariant:** recovery does not silently create an unrelated normal CFE that duplicates the fiscal operation.

---

# UC-SYNC-001 — Synchronize an offline business-operation batch

Detailed protocol is in `06_OFFLINE_CONTINGENCY_AND_SYNC.md`.

**Summary:**
1. client submits batch with device/operation IDs and local ordering;
2. server deduplicates idempotently;
3. validates current master/rule state;
4. accepts, rejects or flags conflicts per operation;
5. preserves original occurrence time plus server receipt time;
6. links offline sale to CFC when a fiscal contingency document was issued;
7. returns per-operation canonical IDs/states and next sync cursor.

---

# UC-INV-001 — Manual stock adjustment

**Actors:** authorized inventory/admin role.

**Flow:**
1. Identify item/location and current quantity/version.
2. Require adjustment reason and positive/negative quantity delta.
3. Validate authorization and configured negative-stock policy.
4. Append immutable stock movement; do not rewrite history.
5. Update/recalculate stock position atomically.
6. Audit before/after, reason, actor and terminal.

**Idempotency:** required for offline/retry-capable clients.

---

# UC-INV-002 — Transfer stock between locations

**Flow:**
1. Create transfer order from source to destination.
2. Reserve/dispatch from source.
3. Record in-transit state if operationally needed.
4. Receive at destination.
5. Create paired traceable movements.
6. Handle discrepancies through explicit adjustment, not hidden overwrite.

---

# UC-PROC-001 — Generate replenishment/EOQ proposal

**Actors:** purchaser/inventory manager.

**Flow:**
1. Select stock scope/category/supplier.
2. Load historical demand from real sale/stock movements; no mock velocity assumptions.
3. Load configured order cost, holding-cost policy, lead time, safety stock and horizon.
4. Compute advisory EOQ/ROP/coverage metrics.
5. Present candidate lines and explanation inputs.
6. User may override quantities with reason if policy requires.
7. Save proposal/simulation snapshot.
8. No stock mutation occurs.

**Nature:** advisory analytics, not fiscal truth.

---

# UC-PROC-002 — Approve purchase order and receive goods

**Flow:**
1. Convert proposal/manual request to purchase order.
2. Select supplier, currency, expected dates and lines.
3. Approve according to permission/workflow.
4. On receipt, record quantities/costs and discrepancies.
5. Append inventory receipt movements for stock-tracked items.
6. Optionally create/link supplier payable and received fiscal-document record.
7. Update costing layers/PPP according to enabled costing policy.
8. Audit approval and receipt.

**Invariant:** EOQ UI cannot directly “apply stock” without a purchase-receipt business event.

---

# UC-AR-001 — Record customer collection and allocate payment

**Actors:** cashier/treasury.

**Flow:**
1. Create payment/collection with external reference/payment medium.
2. Idempotently persist payment.
3. Allocate all/part to one or more receivables according to policy.
4. Recalculate open balances from allocations.
5. Generate receipt/document representation when required by business policy.
6. Update cash-shift expected totals if applicable.
7. Audit payment and allocations.

**Invariant:** overpayment/advance requires an explicit policy; do not silently truncate as the demo does.

---

# UC-AP-001 — Record supplier payment and allocate

Mirror of UC-AR-001 for payables, with supplier/treasury permissions and cash/bank consequences.

**Invariant:** never silently cap an entered payment without returning/handling the unapplied amount according to policy.

---

# UC-CASH-002 — Close and reconcile cash shift

**Actors:** cashier + optional supervisor.

**Flow:**
1. Freeze/select shift for closing.
2. Calculate expected totals by payment medium from recorded transactions.
3. Capture physical counts/vouchers/transfers/checks.
4. Calculate variances.
5. Require explanation/approval above configured tolerance.
6. Close shift and prevent ordinary mutation of closed records.
7. Audit counted values, expected values, variance and approvals.

---

# UC-RCV-001 — Register/import received CFE

**Actors:** accountant/admin/integration worker.

**Flow:**
1. Receive XML/provider event/manual metadata.
2. Store original artifact/hash/source.
3. Validate XML schema/signature/issuer identity/CAE/fiscal arithmetic under its declared format version.
4. Detect duplicate document identity.
5. Link or create supplier master candidate without mutating source evidence.
6. Optionally create payable/purchase association after human or rule-based approval.
7. Persist validation findings and audit intake.

---

# UC-XML-001 — Validate one or a batch of fiscal XML files

**Actors:** accountant/auditor.

**Flow:**
1. Upload/import files under size/type controls.
2. Detect specification/document type.
3. Perform XSD validation.
4. Verify digital signature/certificate chain according to applicable policy.
5. Validate RUT/identity, CAE/range/date and mathematical/tax consistency.
6. Produce structured findings by severity/code/source rule.
7. Persist optional validation run/report metadata.
8. Exportable visual/PDF rendering is a later frontend/report concern.

---

# UC-REP-001 — Generate and submit daily fiscal report

**Actor:** scheduler/fiscal operator.

**Flow:**
1. For each required calendar date, aggregate CFE/CFC consumption according to official report rules.
2. Include issued/annulled/contingency consumption and required monetary conversion rules.
3. Generate official XML and validate it.
4. Sign with valid issuer certificate.
5. Persist report/version/hash.
6. Submit within the applicable DGI window.
7. Persist acknowledgement/rejection and support corrective/reliquidation workflow when required.
8. Generate a report even for no-operation days when required from issuer effective date.
9. Audit generation/submission/response.

**Invariant:** management “sales report” and DGI Daily Report are different artifacts.

---

# UC-AUD-001 — Query/export durable audit trail

**Actors:** administrator/auditor according to permission.

**Flow:**
1. Filter by actor/event/module/entity/date/correlation/device/IP where available.
2. Return event time, actor, source context, entity, action, before/after or relevant immutable metadata.
3. Protect sensitive values through redaction policies.
4. Export uses the same server-side authorization and audit query.
5. Audit security-sensitive audit-log access/export if policy requires.

---

# UC-MON-001 — Monitor CAE/certificate/fiscal integration health

**Flow:**
1. Scheduled checks evaluate CAE expiry/exhaustion, certificate expiry, unsent/rejected fiscal work, provider/DGI health and stuck daily reports.
2. Generate durable alerts with severity/deduplication/acknowledgement.
3. Surface API data for future dashboard/notifications.
4. Never expose certificate secrets/private key material.

---

## Remaining use-case backlog

These must be expanded before their implementation slice:

- fiscal document selection matrix by transaction/receptor profile;
- e-Remito and stock/transport lifecycle;
- e-Resguardo and withholding/perception lifecycle;
- export-document families;
- e-Boleta de Entrada and account-on-behalf families when enabled;
- costing PPP/FIFO close/revaluation rules;
- fiscal calendar ingestion/provenance/update;
- accounting export adapters;
- email/WhatsApp delivery retries and consent/contact rules;
- supplier/customer master deduplication/merge;
- customer credit limits and approvals if product scope enables them;
- backup/restore operational runbooks.

No endpoint implementation is authorized merely because a use case appears in this document. Requirements, architecture/security/data and API contract gates still apply.
