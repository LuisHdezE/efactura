# Specialized Fiscal Use Case Lifecycles

This document expands the specialized fiscal workflows omitted from `05_USE_CASE_LIFECYCLES_CORE.md`. It is a target requirements artifact, not code authorization.

## UC-REM-001 — Issue e-Remito for domestic physical movement

**Actors:** authorized logistics/inventory operator.

**Trigger:** a qualifying physical movement of goods requires e-Remito under the active rule/profile.

**Flow:**
1. Create/identify the underlying movement intent: transfer, delivery, dispatch or other configured reason.
2. Capture origin, destination, parties, logistics context and stock-tracked lines.
3. Validate that the operation is a goods movement and the selected e-Remito family is applicable.
4. Validate stock/dispatch authorization independently from fiscal validation.
5. Atomically reserve the authorized fiscal number from the company-wide CFE-type sequence.
6. Build the e-Remito fiscal snapshot under the active DGI specification.
7. XSD/business-rule validate, sign and persist immutable XML/artifact metadata.
8. Submit before the applicable transport/movement boundary required by DGI rules.
9. Record transport and final fiscal acknowledgement independently.
10. Dispatch/post inventory movement through the linked business transaction.
11. Audit document, movement, actor, source/destination and result.

**Failure:** a rejected fiscal document does not erase the physical movement record; it creates a fiscal regularization work item.

**Open before implementation:** exact current correction procedure for e-Remito must be formally validated. No generic `DELETE /remitos/{id}` is permitted.

---

## UC-EXP-001 — Issue e-Factura de Exportación

**Actors:** authorized sales/export operator.

**Preconditions:** issuer enabled for export family; transaction satisfies export applicability.

**Flow:**
1. Build export sale with foreign receiver, destination/country and required export metadata.
2. Determine whether goods or services are exported and whether this CFE family is required/optional under current rule.
3. Validate currency/exchange-rate and required fiscal fields under the active format version.
4. Validate line/tax/export treatment; do not reuse domestic VAT assumptions blindly.
5. Reserve export-family fiscal number.
6. Generate/validate/sign e-Factura Exportación.
7. Persist and submit according to DGI/export timing requirements and external integration needs.
8. Record acknowledgement and archival artifacts.
9. If credit sale, create receivable under commercial terms independently from fiscal acceptance state.
10. Audit full lifecycle.

**Corrections:** use export NC/ND family (122/123) subject to reference/correction rules; original remains immutable.

---

## UC-EXP-002 — Issue e-Remito de Exportación

**Purpose:** document physical movement of goods in export using the official valued document family.

**Flow:**
1. Link to export sale/shipment when available.
2. Validate goods/logistics/export data.
3. Reserve number from type 124 authorized sequence.
4. Create valued remito fiscal snapshot.
5. Generate/validate/sign/transmit before the applicable movement/customs boundary.
6. Persist shipment/document relationship and audit.

**Invariant:** not applicable to a pure service export without physical goods movement.

---

## UC-RSG-001 — Determine and issue e-Resguardo

**Actors:** authorized treasury/accounting/fiscal operator or automated rule engine with audited approval policy.

**Flow:**
1. Identify underlying payment/operation and parties.
2. Evaluate current retention/perception/credit-communication regulation.
3. Determine whether the amount must instead be represented in the supporting CFE and therefore no separate e-Resguardo is needed.
4. If standalone e-Resguardo is required, calculate/reference the applicable tax concepts and bases using versioned rules.
5. Validate receiver/retained-party identity and required references.
6. Reserve authorized e-Resguardo number.
7. Generate/validate/sign/transmit.
8. Persist links to payment/operation/tax obligation.
9. Record acknowledgement and audit.

**Critical invariant:** no generic fixed percentage in application code. Retention/perception rules are regulatory configuration/rule versions with source provenance.

---

## UC-BE-001 — Issue e-Boleta de Entrada for qualifying purchase

**Actors:** authorized procurement/accounting operator.

**Preconditions:** purchase qualifies under the current DGI e-Boleta de Entrada rule.

