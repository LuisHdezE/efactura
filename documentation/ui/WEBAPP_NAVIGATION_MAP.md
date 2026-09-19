# eFactura WebApp Navigation Map

Status: `ACTIVE / GOVERNED PLANNING`

Baseline branch point: `main@bcefca379158d951e59ad6cf1ee69247d92df010`

## 1. Purpose

Maintain one explicit product-level map of the complete WebApp navigation scope so implemented views, future views and Sidebar visibility never drift apart.

This document is planning/governance. It does not create executable routes by itself.

Executable navigation remains owned by:

`src/WebApp/src/app/routes.tsx`

Only entries registered in `shellRoutes` are allowed to appear as live Sidebar/mobile links.

## 2. Current navigation facts

The accepted web scope contains `WEB-001..WEB-018` plus the additive `WEB-019 Technical Operations Console`.

Current runtime state before Dashboard implementation:

- `WEB-001 / UI-AUTH-001` is a deliberate standalone route at `/acceso` and is not part of Sidebar navigation;
- `WEB-003 / UI-POS-001` is implemented at `/pos` and appears in Sidebar/mobile navigation;
- `WEB-004 / UI-CUSTOMER-001` is implemented at `/clientes` and appears in Sidebar/mobile navigation;
- `WEB-002 / UI-DASHBOARD-001` has an explicitly approved visual baseline whose governance record is pending merge in PR `#160`; React implementation has not started;
- `WEB-005..WEB-019` remain future navigation candidates and must stay hidden until each view is reconciled, governed and implemented.

Therefore:

- total Web interfaces in scope: **19**;
- standalone pre-session interface: **1**;
- shell-hosted/current-or-future interfaces: **18**;
- shell routes currently executable: **2**;
- next shell route planned: **Dashboard**;
- shell candidates remaining after Dashboard: **15**.

## 3. Sidebar information architecture

The target Sidebar information architecture is organized into stable product groups.

### Inicio

Operational entry points and cross-domain summary.

### Comercial

Selling, customer/supplier master data and commercial catalog.

### Inventario y Compras

Stock, transfers, procurement and receiving.

### Finanzas

Receivables, payables and cash-shift operations.

### Fiscal

Fiscal documents, CAE, contingency/synchronization and received CFE validation.

### Reportes

Structured reporting and fiscal calendar.

### Administración

Audit/security/configuration and technical operations/observability.

These group names are product-navigation concepts. They do not alter backend bounded contexts or API ownership.

## 4. Governed navigation matrix

Legend:

- `ACTIVE`: executable route registered in `shellRoutes` and visible in Sidebar/mobile navigation;
- `NEXT`: approved/planned next feature, still hidden until executable;
- `HIDDEN`: future feature, never rendered as a dead link;
- `STANDALONE`: intentionally outside the application shell;
- `UI ID pending`: no stable `UI-*` identifier is assigned until reconciliation proves the view boundary without fabricating backend behavior;
- candidate routes are planning values only until the corresponding reconciliation/specification confirms them.

| WEB | Interface | Product group | Governed UI ID | Candidate / current route | Navigation state | Current UI state |
| --- | --- | --- | --- | --- | --- | --- |
| `WEB-001` | Login and Session Entry | Standalone | `UI-AUTH-001` | `/acceso` | `STANDALONE` | Implemented, visual debt deferred |
| `WEB-002` | Operational Dashboard | Inicio | `UI-DASHBOARD-001` | `/dashboard` | `NEXT` | Visual approved, governance PR `#160` open; implementation pending |
| `WEB-003` | POS Sale | Comercial | `UI-POS-001` | `/pos` | `ACTIVE` | Reviewed / runtime visual accepted |
| `WEB-004` | Customers and Parties | Comercial | `UI-CUSTOMER-001` | `/clientes` | `ACTIVE` | Reviewed / runtime visual accepted |
| `WEB-005` | Suppliers | Comercial | `UI ID pending` | `/proveedores` candidate | `HIDDEN` | Unreconciled |
| `WEB-006` | Products and Services Catalog | Comercial | `UI ID pending` | `/catalogo` candidate | `HIDDEN` | Unreconciled |
| `WEB-007` | Inventory and Movements | Inventario y Compras | `UI ID pending` | `/inventario` candidate | `HIDDEN` | Unreconciled |
| `WEB-008` | Stock Transfers | Inventario y Compras | `UI ID pending` | `/transferencias` candidate | `HIDDEN` | Unreconciled |
| `WEB-009` | Purchase Orders and Receipts | Inventario y Compras | `UI ID pending` | `/compras` candidate | `HIDDEN` | Unreconciled |
| `WEB-010` | Accounts Receivable and Collections | Finanzas | `UI ID pending` | `/cuentas-por-cobrar` candidate | `HIDDEN` | Unreconciled |
| `WEB-011` | Accounts Payable and Supplier Payments | Finanzas | `UI ID pending` | `/cuentas-por-pagar` candidate | `HIDDEN` | Unreconciled |
| `WEB-012` | Cash Shift and Reconciliation | Finanzas | `UI ID pending` | `/caja` candidate | `HIDDEN` | Unreconciled |
| `WEB-013` | Fiscal Documents | Fiscal | `UI ID pending` | `/documentos-fiscales` candidate | `HIDDEN` | Unreconciled |
| `WEB-014` | CAE Administration | Fiscal | `UI ID pending` | `/cae` candidate | `HIDDEN` | Unreconciled |
| `WEB-015` | Contingency and Synchronization Supervision | Fiscal | `UI ID pending` | `/contingencia` candidate | `HIDDEN` | Unreconciled |
| `WEB-016` | Received CFE and XML Validation | Fiscal | `UI ID pending` | `/cfe-recibidos` candidate | `HIDDEN` | Unreconciled |
| `WEB-017` | Reports and Fiscal Calendar | Reportes | `UI ID pending` | `/reportes` candidate | `HIDDEN` | Unreconciled |
| `WEB-018` | Audit, Security and Configuration | Administración | `UI ID pending` | `/administracion` candidate | `HIDDEN` | Unreconciled |
| `WEB-019` | Technical Operations Console | Administración | `UI ID pending` | `/operaciones` candidate | `HIDDEN` | Unreconciled |

