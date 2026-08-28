# Target Traceability and Open Questions

## Source classes

Target decisions are tracked from distinct source classes:

- `AS_IS`: behavior/evidence in current `efactura`.
- `REFERENCE`: capability/workflow observed in `FacturacionElectronicaBases`.
- `DGI`: official regulatory/technical source.
- `NEW_REQUIREMENT`: explicit product requirement supplied by the owner.
- `PROPOSED_TARGET`: architecture/design derived from the previous classes.

These classes must never be collapsed into a single “existing requirement”.

## High-level traceability

| Target capability | Source | Evidence / rationale |
|---|---|---|
| Preserve .NET/C# architecture style | AS_IS + NEW_REQUIREMENT | Current solution + explicit stack-preservation constraint. |
| PostgreSQL/MySQL selectable deployment | NEW_REQUIREMENT | Explicit requirement in current target definition. |
| Goods/services/mixed sales | REFERENCE + NEW_REQUIREMENT | Reference product/service concepts + barber/service example. |
| POS sales | REFERENCE | POS view/server/demo + endpoint design. |
| CFE lifecycle | REFERENCE + DGI | Demo know-how corrected by official DGI lifecycle. |
| Versioned fiscal rules | DGI + PROPOSED_TARGET | DGI format changes, current v25.2. |
| CAE company-wide sequence | DGI | Official numbering/CAE definitions. |
| CFC contingency module | DGI + NEW_REQUIREMENT | Official mandatory module + explicit offline concern. |
| Offline sync/idempotency | REFERENCE + NEW_REQUIREMENT | Reference API note + explicit web/mobile offline requirement. |
| Inventory | AS_IS + REFERENCE | Existing entities + reference POS/catalog/stock workflows. |
| EOQ/ROP | REFERENCE | SimulacionCompraView/dossier. |
| Procurement/receiving | REFERENCE | Purchase simulation/stock-receipt concept, redesigned as proper workflow. |
| CxC/CxP | AS_IS + REFERENCE + NEW_REQUIREMENT | Current accounting entities and full reference flows. |
| Durable audit | REFERENCE + Blueprint REQUIRED | Dossier/audit UI plus Blueprint audit requirements. |
| XML validator | REFERENCE + DGI | Reference tool, target implementation must use official schemas/rules. |
| Fiscal daily report | REFERENCE + DGI | Reference demo corrected by DGI definitions. |
| Cash shift/reconciliation | REFERENCE | Dossier/dashboard capability. |
| Fiscal calendar/cash flow | REFERENCE | Dossier/dashboard capability. |

## Decisions already fixed

- API/backend is the current implementation scope; visual frontend is deferred.
- Reference repository is know-how, not code/stack source.
- .NET/C#/ASP.NET Core remain the target stack.
- PostgreSQL and MySQL are both supported deployment options.
- future web/mobile clients require offline-capable workflows.
- fiscal numbering remains server-authoritative under the baseline architecture.
- technical logs and durable business audit remain separate.
- accepted fiscal evidence is not destructively edited/deleted.

## Open decisions before fiscal implementation

### OQ-001 — Direct DGI vs authorized CFE provider

Need business/deployment decision and provider documentation. Architecture will support an adapter boundary either way.

**Blocks:** final transport/auth/provider-specific API implementation.

### OQ-002 — Initial enabled CFE families

Determine which document families Release 1 must enable after completing the official applicability matrix. The reference demo subset is not authoritative.

**Blocks:** final endpoint/domain acceptance for specialized document flows, not generic core architecture.

### OQ-003 — Certificate custody

Options may include OS certificate store, HSM, Key Vault/secret manager or provider custody depending deployment/integration mode.

**Blocks:** production signing implementation/security review.

### OQ-004 — CFC operational process for each customer

Need to define how authorized/preprinted CFC stock is managed at locations, operators and terminals while respecting DGI rules.

**Blocks:** production offline-contingency rollout.

### OQ-005 — Negative stock/business reservation policy

Reference demo silently floors stock at zero. Target must decide whether overselling is blocked, allowed with backorder, or supervisor-approved by item/location/profile.

**Blocks:** inventory acceptance criteria.

### OQ-006 — Credit policy

Need customer credit terms/limits/approval/overpayment/advance rules.

**Blocks:** advanced CxC policy, not base receivable persistence.

### OQ-007 — Costing scope

PPP and FIFO/PEPS are requested capabilities from the reference dossier. Decide whether both are required in initial release and whether valuation is perpetual/periodic.

### OQ-008 — Accounting export formats

Memory Conty, ZetaSoftware, Gia-Marcos and others are reference targets; exact current import specifications must be obtained before implementing adapters.

### OQ-009 — Fiscal calendar provenance

DGI/BPS due dates can change. Need authoritative source/update process, not hard-coded dates.

### OQ-010 — Messaging providers

Email is core-ready; WhatsApp requires provider/consent/template decisions.

## Questions resolved by official DGI evidence, not open anymore

- CAE numbering is not branch-independent by default: numbering is unique by CFE type for the company.
- CFC contingency is not equivalent to generating a new normal CFE after reconnecting.
- final acceptance is not the same as synchronous envelope receipt.
- an accepted/non-rejected CFE is corrected through correction documents, not destructive editing.

## Gate discipline

This target reconstruction does not mark any Blueprint gate PASS. Before API implementation, the consumer still needs accepted evidence for:

1. Brownfield Inspection / AS-IS;
2. Gap Analysis;
3. TO-BE/Target Definition;
4. Requirements & Domain, including actors, functional/non-functional requirements, rules, use cases, acceptance criteria and traceability;
5. Architecture/Security/Data;
6. API Scope & Contract Design.

The documents in this branch are inputs to those gates, not substitutes for approval.
