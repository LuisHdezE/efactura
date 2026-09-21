# eFactura WebApp Navigation Map

Status: `ACTIVE / GOVERNED PLANNING`

## 1. Purpose

Maintain one explicit product-level map of the complete WebApp navigation scope so implemented views, visual previews, planned views and Sidebar visibility never drift apart.

Runtime navigation metadata is centralized in `src/WebApp/src/app/routes.tsx`. `active` means a real React route exists; `planned` means the item is visible but disabled. Backend/API readiness is tracked separately so a navigable visual preview can exist without pretending that missing HTTP operations are executable.

## 2. Current navigation facts

The accepted web scope contains `WEB-001..WEB-018` plus additive `WEB-019 Technical Operations Console`.

Current runtime/governance state:

- `WEB-001 / UI-AUTH-001` is standalone at `/acceso`;
- `WEB-002 / UI-DASHBOARD-001` is active at `/dashboard`;
- `WEB-003 / UI-POS-001` is active at `/pos`;
- `WEB-004 / UI-CUSTOMER-001` is active at `/clientes`;
- `WEB-005 / UI-SUPPLIER-001` is active at `/proveedores` and its light/dark deployed runtime was explicitly accepted on 2026-09-19;
- `WEB-006 / UI-CATALOG-001` is active at `/catalogo` with explicit demo/mock behavior where integration is not yet authoritative;
- `WEB-007 / UI-INVENTORY-001` is active at `/inventario`; its desktop light/dark deployed runtime was explicitly accepted on 2026-09-20;
- `WEB-008 / UI-TRANSFER-001` has approved visual baseline `v1-responsive-suite` and is implemented at `/transferencias` as `IMPLEMENTED_VISUAL_PREVIEW`; server-owned transfer commands remain disabled because `API-TRF-001..007` remain `MISSING_HTTP`;
- `WEB-009 / UI-PROCUREMENT-001` has approved visual baseline `v1-responsive-suite` and is implemented at `/compras` as `IMPLEMENTED_VISUAL_PREVIEW`; purchase/receipt commands remain disabled because `API-PRC-001..006` and `API-GRC-001..004` remain `MISSING_HTTP`;
- `WEB-010 / UI-RECEIVABLE-001` has approved visual baseline `v1-approved-view` and is implemented at `/cuentas-por-cobrar` as `IMPLEMENTED_VISUAL_PREVIEW`; its deployed desktop runtime was visually accepted on 2026-09-21 and collection/adjustment/reversal controls remain disabled because `API-AR-001..004` and `API-COL-001..003` remain `MISSING_HTTP`;
- `WEB-011 / UI-PAYABLE-001` has approved visual baseline `v1-responsive-suite` and is implemented at `/cuentas-por-pagar` as `IMPLEMENTED_VISUAL_PREVIEW`; all supplier-payment mutations remain disabled because `API-AP-001..004` and `API-PAY-001..003` remain `MISSING_HTTP`;
- `WEB-012..WEB-019` remain roadmap-only planned shell candidates with no executable route yet;
- all 18 shell-hosted product options remain visible in the Sidebar information architecture.

Current counts:

- Web scope total: **19**;
- standalone implemented: **1**;
- active shell routes: **10**;
- active visual-preview routes: **4**;
- planned disabled shell options: **8**.

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

- `ACTIVE`: real React route derived from the unified registry and rendered as a link;
- `ACTIVE_VISUAL_PREVIEW`: active React route built from an approved visual baseline, using explicit local demo data while authoritative HTTP operations remain unavailable;
- `PLANNED_DISABLED`: visible product-navigation option with no React route and no click behavior;
- `STANDALONE`: intentionally outside the application shell;
- `UI ID pending`: no stable `UI-*` identifier is assigned yet.

