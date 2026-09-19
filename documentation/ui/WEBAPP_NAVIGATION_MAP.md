# eFactura WebApp Navigation Map

Status: `ACTIVE / GOVERNED PLANNING`

## 1. Purpose

Maintain one explicit product-level map of the complete WebApp navigation scope so implemented views, planned views and Sidebar visibility never drift apart.

This document is planning/governance. It does not create executable routes by itself.

Runtime navigation metadata is centralized in:

`src/WebApp/src/app/routes.tsx`

The registry distinguishes:

- `active`: a real executable route backed by an implemented feature;
- `planned`: a governed product option that is visible in navigation but deliberately non-interactive until implementation exists.

Planned options are not routes and must never produce 404/dead-link behavior.

## 2. Current navigation facts

The accepted web scope contains `WEB-001..WEB-018` plus additive `WEB-019 Technical Operations Console`.

Current runtime state after Dashboard implementation and runtime visual refinement:

- `WEB-001 / UI-AUTH-001` is a deliberate standalone route at `/acceso` and is not part of Sidebar navigation;
- `WEB-002 / UI-DASHBOARD-001` is implemented at `/dashboard`;
- `WEB-003 / UI-POS-001` is implemented at `/pos`;
- `WEB-004 / UI-CUSTOMER-001` is implemented at `/clientes`;
- `WEB-005..WEB-019` remain planned shell candidates with no executable route yet;
- all 18 shell-hosted product options are visible in the Sidebar information architecture;
- only the 3 implemented entries are interactive links;
- the 15 future entries are visible as disabled/non-clickable options marked by presentation state, not fake routes.

Therefore:

- total Web interfaces in scope: **19**;
- standalone implemented interfaces: **1**;
- shell-hosted/current-or-future interfaces: **18**;
- active shell routes: **3**;
- planned disabled shell options: **15**.

## 3. Sidebar information architecture

The complete Sidebar is organized into these stable product groups and labels.

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

These group names are product-navigation concepts. They do not alter backend bounded contexts or API ownership.

## 4. Governed navigation matrix

Legend:

- `ACTIVE`: executable route derived from the unified navigation registry and rendered as a link;
- `PLANNED_DISABLED`: visible product-navigation option with no executable route and no click behavior;
- `STANDALONE`: intentionally outside the application shell;
- `UI ID pending`: no stable `UI-*` identifier is assigned until reconciliation proves the view boundary without fabricating backend behavior;
- candidate routes are planning values only until the corresponding reconciliation/specification confirms them.

| WEB | Interface | Product group | Governed UI ID | Candidate / current route | Navigation state | Current UI state |
| --- | --- | --- | --- | --- | --- | --- |
| `WEB-001` | Login and Session Entry | Standalone | `UI-AUTH-001` | `/acceso` | `STANDALONE` | Implemented, visual debt deferred |
| `WEB-002` | Operational Dashboard | Inicio | `UI-DASHBOARD-001` | `/dashboard` | `ACTIVE` | Implemented, deployed, runtime refinement applied |
| `WEB-003` | POS Sale | Comercial | `UI-POS-001` | `/pos` | `ACTIVE` | Reviewed / runtime visual accepted |
| `WEB-004` | Customers and Parties | Comercial | `UI-CUSTOMER-001` | `/clientes` | `ACTIVE` | Reviewed / runtime visual accepted |
| `WEB-005` | Suppliers | Comercial | `UI ID pending` | `/proveedores` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-006` | Products and Services Catalog | Comercial | `UI ID pending` | `/catalogo` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-007` | Inventory and Movements | Inventario y Compras | `UI ID pending` | `/inventario` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-008` | Stock Transfers | Inventario y Compras | `UI ID pending` | `/transferencias` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-009` | Purchase Orders and Receipts | Inventario y Compras | `UI ID pending` | `/compras` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-010` | Accounts Receivable and Collections | Finanzas | `UI ID pending` | `/cuentas-por-cobrar` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-011` | Accounts Payable and Supplier Payments | Finanzas | `UI ID pending` | `/cuentas-por-pagar` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-012` | Cash Shift and Reconciliation | Finanzas | `UI ID pending` | `/caja` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-013` | Fiscal Documents | Fiscal | `UI ID pending` | `/documentos-fiscales` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-014` | CAE Administration | Fiscal | `UI ID pending` | `/cae` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-015` | Contingency and Synchronization Supervision | Fiscal | `UI ID pending` | `/contingencia` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-016` | Received CFE and XML Validation | Fiscal | `UI ID pending` | `/cfe-recibidos` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-017` | Reports and Fiscal Calendar | Reportes | `UI ID pending` | `/reportes` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-018` | Audit, Security and Configuration | Administración | `UI ID pending` | `/administracion` candidate | `PLANNED_DISABLED` | Unreconciled |
| `WEB-019` | Technical Operations Console | Administración | `UI ID pending` | `/operaciones` candidate | `PLANNED_DISABLED` | Unreconciled |

## 5. Visibility and activation rule

The Navigation Map answers **where every governed product option belongs**.

`src/WebApp/src/app/routes.tsx` owns the runtime registry and separates visibility from executability.

Rules:

1. Every shell-hosted `WEB-*` option appears in the complete product navigation from the beginning.
2. An unimplemented option is rendered disabled/non-clickable and does not receive a live `to` target.
3. Candidate routes in this document must never be used as fake links.
4. An option becomes `active` only after reconciliation, stable `UI-*` assignment, route confirmation, visual approval when required, React implementation and capability registration.
5. Only `active` entries are derived into `shellRoutes` and therefore into React Router.
6. Sidebar and mobile navigation consume the same grouped registry so labels/group placement cannot drift.
7. A disabled entry communicates roadmap structure only. It must not imply backend/API readiness.

This preserves the no-dead-links rule while keeping the full application architecture visible to the user.

## 6. Unified registry requirement

There must not be separate hand-maintained lists for active routes and Sidebar product options.

The canonical runtime registry in `routes.tsx` contains both active and planned navigation items. `shellRoutes` is derived only from entries whose state is `active`.

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

When a planned view is implemented:

- reconcile its `WEB-*` scope;
- assign/confirm its governed `UI-*` identifier;
- confirm the final route;
- bind accepted capability evidence;
- implement the React feature inside the shared shell;
- change the existing navigation item from `planned` to `active` rather than adding a duplicate item;
- verify direct URL, Sidebar, mobile navigation and active-state behavior.

`/pos` remains `defaultShellRoute` until a separate explicit product decision changes it.

## 8. Progress accounting rule

Every UI roadmap/checkpoint report must include:

1. total Web interfaces in accepted scope;
2. implemented standalone views;
3. active shell routes;
4. planned disabled shell options.

Current checkpoint:

```text
Web scope total: 19
Standalone implemented: 1
Active shell routes: 3
Planned disabled shell options: 15
```

## 9. Change control

Changing any of these requires an explicit navigation-governance update:

- product group assignment;
- candidate/final route;
- standalone vs shell-hosted classification;
- ordering policy;
- visibility/disabled-state rule;
- global Sidebar/mobile grouping behavior.

A feature PR may activate an already-governed entry, but it must not silently redesign the global information architecture.

## 10. Relationship to shell policy

This document complements `documentation/ui/WEBAPP_SHELL_POLICY.md`.

- `WEBAPP_NAVIGATION_MAP.md`: complete product-navigation roadmap and visibility state;
- `WEBAPP_SHELL_POLICY.md`: reusable shell behavior and activation rules;
- `capabilities.ts`: bindings for implemented UI capabilities only;
- `routes.tsx`: unified product-navigation registry plus derived executable `shellRoutes`.
