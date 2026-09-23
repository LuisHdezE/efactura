# eFactura WebApp Navigation Map

Status: `ACTIVE / GOVERNED PLANNING`

## 1. Purpose

Maintain one explicit product-level map of the complete WebApp navigation scope so implemented views, visual previews, planned views and Sidebar visibility never drift apart.

Runtime navigation metadata is centralized in `src/WebApp/src/app/routes.tsx`. `active` means a real React route exists; `planned` means the item is visible but disabled. Backend/API readiness is tracked separately so a navigable UI may exist without pretending that a still-disabled frontend integration is executable.

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
- `WEB-008 / UI-TRANSFER-001` has approved visual baseline `v1-responsive-suite` and is implemented at `/transferencias` as `IMPLEMENTED_VISUAL_PREVIEW`;
- `WEB-009 / UI-PROCUREMENT-001` has approved visual baseline `v1-responsive-suite` and is implemented at `/compras` as `IMPLEMENTED_VISUAL_PREVIEW`;
- `WEB-010 / UI-RECEIVABLE-001` has approved visual baseline `v1-approved-view` and is implemented at `/cuentas-por-cobrar` as `IMPLEMENTED_VISUAL_PREVIEW`;
- `WEB-011 / UI-PAYABLE-001` has approved visual baseline `v1-responsive-suite` and is implemented at `/cuentas-por-pagar` as `IMPLEMENTED_VISUAL_PREVIEW`;
- `WEB-012 / UI-CASH-001` has approved visual baseline `v1-responsive-composite` and is implemented at `/caja` as `IMPLEMENTED_VISUAL_PREVIEW`;
- `WEB-013 / UI-FIS-001` has approved visual baseline `v1-responsive-composite` and is implemented at `/documentos-fiscales` as `IMPLEMENTED_VISUAL_PREVIEW`; deployed desktop light/dark and 390×844 mobile runtime were visually accepted on 2026-09-22;
- `WEB-014 / UI-CAE-001` has approved visual baseline `v1-responsive-composite` (`4:3`, gen_id `ea4d59d5-0a58-451f-bd34-9af9b939819b`) and is implemented at `/cae` as `IMPLEMENTED_API_MOCK_DATA`; its deployed desktop light/dark and mobile 390×844 runtime were visually accepted on 2026-09-23; `API-CAE-001..007` remain mapped while server-owned mutations stay blocked until authoritative WebApp session/permission and HTTP integration are enabled;
- `WEB-015 / UI-CON-001` has approved visual baseline `v1-responsive-composite` (`4:3`, gen_id `4f3edc3e-cb6e-45f2-b0aa-ecad5854d76f`) and is implemented at `/contingencia` as `IMPLEMENTED_VISUAL_PREVIEW`; `operations: []` is mandatory while required `API-CNT-001..007` and `API-SYN-001..003` remain `MISSING_HTTP`, and all server-owned actions stay blocked;
- `WEB-016..WEB-019` remain roadmap-only planned shell candidates with no executable route yet;
- all 18 shell-hosted product options remain visible in the Sidebar information architecture.

Current counts:

- Web scope total: **19**;
- standalone implemented: **1**;
- active shell routes: **14**;
- active visual-preview routes: **7**;
- planned disabled shell options: **4**.

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
- Administración de CAE
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
- `ACTIVE_VISUAL_PREVIEW`: active React route built from an approved visual baseline, using explicit local demo data while required authoritative HTTP operations remain unavailable;
- `PLANNED_DISABLED`: visible product-navigation option with no React route and no click behavior;
- `STANDALONE`: intentionally outside the application shell.

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
| `WEB-012` | Cash Shift and Reconciliation | Finanzas | `UI-CASH-001` | `/caja` | `ACTIVE_VISUAL_PREVIEW` | local demo, API pending |
| `WEB-013` | Fiscal Documents | Fiscal | `UI-FIS-001` | `/documentos-fiscales` | `ACTIVE_VISUAL_PREVIEW` | local demo, API pending |
| `WEB-014` | CAE Administration | Fiscal | `UI-CAE-001` | `/cae` | `ACTIVE` | API contract mapped / mock gateway |
| `WEB-015` | Contingency and Synchronization Supervision | Fiscal | `UI-CON-001` | `/contingencia` | `ACTIVE_VISUAL_PREVIEW` | local fixtures, `operations: []`, API pending |
| `WEB-016` | Received CFE and XML Validation | Fiscal | `UI ID pending` | `/cfe-recibidos` candidate | `PLANNED_DISABLED` | — |
| `WEB-017` | Reports and Fiscal Calendar | Reportes | `UI ID pending` | `/reportes` candidate | `PLANNED_DISABLED` | — |
| `WEB-018` | Audit, Security and Configuration | Administración | `UI ID pending` | `/administracion` candidate | `PLANNED_DISABLED` | — |
| `WEB-019` | Technical Operations Console | Administración | `UI ID pending` | `/operaciones` candidate | `PLANNED_DISABLED` | — |

