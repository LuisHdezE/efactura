# eFactura WebApp Navigation Map

Status: `ACTIVE / GOVERNED PLANNING`

## 1. Purpose

Maintain one explicit product-level map of the complete WebApp navigation scope so implemented views, planned views and Sidebar visibility never drift apart.

Runtime navigation metadata is centralized in `src/WebApp/src/app/routes.tsx` and distinguishes `active` executable entries from `planned` visible-but-disabled entries. Planned options are not routes and must never produce dead links.

## 2. Current navigation facts

The accepted web scope contains `WEB-001..WEB-018` plus additive `WEB-019 Technical Operations Console`.

Current runtime/governance state:

- `WEB-001 / UI-AUTH-001` is standalone at `/acceso`;
- `WEB-002 / UI-DASHBOARD-001` is active at `/dashboard`;
- `WEB-003 / UI-POS-001` is active at `/pos`;
- `WEB-004 / UI-CUSTOMER-001` is active at `/clientes`;
- `WEB-005 / UI-SUPPLIER-001` is active at `/proveedores` and its light/dark deployed runtime was explicitly accepted on 2026-09-19;
- `WEB-006 / UI-CATALOG-001` is active at `/catalogo` and its light/dark deployed runtime was explicitly accepted on 2026-09-20; explicit demo/mock data and disabled write affordances remain in place until the governed API-integration lane enables them;
- `WEB-007 / UI-INVENTORY-001` is active at `/inventario` with explicit demo/mock data, read-only inventory positions/movements and disabled write affordance until the governed integration/permission lane enables `inventory.adjust`; its desktop light/dark deployed runtime was explicitly accepted on 2026-09-20;
- `WEB-008 / UI-TRANSFER-001` is reconciled, has approved visual baseline `v1-responsive-suite`, and is reserved for `/transferencias`, but remains `PLANNED_DISABLED` because `API-TRF-001..007` are accepted contracts with `MISSING_HTTP` implementation state;
- `WEB-009 / UI-PROCUREMENT-001` is reconciled, has approved visual baseline `v1-responsive-suite`, and is reserved for `/compras`, but remains `PLANNED_DISABLED` because `API-PRC-001..006` and `API-GRC-001..004` are accepted contracts with `MISSING_HTTP` implementation state;
- `WEB-010 / UI-RECEIVABLE-001` is reconciled and reserved for `/cuentas-por-cobrar`, but remains `PLANNED_DISABLED` because `API-AR-001..004` and `API-COL-001..003` are accepted contracts with `MISSING_HTTP` implementation state;
- `WEB-011..WEB-019` remain planned shell candidates with no executable route yet;
- all 18 shell-hosted product options remain visible in the Sidebar information architecture;
- 6 entries are interactive shell routes;
- 12 future entries remain visible as disabled/non-clickable options, not fake routes.

Current counts:

- Web scope total: **19**;
- standalone implemented: **1**;
- active shell routes: **6**;
- planned disabled shell options: **12**.

## 3. Sidebar information architecture

### Inicio
- Dashboard

### Comercial
- Punto de Venta
- Clientes
- Proveedores
- Productos y Servicios

### Inventario y Compras
- Inventario
- Transferencias
- Órdenes de compra y Recepciones

### Finanzas
- Cuentas por cobrar
- Cuentas por pagar
- Caja y conciliación

### Fiscal
- Documentos fiscales
- CAE
- Contingencia / Sincronización
- CFE recibidos

### Reportes
- Reportes y Calendario fiscal

### Administración
- Auditoría / Seguridad / Configuración
- Consola técnica

These groups are product-navigation concepts and do not alter backend bounded contexts or API ownership.

## 4. Governed navigation matrix

Legend:

- `ACTIVE`: executable route derived from the unified navigation registry and rendered as a link;
- `PLANNED_DISABLED`: visible product-navigation option with no executable route and no click behavior;
- `STANDALONE`: intentionally outside the application shell;
- `UI ID pending`: no stable `UI-*` identifier is assigned until reconciliation proves the view boundary;
- candidate routes are planning values only.

