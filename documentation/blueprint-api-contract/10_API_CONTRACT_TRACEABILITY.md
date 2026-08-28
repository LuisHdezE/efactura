# API Contract Traceability

## Semantic chain

The v1 contract follows:

`Requirement -> Use Case -> Interface Scope -> API ID / operationId -> later OpenAPI -> implementation/test/evidence`.

Endpoint inventories are the operation authority. This document records requirement-area coverage and explicitly identifies behavior that is internal rather than an HTTP resource.

## Major requirement coverage

| Requirement area | Contracted API families |
|---|---|
| `FR-001..005` foundation/identity/audit/db choice | IAM, ORG, AUD, CFG; database choice is deployment architecture rather than HTTP input |
| `FR-010..019` parties/catalog/fiscal identity | PTY, CAT, REF |
| `FR-020..027` sales/payment/cash | POS, SAL, PMT, COL, PAY, CSH |
| `FR-030..049` CFE/CAE/cross-border | SAL fiscal preview/confirm, FIS, CAE, DFR, REF fiscal metadata; specialized families deferred as below |
| `FR-050..056` contingency/offline | CNT, DEV, SYN |
| `FR-060..068` inventory/procurement | INV, TRF, RPL, PRC, GRC |
| `FR-070..074` CxC/CxP/treasury/cash flow | AR, AP, COL, PAY, REP |
| `FR-080..086` received CFE/reporting/integrations | RCV, XML, REP, CAL, FDL, AEX, INT |
| security/integrity NFRs | auth matrix, Problem Details, idempotency/concurrency, audit mapping |
| PostgreSQL/MySQL portability | no client-selected DB endpoint; deployment Infrastructure fulfills NFR-005 with shared contract tests |

## Internal requirements that do not need a public endpoint

- Fiscal signing/certificate use occurs behind `IFiscalSigner`; no public signing-key endpoint.
- DGI/provider transport occurs behind `IFiscalTransportGateway`; provider SDK routes/callback mechanics are Infrastructure contracts, not client APIs.
- Fiscal number reservation is an internal atomic application/domain step triggered by issuance; clients never call `nextNumber`.
- Outbox/inbox dispatch is background reliability infrastructure; safe operational status is exposed, message mutation is not.
- Durable audit emission is a cross-cutting side effect of authorized operations; clients cannot post arbitrary audit facts.
- Exact PostgreSQL/MySQL selection and migrations are deployment concerns.
- NFR testing/performance/backup requirements become implementation/QA/operations evidence, not CRUD endpoints.

## Specialized fiscal traceability

| Capability | Requirement/use-case source | Contract disposition |
|---|---|---|
| e-Remito / e-Remito Exportación | FR-041/049, specialized fiscal use cases | `DEFERRED_PENDING_RULES` |
| e-Resguardo | FR-041 | `DEFERRED_PENDING_RULES` |
| e-Boleta de Entrada | FR-041 / procurement fiscal use case | `DEFERRED_PENDING_RULES` |
| Venta por Cuenta Ajena | FR-041 | `DEFERRED_PENDING_RULES` |
| export-of-services documentation strategy | FR-047/048 | tax/fiscal preview is contracted; specialized issue command deferred until strategy/rules accepted |

Architecture supports these modules. The contract intentionally refuses to publish an unsafe generic `POST /fiscal-documents { type: any }` escape hatch.

## Interface traceability

`09_INTERFACE_SCOPE_RECONCILIATION.md` maps every accepted `WEB-001..WEB-018` and `APP-001..APP-008` scope item to operationIds or an explicit external/deferred boundary.

The later `EXECUTABLE_INVENTORY` must bind COMMITTED interfaces to these real operationIds only after API Gate.

## Brownfield traceability

`08_BROWNFIELD_COMPATIBILITY_MATRIX.md` maps the 69 observed legacy operations by controller family to v1 resources or explicit non-replacement decisions. Old behavior is not silently rewritten into v1, and v1 quality rules are not falsely attributed to legacy endpoints.

## Requirement truth rules

- Demo/reference behavior never overrides accepted DGI-backed requirement evidence.
- A screen/request from Interface Scope does not create a server business rule.
- A request DTO cannot carry an authoritative tax treatment/CFE type when the server selector owns that decision.
- A client-calculated balance/stock/tax is display/provisional data only until server canonical response.
- A provider callback cannot directly mutate domain state without inbox validation/idempotency/response interpretation.

## operationId stability

Once human API Contract acceptance is recorded:

- API IDs are stable project references;
- `operationId` is the canonical future OpenAPI identifier;
- OpenAPI/Postman/tests/client inventory use it exactly;
- rename/removal is a contract change, not refactoring noise;
- after API consumers exist, affected operationId/auth/error/versioning changes require Blueprint API-impact analysis and scoped revalidation.