## 5. Visibility and activation rule

1. Every accepted shell-hosted `WEB-*` option appears in complete product navigation.
2. An unimplemented option is disabled/non-clickable and does not receive a route target.
3. Candidate/reserved routes must never become fake links.
4. A page with an approved visual baseline may become active once a real responsive React surface exists and its data/operation boundary is explicit.
5. Mock-backed activation must not claim live backend state or live permissions.
6. Server-owned mutations remain blocked until the WebApp has authoritative session/permission context and governed HTTP integration.
7. Only `active` registry entries are derived into `shellRoutes` and React Router.
8. Sidebar and mobile navigation consume the same grouped registry.

## 6. Unified registry requirement

`routes.tsx` contains both active and planned navigation items. `shellRoutes` is derived only from entries whose state is `active`.

`capabilities.ts` distinguishes `IMPLEMENTED_API_MOCK_DATA` from `IMPLEMENTED_VISUAL_PREVIEW` and records the accepted operation set for API-mapped views.

## 7. CAE activation boundary

`UI-CAE-001` deliberately activates in `IMPLEMENTED_API_MOCK_DATA`, not as fake live API integration.

The backend operation set `API-CAE-001..007` is implemented and mapped in `capabilities.ts`, but the current WebApp service layer still fails closed when `VITE_DATA_MODE=api`; the hardcoded shell user is not authoritative permission context.

Therefore `/cae` may:

- render the approved responsive composition;
- load deterministic CAE and allocation fixtures through `gateways.cae`;
- demonstrate local search/filter/selection;
- preserve contract-backed field names and status concepts;
- show mutation placement from the approved baseline.

It must not yet:

- import a CAE;
- activate a CAE;
- create an allocation;
- close an allocation;
- claim `fiscal.manage_cae` from the demo shell identity;
- issue direct `fetch`/Axios calls that bypass the governed service layer;
- invent `NextNumber`, consumed percentage or remaining-number counts.

## 8. WEB-015 visual-preview boundary

`UI-CON-001` is active at `/contingencia` exclusively as `IMPLEMENTED_VISUAL_PREVIEW`.

WEB-015 maps conceptually to `API-CNT-001..007` plus `API-SYN-001..003`, but the accepted completion matrices currently classify all ten as `MISSING_HTTP`. For that reason the capability deliberately registers `operations: []`.

The implementation may:

- render deterministic local fixtures for supervision, status, queue/history and detail;
- distinguish `Cliente / API` from `DGI / Proveedor`;
- perform local search/filter/selection only;
- show all canonical sync result labels as text;
- transform the dense desktop table into cards on mobile;
- show disabled server-owned action placement.

It must not:

- claim live DGI/provider status;
- claim live client/API server status beyond clearly marked demo data;
- enter or exit contingency;
- register or reconcile CFC documents;
- submit synchronization batches;
- execute review/recovery mutations;
- imply server permission authority from the demo shell identity.

Generated copy in the approved visual remains subordinate to these contracts.

## 9. Latest accepted runtime checkpoint

`UI-CAE-001` at `/cae` remains the latest shell route whose deployed runtime was explicitly accepted in the governed review lane on 2026-09-23.

Accepted evidence covers desktop light, desktop dark and mobile 390×844. That runtime closure does not change its API/session boundary.

WEB-015 implementation does not alter this checkpoint until its own deployment and separate runtime visual acceptance are completed.

## 10. Current next-view checkpoint

Current implementation checkpoint: `WEB-015 / UI-CON-001 — Contingency and Synchronization Supervision`.

Approved baseline: `v1-responsive-composite`, aspect ratio `4:3`, artifact `4f3edc3e-cb6e-45f2-b0aa-ecad5854d76f`.

Implementation mode: `IMPLEMENTED_VISUAL_PREVIEW` with local deterministic fixtures, `operations: []`, dedicated QA guard and runtime acceptance still pending.