| WEB | Interface | Product group | Governed UI ID | Candidate / current route | Navigation state |
| --- | --- | --- | --- | --- | --- |
| `WEB-001` | Login and Session Entry | Standalone | `UI-AUTH-001` | `/acceso` | `STANDALONE` |
| `WEB-002` | Operational Dashboard | Inicio | `UI-DASHBOARD-001` | `/dashboard` | `ACTIVE` |
| `WEB-003` | POS Sale | Comercial | `UI-POS-001` | `/pos` | `ACTIVE` |
| `WEB-004` | Customers and Parties | Comercial | `UI-CUSTOMER-001` | `/clientes` | `ACTIVE` |
| `WEB-005` | Suppliers | Comercial | `UI-SUPPLIER-001` | `/proveedores` | `ACTIVE` |
| `WEB-006` | Products and Services Catalog | Comercial | `UI-CATALOG-001` | `/catalogo` | `ACTIVE` |
| `WEB-007` | Inventory and Movements | Inventario y Compras | `UI-INVENTORY-001` | `/inventario` | `ACTIVE` |
| `WEB-008` | Stock Transfers | Inventario y Compras | `UI-TRANSFER-001` | `/transferencias` reserved | `PLANNED_DISABLED` |
| `WEB-009` | Purchase Orders and Receipts | Inventario y Compras | `UI-PROCUREMENT-001` | `/compras` reserved | `PLANNED_DISABLED` |
| `WEB-010` | Accounts Receivable and Collections | Finanzas | `UI-RECEIVABLE-001` | `/cuentas-por-cobrar` reserved | `PLANNED_DISABLED` |
| `WEB-011` | Accounts Payable and Supplier Payments | Finanzas | `UI ID pending` | `/cuentas-por-pagar` candidate | `PLANNED_DISABLED` |
| `WEB-012` | Cash Shift and Reconciliation | Finanzas | `UI ID pending` | `/caja` candidate | `PLANNED_DISABLED` |
| `WEB-013` | Fiscal Documents | Fiscal | `UI ID pending` | `/documentos-fiscales` candidate | `PLANNED_DISABLED` |
| `WEB-014` | CAE Administration | Fiscal | `UI ID pending` | `/cae` candidate | `PLANNED_DISABLED` |
| `WEB-015` | Contingency and Synchronization Supervision | Fiscal | `UI ID pending` | `/contingencia` candidate | `PLANNED_DISABLED` |
| `WEB-016` | Received CFE and XML Validation | Fiscal | `UI ID pending` | `/cfe-recibidos` candidate | `PLANNED_DISABLED` |
| `WEB-017` | Reports and Fiscal Calendar | Reportes | `UI ID pending` | `/reportes` candidate | `PLANNED_DISABLED` |
| `WEB-018` | Audit, Security and Configuration | Administración | `UI ID pending` | `/administracion` candidate | `PLANNED_DISABLED` |
| `WEB-019` | Technical Operations Console | Administración | `UI ID pending` | `/operaciones` candidate | `PLANNED_DISABLED` |

## 5. Visibility and activation rule

1. Every accepted shell-hosted `WEB-*` option appears in the complete product navigation from the beginning.
2. An unimplemented option is rendered disabled/non-clickable and does not receive a live route target.
3. Candidate/reserved routes in this document must never be used as fake links.
4. An option becomes `active` only after reconciliation, stable `UI-*` assignment, route confirmation, visual approval when required, executable capability evidence, React implementation and capability registration.
5. Only `active` entries are derived into `shellRoutes` and React Router.
6. Sidebar and mobile navigation consume the same grouped registry.
7. Disabled entries communicate roadmap structure only and do not imply backend/API readiness.

This preserves the no-dead-links rule while keeping the full application architecture visible.

## 6. Unified registry requirement

There must not be separate hand-maintained lists for active routes and Sidebar product options.

`routes.tsx` contains both active and planned navigation items. `shellRoutes` is derived only from entries whose state is `active`.