**Flow:**
1. Register acquisition/seller and source evidence.
2. Determine seller electronic/documentation status and purchase scenario.
3. Verify that seller has not already documented an operation for which e-Boleta de Entrada would be inappropriate.
4. Evaluate whether this is the regulated non-documented-seller purchase case or another explicitly covered case such as applicable FX-resale purchase.
5. Capture mandatory seller/receiver identity fields under current threshold/rule.
6. Calculate purchase amounts, taxes and any allowed retention/perception information.
7. Reserve e-Boleta de Entrada number.
8. Generate/validate/sign/transmit.
9. Link to purchase, inventory receipt and/or payable without losing the original fiscal snapshot.
10. Audit rule result and source evidence.

**Corrections:** use 152/153 as allowed; original 151 remains immutable.

**Invariant:** user cannot choose e-Boleta de Entrada merely because supplier lacks an invoice in the application.

---

## UC-CTA-001 — Record sale on behalf of a principal (Venta por Cuenta Ajena)

**Actors:** authorized sales operator.

**Preconditions:** issuer/transaction belongs to the applicable account-on-behalf regime.

**Flow:**
1. Select principal/represented party and establish contractual/business context.
2. Create sale lines identifying economic ownership as required by the applicable rule.
3. Determine consumption-final vs taxpayer receiver path.
4. Fiscal selector chooses 131 or 141 family as appropriate, not ordinary 101/111.
5. Calculate commercial/fiscal values according to the account-on-behalf rule version.
6. Reserve family-specific number.
7. Generate/validate/sign/transmit.
8. Persist principal, receiver and intermediary snapshots.
9. Create financial settlement/accounting consequences separately from ordinary own-account sales.
10. Audit principal/agent context.

**Corrections:** 132/133 or 142/143 as applicable.

---

## UC-CFC-002 — Manage contingency-document stock/ranges

**Actors:** administrator/fiscal supervisor.

**Purpose:** support the official CFC process operationally without confusing it with CAE normal CFE numbering.

**Flow:**
1. Register authorized/preprinted contingency document stock/range and physical location/custodian when applicable.
2. Track availability by corresponding CFE/CFC family.
3. Reconcile issued, unused, annulled/lost/damaged records according to approved policy.
4. Prevent assigning one CFC identity to two business operations.
5. Alert low stock/expiry/invalid state if applicable.
6. Audit custody and status changes.

**Invariant:** generic offline clients cannot manufacture a CFC number that has not been authorized/registered by the contingency process.

---

## UC-CFC-003 — Recover and report outstanding contingency operations

**Flow:**
1. Detect/open contingency episode and outstanding CFC records after system recovery.
2. Synchronize locally captured sale details if needed.
3. Validate CFC identity and substituted CFE family.
4. Create required electronic representation/data for the CFC according to current DGI rules.
5. Submit/report without replacing the CFC's fiscal identity with an unrelated normal CFE.
6. Include consumption in daily report according to official rules.
7. Persist acknowledgements/findings.
8. Close contingency episode only after all required follow-up is reconciled or explicitly escalated.
9. Audit recovery.

---

## UC-REP-002 — Correct/reliquidate fiscal daily report

**Actors:** background/fiscal operator.

**Trigger:** DGI rejection, detected discrepancy or officially allowed correction/reliquidation need.

**Flow:**
1. Load immutable previously generated daily-report version and DGI response.
2. Recalculate from authoritative CFE/CFC consumption and official rule version.
3. Explain delta between prior and corrected version.
4. Generate next permissible report/reliquidation artifact.
5. Validate/sign/submit.
6. Preserve all versions and responses.
7. Audit reason, actor/automation and delta.

---

## UC-INT-001 — Exchange CFE with another electronic issuer

**Actors:** integration worker.

**Scope:** applicable when CFE exchange/acknowledgement obligations are enabled.

**Flow:**
1. Determine whether recipient is an electronic issuer and exchange channel/provider contract applies.
2. Deliver authorized CFE artifact through configured channel after required DGI submission condition is met.
3. Receive/persist recipient delivery/acknowledgement information separately from DGI fiscal acceptance.
4. Retry idempotently and retain delivery evidence.
5. Audit exchange failures and escalation.

**Open:** exact provider/service-web exchange contract must be confirmed against current DGI/provider documentation before implementation.

---

## Specialized family enablement rule

Each specialized family has feature/profile state:

- `NOT_CONFIGURED`
- `ELIGIBLE`
- `ENABLED_TEST`
- `CERTIFIED/APPROVED_FOR_PRODUCTION` where applicable to the integration path
- `SUSPENDED`

The application must reject specialized issuance if the issuer is not enabled for that family or the required rule/configuration is incomplete.
