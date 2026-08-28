# Reference Capability Inventory

## Purpose

This document captures capabilities observed in `LuisHdezE/FacturacionElectronicaBases` and classifies them as reference know-how for the future state of `LuisHdezE/efactura`. It is **not** evidence that eFactura already implements these capabilities and it is **not** normative authority for DGI compliance.

## Baselines

- Consumer AS-IS baseline: `LuisHdezE/efactura@a6c9bf96572b8a0a88efde2c68b0749a71020a18`
- Reference repository baseline: `LuisHdezE/FacturacionElectronicaBases@456c0d6f2e543c91d84ea87df4166ab158c6bba2`
- Evaluation framework: `SoftwareDevelopmentBlueprint 0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`
- Technology preservation: ASP.NET Core/.NET/C# and the existing clean-architecture direction are preserved. Reference TypeScript/Express code is never copied as target architecture.

## Evidence classes

- `DEMO_IMPLEMENTED`: executable behavior exists in the reference demo.
- `UI_IMPLEMENTED`: a user-facing workflow exists, sometimes backed by in-memory state.
- `DOCUMENTED`: described in the dossier/API/architecture documents.
- `SIMULATED`: behavior presents an external/fiscal result without a real authority/provider behind it.
- `DEV_TOOL`: engineering/demo tooling, not a business capability.
- `TARGET_COMMIT`: intended for eFactura API scope.
- `TARGET_CONDITIONAL`: useful but only when enabled by customer profile or later decision.
- `FRONT_DEFERRED`: useful interface behavior, API must support its data but visual work is deferred.
- `EXCLUDE_RUNTIME`: do not expose as business-product runtime functionality.

## Capability inventory

