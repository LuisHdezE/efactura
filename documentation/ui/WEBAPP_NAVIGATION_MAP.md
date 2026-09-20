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
- `WEB-006 / UI-CATALOG-001` is visual-approved for reserved route `/catalogo`, but remains `PLANNED_DISABLED` pending implementation;
- `WEB-007..WEB-019` remain planned shell candidates with no executable route yet;
- all 18 shell-hosted product options remain visible in the Sidebar information architecture;
- 4 entries are interactive shell routes;
- 14 future entries remain visible as disabled/non-clickable options, not fake routes.

Current counts:

- Web scope total: **19**;
- standalone implemented: **1**;
- active shell routes: **4**;
- planned disabled shell options: **14**.

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
| `WEB-006` | Products and Services Catalog | Comercial | `UI-CATALOG-001` | `/catalogo` | `PLANNED_DISABLED` |
| `WEB-007` | Inventory and Movements | Inventario y Compras | `UI ID pending` | `/inventario` candidate | `PLANNED_DISABLED` |
| `WEB-008` | Stock Transfers | Inventario y Compras | `UI ID pending` | `/transferencias` candidate | `PLANNED_DISABLED` |
| `WEB-009` | Purchase Orders and Receipts | Inventario y Compras | `UI ID pending` | `/compras` candidate | `PLANNED_DISABLED` |
| `WEB-010` | Accounts Receivable and Collections | Finanzas | `UI ID pending` | `/cuentas-por-cobrar` candidate | `PLANNED_DISABLED` |
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
3. Candidate routes in this document must never be used as fake links.
4. An option becomes `active` only after reconciliation, stable `UI-*` assignment, route confirmation, visual approval when required, React implementation and capability registration.
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

When a planned view is implemented, the existing navigation item changes from `planned` to `active` after its `WEB-*` scope, `UI-*` identifier, final route, capability evidence and React feature are governed and implemented.

Runtime acceptance is recorded separately from route activation. An active route may still have visual/governance debt, and runtime acceptance must never be inferred merely from implementation.

`/pos` remains `defaultShellRoute` until a separate explicit product decision changes it.

## 8. Runtime acceptance checkpoint

`UI-SUPPLIER-001` is the latest shell route to complete the governed visual/runtime lane.

Accepted evidence:

- approved baseline: `UI-SUPPLIER-001 v1-theme-pair`;
- implementation PR: `#171`;
- runtime-polish PR: `#172`;
- accepted deployed WebApp commit: `525a5be0bee9458220d0e464f0d2df8aa935497b`;
- human runtime acceptance: `Apruebo cierre runtime UI-SUPPLIER-001` on 2026-09-19;
- acceptance record: `documentation/ui/reviews/UI-SUPPLIER-001/RUNTIME_VISUAL_ACCEPTANCE.md`.

This does not promote the view to final repository-wide `ACCEPTED`; its specification still records independent traceability and binary-preservation debt.

## 9. Current next-view checkpoint

`WEB-006` has now been reconciled as `UI-CATALOG-001` with reserved route `/catalogo`.

Its first governed scope is the executable commercial-item master supported by `API-CAT-001..009` and `API-REF-008`, without fabricating pricing, images/media or stock quantities absent from the current item contract.

`UI-CATALOG-001 v1-theme-pair` was explicitly approved by Luis on 2026-09-20. Its exact generated source is fingerprinted under `documentation/ui/references/approved/UI-CATALOG-001/v1-theme-pair/`, with byte-identical binary preservation still pending through the current repository connector.

The option remains `PLANNED_DISABLED` until the React implementation is completed, capability registration is added, applicable CI is green and merge is explicitly authorized.

## 10. Change control

Changing product group assignment, route, standalone/shell classification, ordering, visibility rules or Sidebar/mobile grouping requires an explicit navigation-governance update.

## 11. Relationship to shell policy

- `WEBAPP_NAVIGATION_MAP.md`: complete product-navigation roadmap and visibility state;
- `WEBAPP_SHELL_POLICY.md`: reusable shell behavior and activation rules;
- `capabilities.ts`: implemented UI capability bindings only;
- `routes.tsx`: unified product-navigation registry plus derived executable `shellRoutes`.
