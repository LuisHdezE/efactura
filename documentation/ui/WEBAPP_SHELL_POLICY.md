# eFactura WebApp Shell Policy

Status: `ACTIVE / GOVERNED`

## Purpose

Define one reusable application shell for every governed WebApp view so global navigation and chrome are not repainted or reimplemented per feature.

The complete product-navigation structure is governed by `documentation/ui/WEBAPP_NAVIGATION_MAP.md`.

## Canonical shell components

- `src/WebApp/src/layout/Topbar.tsx`
- `src/WebApp/src/layout/Sidebar.tsx`
- `src/WebApp/src/layout/BottomBar.tsx`
- `src/WebApp/src/layout/MobileNavigation.tsx`
- `src/WebApp/src/layout/AppShell.tsx`

Feature pages must not recreate their own global sidebar, topbar or bottom bar.

## Unified navigation policy

Product-navigation metadata and route binding are centralized in `src/WebApp/src/app/routes.tsx`.

The registry has two navigation states:

- `active`: a real React route exists and the Sidebar/mobile item is clickable;
- `planned`: the product option belongs in the governed information architecture but has no React route yet.

Backend execution readiness is separate from route availability. An `active` capability may be:

- `IMPLEMENTED_API_MOCK_DATA`: implemented UI backed by the existing mock/API gateway boundary;
- `IMPLEMENTED_VISUAL_PREVIEW`: implemented navigable visual preview using explicit local demonstration data while its authoritative HTTP API remains unavailable.

A visual-preview route is not API-complete. It must visibly identify its demo state and keep every server-owned mutation disabled.

A `planned` entry may also carry optional product-facing progress metadata:

- `governed` renders as `En diseño` after the `WEB-* -> UI-*` boundary has been reconciled and specified;
- `visual-approved` renders as `Diseño listo` after the exact governed visual baseline has been explicitly approved but no navigable preview has been implemented.

`Sidebar` and `MobileNavigation` consume the complete grouped registry. `shellRoutes` is derived only from `active` entries and drives React Router. There must never be a second manually-maintained Sidebar list.

## Product navigation groups

- `Inicio`;
- `Comercial`;
- `Inventario y Compras`;
- `Finanzas`;
- `Fiscal`;
- `Reportes`;
- `Administración`.

Every accepted shell-hosted `WEB-*` option is visible inside one of these groups from the beginning. Feature pages must not decide their own Sidebar section.

## Planned, preview and API-backed navigation

Runtime rules:

- no dead links;
- no candidate route is exposed merely because it exists in planning;
- planned modules are visible but disabled/non-clickable;
- planned modules may expose a compact progress badge when governance has advanced beyond roadmap-only state;
- an approved visual baseline may become a navigable visual preview only when the React page is actually implemented and clearly labelled as demo/preview;
- visual preview data must be explicit local demonstration data and must never be presented as server state;
- server-owned actions in a visual preview remain disabled until executable API evidence and permission integration exist;
- only `active` entries receive a route target and active-link behavior;
- only `active` entries are present in `shellRoutes`.

A disabled future module is a product-architecture cue. A visual-preview module is a real frontend route, but not proof of backend readiness.

## Capability binding

`src/WebApp/src/app/capabilities.ts` remains authoritative for implemented UI capability bindings.

An API/mock-integrated capability may list accepted operations. A visual-preview capability uses `IMPLEMENTED_VISUAL_PREVIEW` and must not fabricate executable operations while its HTTP layer is missing.

Planned navigation entries must not invent `UiCapability` identifiers, operations or backend contracts merely to appear in the Sidebar.

## New-view Definition of Done

For every planned WebApp view transitioning to an API-backed active route:

1. Reconcile the upstream `WEB-*` candidate and assign a governed `UI-*` identifier.
2. Confirm its Navigation Map group and final route.
3. Add/update its `UiCapability` only from accepted evidence.
4. Implement the feature page without global shell duplication.
5. Convert the existing navigation entry from `planned` to `active`, binding its capability and component.
6. Confirm it is derived into `shellRoutes`.
7. Confirm direct URL navigation and active-link state.
8. Confirm Sidebar and mobile navigation retain the same label/group.
9. Preserve light/dark shell behavior.
10. Run Frontend Demo CI and repository gates.
11. Deploy and perform runtime visual review.
12. Update Navigation Map progress accounting.

For a visual-preview transition, steps 1, 2, 4, 5, 6, 7, 8, 9, 10, 11 and 12 still apply, plus these constraints:

- the exact visual baseline must already be approved;
- capability status must be `IMPLEMENTED_VISUAL_PREVIEW`;
- preview/demo state must be visible in the page;
- server-dependent controls remain disabled;
- no missing API operation may be reported as executable.

## Shell visual-change rule

The shell is shared infrastructure. Changes to Sidebar, Topbar, BottomBar, mobile navigation, shell width, global spacing, brand treatment or theme behavior are shell-wide changes and must be reviewed across all implemented views.

## Sidebar density rule

The desktop Sidebar represents business-application information architecture, not a tile launcher.

Therefore:

- entries use compact horizontal rows;
- icons remain subordinate to labels and must not dominate row height;
- group headings remain visible and stable;
- planned entries use the same rhythm but reduced emphasis and no click behavior;
- active visual-preview routes use normal link behavior because a real page exists, while their page content carries the preview warning;
- the Sidebar may scroll independently when viewport height cannot contain the full map;
- planned modules must not inflate navigation into oversized cards.

## Standalone-route exception

Current explicit exception:

- `/acceso` (`UI-AUTH-001`) because it represents pre-session/session-entry presentation.

Standalone routes must be deliberate and documented.

## Current checkpoint

- total accepted Web interfaces: **19**;
- implemented standalone views: **1**;
- active shell routes: **8**;
- active visual-preview routes: **2** (`WEB-008`, `WEB-009`);
- planned disabled shell options: **10**;
- planned with governed/specification-ready boundary: **1** (`WEB-010`).

`/pos` remains the default shell route until a separate explicit product decision changes it.

## Non-negotiable rule

`Sidebar`, `Topbar` and `BottomBar` are reusable platform components, not per-screen artwork.

Future views extend the shell. Every accepted shell-hosted product option is visible in governed navigation. A route may be interactive only when a real React surface exists; backend-dependent behavior must remain truthful about its actual readiness.