## 5. Visibility rule

The Navigation Map answers **where a view belongs**.

`src/WebApp/src/app/routes.tsx` answers **whether the view exists now**.

A future entry from this document must not be shown disabled, as a placeholder or as a dead link merely because it exists in the product roadmap.

A view becomes visible only when all of the following are true:

1. its upstream `WEB-*` scope has been reconciled;
2. a stable governed `UI-*` identifier exists;
3. its route has been confirmed by the view specification;
4. its visual baseline has been explicitly approved when visual approval is required;
5. the React feature is implemented inside the shared shell;
6. its `UiCapability` is registered from accepted evidence;
7. its route is registered in `shellRoutes`;
8. direct URL, Sidebar and mobile navigation behavior are verified.

Registration in `shellRoutes` is the activation event. No second manual Sidebar list is allowed.

## 6. Group metadata requirement

Before the third shell-hosted feature is implemented, executable route metadata must be extended so each `ShellRouteDefinition` can identify its navigation group.

The Sidebar and `MobileNavigation` must consume the same grouping metadata. Feature components must not encode their own section placement.

Expected conceptual route metadata:

```text
uiId
route
label
icon
navigationGroup
element
```

The exact TypeScript shape is implementation detail and belongs in the feature/refactor PR, not in this planning document.

## 7. Dashboard activation

`UI-DASHBOARD-001` is the next route to activate.

Its implementation must:

- remain inside the reusable `AppShell`;
- register `/dashboard` in `shellRoutes`;
- classify the route in `Inicio`;
- cause Dashboard to appear automatically in Sidebar and mobile navigation;
- preserve `/pos` as `defaultShellRoute` until a separate explicit post-runtime product approval changes the default;
- use explicit mock/demo Dashboard data while the contracted Dashboard endpoints remain non-executable.

After activation the shell navigation count becomes **3 active routes of 18 shell-hosted/current-or-future interfaces**.

## 8. Progress accounting rule

Every UI roadmap/checkpoint report must include four numbers:

1. total Web interfaces in accepted scope;
2. implemented standalone views;
3. active shell routes;
4. remaining shell candidates.

Current checkpoint:

```text
Web scope total: 19
Standalone implemented: 1
Active shell routes: 2
Next shell route: Dashboard
Remaining shell candidates after Dashboard: 15
```

This prevents the UI roadmap from becoming disconnected from the Sidebar and makes progress visible at a glance.

## 9. Change control

Changing any of these requires an explicit navigation-governance update:

- product group assignment;
- candidate/final route;
- standalone vs shell-hosted classification;
- ordering policy;
- visibility rule;
- global Sidebar/mobile grouping behavior.

A feature PR may activate an already-governed entry, but it must not silently redesign the global information architecture.

## 10. Relationship to shell policy

This document complements `documentation/ui/WEBAPP_SHELL_POLICY.md`.

- `WEBAPP_NAVIGATION_MAP.md` is the complete product-navigation roadmap;
- `WEBAPP_SHELL_POLICY.md` defines reusable shell behavior and activation rules;
- `capabilities.ts` binds implemented UI capabilities to accepted API evidence;
- `routes.tsx` is the executable source of truth for live shell navigation.

The roadmap may contain hidden future entries. Runtime navigation may contain only executable entries.