Conceptual metadata:

```text
webId
state: active | planned
label/capability
icon
navigationGroup
element (active only)
```

Feature components must not encode their own Sidebar placement.

## 7. Activation progression

When a planned view is implemented, the existing navigation item changes from `planned` to `active` only after its `WEB-*` scope, `UI-*` identifier, final route, executable capability evidence and React feature are governed and implemented.

Visual approval alone does not authorize route activation when the required backend/API capability is non-executable.

Runtime acceptance is recorded separately from route activation. An active route may still have visual/governance debt, and runtime acceptance must never be inferred merely from implementation.

`/pos` remains `defaultShellRoute` until a separate explicit product decision changes it.

## 8. Latest runtime acceptance checkpoint

`UI-INVENTORY-001` is the latest shell route to complete the governed WebApp runtime-review lane.

Accepted evidence includes:

- approved baseline: `UI-INVENTORY-001 v1-responsive-suite`;
- visual-governance PR: `#183`;
- implementation PR: `#184`;
- accepted deployed WebApp commit: `45a57dab16d27d6d455aeebef895c495968da63b`;
- `Deploy eFactura Demo #31`: `SUCCESS`;
- post-merge `Frontend Demo CI #72`: `SUCCESS`;
- post-merge `Clean Architecture Guard #638`: `SUCCESS`;
- desktop light/dark runtime review: accepted;
- explicit owner closure: `Apruebo cierre runtime UI-INVENTORY-001`;
- acceptance record: `documentation/ui/reviews/UI-INVENTORY-001/RUNTIME_VISUAL_ACCEPTANCE.md`;
- closure documentation merged through PR `#186`.

Separate deployed mobile runtime screenshots were not independently reviewed in that closure. Approved visual PNG binaries remain `BINARY_PRESERVATION_PENDING`, and the view is not promoted to repository-wide `ACCEPTED`.

## 9. Current next-view checkpoint

`WEB-010 / UI-RECEIVABLE-001` is the next governed WebApp view.

`WEB-008 / UI-TRANSFER-001` and `WEB-009 / UI-PROCUREMENT-001` already have approved `v1-responsive-suite` visual baselines, but executable implementation remains blocked because their required Wave 4 HTTP operations are still `MISSING_HTTP`.

The `WEB-010` target boundary is supported by `FR-023`, `FR-026`, `FR-027`, `FR-070`, `FR-072`, `FR-073`, `FR-074`, `UC-SALE-003` and `UC-AR-001`: expose receivables/aging, preserve allocation-derived balances, register partial/full collections, and handle overpayment/advance only through explicit backend policy.

The accepted API contract assigns:

- `API-AR-001` list receivables;
- `API-AR-002` receivable detail;
- `API-AR-003` authoritative aging projection;
- `API-AR-004` append receivable adjustment;
- `API-COL-001` create collection/allocation;
- `API-COL-002` collection detail;
- `API-COL-003` compensating collection reversal.

Wave 3 currently records all seven operations as `MISSING_HTTP` with no WebApi evidence. Therefore this checkpoint authorizes functional specification and visual drafting only.

The route `/cuentas-por-cobrar` remains `PLANNED_DISABLED`; no React route activation, fake live collection, local balance mutation, destructive history rewrite, silent overpayment truncation or invented advance policy is authorized until the receivables/collections API lane provides executable evidence and the UI is reconciled again.

Shell counts remain 6 active / 12 planned.

## 10. Change control

Changing product group assignment, route, standalone/shell classification, ordering, visibility rules or Sidebar/mobile grouping requires an explicit navigation-governance update.

## 11. Relationship to shell policy

- `WEBAPP_NAVIGATION_MAP.md`: complete product-navigation roadmap and visibility state;
- `WEBAPP_SHELL_POLICY.md`: reusable shell behavior and activation rules;
- `capabilities.ts`: implemented UI capability bindings only;
- `routes.tsx`: unified product-navigation registry plus derived executable `shellRoutes`.