| Area | Reference evidence | Reference state | Target disposition | Notes |
|---|---|---|---|---|
| Authentication/session | `/api/auth/login`, `/api/auth/me`, user profiles/roles | DEMO_IMPLEMENTED | TARGET_COMMIT | Replace demo token generation with production AuthN/AuthZ. |
| Company/emitter profile | `EmisorConfig`, dossier/API docs | DOCUMENTED/UI_IMPLEMENTED | TARGET_COMMIT | Fiscal configuration belongs to organization module. |
| Branches/terminals | API design + POS views | DOCUMENTED | TARGET_COMMIT | Branch identity must not imply branch-specific CAE numbering. |
| Users/roles/permissions | `UserProfile`, restricted access view | UI_IMPLEMENTED | TARGET_COMMIT | Move from booleans/demo roles to permission policy. |
| Customers | API + `ClientesView` + server CRUD | DEMO_IMPLEMENTED | TARGET_COMMIT | RUT/CI validation must be domain-level and fiscal rules versioned. |
| Suppliers | API + server CRUD + accounts | DEMO_IMPLEMENTED | TARGET_COMMIT | Supports purchasing/CxP and received fiscal docs. |
| Product catalog | server CRUD + `ProductosView` | DEMO_IMPLEMENTED | TARGET_COMMIT | Product is stock-tracked sellable item. |
| Services catalog | API design | DOCUMENTED | TARGET_COMMIT | Service is non-stock sellable item; same sale/fiscal engine. |
| Mixed goods/services | implied by target requirement | NEW_REQUIREMENT | TARGET_COMMIT | Commercial item abstraction must support product/service/mixed sales. |
| POS sale | `PosView`, CFE emission flow | UI_IMPLEMENTED/DEMO_IMPLEMENTED | TARGET_COMMIT_WITH_REDESIGN | Separate sale lifecycle from fiscal-document lifecycle. |
| Cash/credit sale | `FormaPago`, automatic CxC creation | DEMO_IMPLEMENTED | TARGET_COMMIT | Credit terms and receivable are explicit use cases. |
| CFE archive | `/api/cfe`, detail/XML, `CfeListView` | DEMO_IMPLEMENTED | TARGET_COMMIT | Demo acceptance state is simulated and must not be copied. |
| Fiscal document types | `TipoCFE`, dossier, OpenAPI | DOCUMENTED | TARGET_COMMIT_WITH_REDESIGN | Demo enum is incomplete for current DGI catalog. Use versioned fiscal catalog/rules. |
| CAE management | `/api/dgi/caes`, UI/DGI module | DEMO_IMPLEMENTED | TARGET_COMMIT_WITH_REDESIGN | CAE validation/number reservation must be concurrency-safe and DGI-correct. |
| XML generation | `dgiValidation.ts`, generated C# explorer | SIMULATED | TARGET_COMMIT_WITH_REVALIDATION | Must implement official XSD/version contracts, not string checks. |
| XMLDSig/signature | demo utility + C# reference | SIMULATED | TARGET_COMMIT_WITH_REVALIDATION | Real X.509 key handling and signing must be infrastructure-backed. |
| DGI/provider transport | `/api/cfe/:id/enviar-dgi`, envelope demo | SIMULATED | TARGET_COMMIT_WITH_REDESIGN | Introduce provider/direct-DGI adapter abstraction and asynchronous acknowledgements. |
| DGI envelope | `/api/dgi/sobres/enviar` | SIMULATED | TARGET_COMMIT | Current DGI envelope/report contracts must drive implementation. |
| DGI daily report | `/api/reportes/reporte-diario-dgi` | SIMULATED | TARGET_COMMIT_WITH_REVALIDATION | Current demo response is not compliance evidence. |
| Contingency | architecture document + UI notions | DOCUMENTED | TARGET_COMMIT_WITH_REDESIGN | Must implement official CFC module semantics; never invent normal CFE numbers offline. |
| Offline client operation | API design note | DOCUMENTED | TARGET_COMMIT | New explicit requirement: offline-capable web/mobile clients and safe resynchronization. |
| Idempotency | API design notes | DOCUMENTED | TARGET_COMMIT | Required for sales, fiscalization, payments, sync and other retry-sensitive commands. |
| Inventory stock | products + movement server endpoints | DEMO_IMPLEMENTED | TARGET_COMMIT | Only stock-tracked items participate. |
| Stock adjustments | `/api/inventario/ajuste` | DEMO_IMPLEMENTED | TARGET_COMMIT_WITH_REDESIGN | Reason, authorization, before/after and durable audit required. |
| Replenishment alerts | `AlertaPreventivaStock` | UI_IMPLEMENTED | TARGET_COMMIT | API supplies computed/current replenishment signals. |
| EOQ/Wilson simulation | `SimulacionCompraView` | UI_IMPLEMENTED | TARGET_COMMIT | Separate advisory simulation from purchase-order approval. |
| Reorder point / lead time | `SimulacionCompraView` | UI_IMPLEMENTED | TARGET_COMMIT | Parameters configurable; no mock demand assumptions in production. |
| Purchase order proposal | Excel/PDF + apply-to-stock workflow | UI_IMPLEMENTED | TARGET_COMMIT_WITH_REDESIGN | Receiving stock must occur through purchase receiving, not direct UI mutation. |
| Inventory costing PPP/FIFO | dossier | DOCUMENTED | TARGET_CONDITIONAL | Must be designed as costing policy, not UI calculation. |
| Accounts receivable | server CxC + `CuentasView` | DEMO_IMPLEMENTED | TARGET_COMMIT | Partial/full collection, aging, balances. |
| Accounts payable | server CxP + `CuentasView` | DEMO_IMPLEMENTED | TARGET_COMMIT | Partial/full supplier payments and source-document linkage. |
| Payment application | CxC/CxP payment endpoints | DEMO_IMPLEMENTED | TARGET_COMMIT_WITH_REDESIGN | Payment and allocation are separate durable records. |
| Cash register / shift | dossier/dashboard concepts | DOCUMENTED/UI_IMPLEMENTED | TARGET_COMMIT | Open, close, count, reconciliation, variance. |
| Payment-media reconciliation | dossier | DOCUMENTED | TARGET_COMMIT | Cash/card/transfer/check categories extensible. |
| Received CFE | API design/dossier | DOCUMENTED | TARGET_COMMIT | Manual/import/integration entry and validation. |
| XML batch validator | `ValidadorXmlCfes` | UI_IMPLEMENTED | TARGET_COMMIT | API-side validation service; visual report later. |
| Audit trail | `BitacoraAuditoriaView`, types, dossier | UI_IMPLEMENTED | TARGET_COMMIT_WITH_REDESIGN | Durable immutable-ish business/security audit, not frontend state. |
| Certificate management | types + DGI module | UI_IMPLEMENTED | TARGET_COMMIT | Secrets/private keys never returned to clients. |
| Email delivery | SMTP types/log | UI/DOCUMENTED | TARGET_COMMIT | Provider abstraction; delivery logs. |
| WhatsApp delivery | API design note | DOCUMENTED | TARGET_CONDITIONAL | External provider integration later. |
| Fiscal/management reports | report views + dossier | UI_IMPLEMENTED/DOCUMENTED | TARGET_COMMIT | Data/report APIs first; visual dashboards later. |
| Fiscal calendar | dossier/dashboard | DOCUMENTED/UI_IMPLEMENTED | TARGET_COMMIT | Source/provenance and update lifecycle required. |
| Cash-flow projection | dossier/dashboard | DOCUMENTED/UI_IMPLEMENTED | TARGET_COMMIT | Derived from receivables/payables plus planned events. |
| Accounting exports | dossier | DOCUMENTED | TARGET_CONDITIONAL | Adapter per external accounting format. |
| Backup/cloud controls | audit event types/UI hints | UI_IMPLEMENTED | TARGET_CONDITIONAL | Operational capability, not fiscal-domain core. |
| Swagger explorer | `SwaggerView` | DEV_TOOL | EXCLUDE_RUNTIME | OpenAPI remains engineering/API contract tooling. |
| Test runner UI | `TestsRunnerView` | DEV_TOOL | EXCLUDE_RUNTIME | CI/testing concern, not customer API. |
| C# code explorer/export | `CSharpExplorerView`, `csharpCodebase.ts` | DEV_TOOL | EXCLUDE_RUNTIME | Architecture reference only. |

## Important interpretation rules

1. `FacturacionElectronicaBases` supplies **capability and workflow ideas**, not fiscal authority.
2. Any feature that says or implies “DGI accepted” without a real integration is classified `SIMULATED`.
3. UI components are used to identify data/actions the future API must support, but no visual implementation is part of this phase.
4. All target business capabilities will be translated into the existing .NET stack and clean dependency direction.
5. Regulatory-sensitive logic must be tied to an effective rule/specification version and source provenance.