| WEB | Interface | Product group | Governed UI ID | Current / candidate route | Navigation state | Runtime mode |
| --- | --- | --- | --- | --- | --- | --- |
| `WEB-001` | Login and Session Entry | Standalone | `UI-AUTH-001` | `/acceso` | `STANDALONE` | implemented |
| `WEB-002` | Operational Dashboard | Inicio | `UI-DASHBOARD-001` | `/dashboard` | `ACTIVE` | API/mock boundary |
| `WEB-003` | POS Sale | Comercial | `UI-POS-001` | `/pos` | `ACTIVE` | API/mock boundary |
| `WEB-004` | Customers and Parties | Comercial | `UI-CUSTOMER-001` | `/clientes` | `ACTIVE` | API/mock boundary |
| `WEB-005` | Suppliers | Comercial | `UI-SUPPLIER-001` | `/proveedores` | `ACTIVE` | API/mock boundary |
| `WEB-006` | Products and Services Catalog | Comercial | `UI-CATALOG-001` | `/catalogo` | `ACTIVE` | API/mock boundary |
| `WEB-007` | Inventory and Movements | Inventario y Compras | `UI-INVENTORY-001` | `/inventario` | `ACTIVE` | API/mock boundary |
| `WEB-008` | Stock Transfers | Inventario y Compras | `UI-TRANSFER-001` | `/transferencias` | `ACTIVE_VISUAL_PREVIEW` | local demo, API pending |
| `WEB-009` | Purchase Orders and Receipts | Inventario y Compras | `UI-PROCUREMENT-001` | `/compras` | `ACTIVE_VISUAL_PREVIEW` | local demo, API pending |
| `WEB-010` | Accounts Receivable and Collections | Finanzas | `UI-RECEIVABLE-001` | `/cuentas-por-cobrar` | `ACTIVE_VISUAL_PREVIEW` | local demo, API pending |
| `WEB-011` | Accounts Payable and Supplier Payments | Finanzas | `UI-PAYABLE-001` | `/cuentas-por-pagar` | `ACTIVE_VISUAL_PREVIEW` | local demo, API pending |
| `WEB-012` | Cash Shift and Reconciliation | Finanzas | `UI ID pending` | `/caja` candidate | `PLANNED_DISABLED` | — |
| `WEB-013` | Fiscal Documents | Fiscal | `UI ID pending` | `/documentos-fiscales` candidate | `PLANNED_DISABLED` | — |
| `WEB-014` | CAE Administration | Fiscal | `UI ID pending` | `/cae` candidate | `PLANNED_DISABLED` | — |
| `WEB-015` | Contingency and Synchronization Supervision | Fiscal | `UI ID pending` | `/contingencia` candidate | `PLANNED_DISABLED` | — |
| `WEB-016` | Received CFE and XML Validation | Fiscal | `UI ID pending` | `/cfe-recibidos` candidate | `PLANNED_DISABLED` | — |
| `WEB-017` | Reports and Fiscal Calendar | Reportes | `UI ID pending` | `/reportes` candidate | `PLANNED_DISABLED` | — |
| `WEB-018` | Audit, Security and Configuration | Administración | `UI ID pending` | `/administracion` candidate | `PLANNED_DISABLED` | — |
| `WEB-019` | Technical Operations Console | Administración | `UI ID pending` | `/operaciones` candidate | `PLANNED_DISABLED` | — |

## 5. Visibility and activation rule

1. Every accepted shell-hosted `WEB-*` option appears in the complete product navigation from the beginning.
2. An unimplemented option is rendered disabled/non-clickable and does not receive a route target.
3. Candidate/reserved routes must never become fake links.
4. A page with an approved visual baseline may become `ACTIVE_VISUAL_PREVIEW` once a real responsive React surface is implemented and visibly marked as preview/demo.
5. A visual preview must not claim backend readiness, must not fabricate server state and must keep server-owned mutations disabled while required HTTP operations are missing.
6. API-backed activation remains a separate integration gate.
7. Only `active` registry entries are derived into `shellRoutes` and React Router.
8. Sidebar and mobile navigation consume the same grouped registry.

## 6. Unified registry requirement

`routes.tsx` contains both active and planned navigation items. `shellRoutes` is derived only from entries whose state is `active`.

`capabilities.ts` distinguishes `IMPLEMENTED_API_MOCK_DATA` from `IMPLEMENTED_VISUAL_PREVIEW`.

## 7. Activation progression

A visual-preview route requires a reconciled `UI-*`, confirmed route, approved visual baseline, implemented responsive React surface, explicit demo state, disabled server-owned actions, successful repository gates and runtime visual review.

Promotion from `IMPLEMENTED_VISUAL_PREVIEW` to API-integrated behavior requires fresh executable API evidence, permission reconciliation and a separate integration review. Route availability never implies backend readiness.

`/pos` remains `defaultShellRoute` until a separate explicit product decision changes it.

## 8. Latest accepted runtime checkpoint

`UI-RECEIVABLE-001` at `/cuentas-por-cobrar` remains the latest shell route whose deployed visual runtime was explicitly accepted in the governed review lane on 2026-09-21.

`UI-PAYABLE-001` is implemented as a preview candidate but still requires post-deployment desktop/mobile runtime acceptance.

## 9. Current next-view checkpoint

Current implementation checkpoint: `WEB-011 / UI-PAYABLE-001 — Cuentas por pagar`.

Approved baseline: `v1-responsive-suite`, with separately reviewed desktop and mobile artifacts.

The accepted payables/supplier-payment contracts are `API-AP-001..004` and `API-PAY-001..003`, all currently `MISSING_HTTP`. The route is therefore limited to `ACTIVE_VISUAL_PREVIEW`: local demo inspection is allowed; supplier-payment posting, adjustment, reversal, authoritative balance mutation, silent payment truncation, invented unapplied-payment policy and cash/bank side effects remain prohibited.

After runtime acceptance of this preview, the next product view returns to `WEB-012 — Caja y conciliación`, which remains planned and has no governed `UI-*` baseline yet.

Shell counts are **10 active / 8 planned**, with **4 active visual previews**.

## 10. Change control

Changing product group assignment, route, standalone/shell classification, ordering, visibility rules, preview status, progress metadata or Sidebar/mobile grouping requires an explicit navigation-governance update.

## 11. Relationship to shell policy

- `WEBAPP_NAVIGATION_MAP.md`: complete product-navigation roadmap, visibility and runtime mode;
- `WEBAPP_SHELL_POLICY.md`: reusable shell behavior and preview/activation rules;
- `PREVIEW_ROUTE_POLICY_AMENDMENT.md`: explicit boundary for navigable visual previews without executable HTTP operations;
- `capabilities.ts`: implemented UI capability mode and route bindings;
- `routes.tsx`: unified product-navigation registry plus derived executable `shellRoutes`.
