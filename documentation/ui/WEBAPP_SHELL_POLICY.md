# eFactura WebApp Shell Policy

Status: `ACTIVE / GOVERNED`

## Purpose

Define one reusable application shell for every governed WebApp view so global navigation and chrome are not repainted or reimplemented per feature.

The complete planned product-navigation structure is governed by `documentation/ui/WEBAPP_NAVIGATION_MAP.md`.

## Canonical shell components

- `src/WebApp/src/layout/Topbar.tsx`
- `src/WebApp/src/layout/Sidebar.tsx`
- `src/WebApp/src/layout/BottomBar.tsx`
- `src/WebApp/src/layout/MobileNavigation.tsx`
- `src/WebApp/src/layout/AppShell.tsx`

Feature pages must not recreate their own global sidebar, topbar or bottom bar.

## Unified navigation policy

Product-navigation metadata and executable route binding are centralized in `src/WebApp/src/app/routes.tsx`.

The registry has two execution states:

- `active`: the feature exists and is executable;
- `planned`: the product option belongs in the governed information architecture but has no executable route yet.

A `planned` entry may also carry optional product-facing progress metadata:

- `governed` renders as `En diseño` after the `WEB-* -> UI-*` boundary has been reconciled and specified;
- `visual-approved` renders as `Diseño listo` after the exact governed visual baseline has been explicitly approved.

Progress metadata is informational only. It never changes an item's execution state, never creates a route target and never proves backend/API readiness.

`Sidebar` and `MobileNavigation` consume the complete grouped registry. `shellRoutes` is derived only from `active` entries and drives React Router.

There must never be a second manually-maintained Sidebar list.

## Product navigation groups

- `Inicio`;
- `Comercial`;
- `Inventario y Compras`;
- `Finanzas`;
- `Fiscal`;
- `Reportes`;
- `Administración`.

Every accepted shell-hosted `WEB-*` option is visible inside one of these groups from the beginning. Feature pages must not decide their own Sidebar section.

## Planned versus executable navigation

Runtime rules:

- no dead links;
- no candidate route is exposed merely because it exists in planning;
- planned modules are visible but disabled/non-clickable;
- planned modules may expose a compact progress badge when governance has advanced beyond roadmap-only state;
- a progress badge must not imply that the module is executable or that its backend exists;
- planned modules must not pretend to have an implemented page or backend capability;
- only active entries receive a route target and active-link behavior;
- only active entries are present in `shellRoutes`.

A disabled future module is a product-architecture cue, not a placeholder page.

## Capability binding

`src/WebApp/src/app/capabilities.ts` remains authoritative for implemented UI-to-API capability bindings.

Only implemented/active shell entries require a governed `UiCapability`. Planned navigation entries must not invent `UiCapability` identifiers, operations or backend contracts merely to appear in the Sidebar.

Progress metadata on a planned item is not a capability binding.

## New-view Definition of Done

For every planned WebApp view transitioning to active:

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

Planned progress changes (`roadmap -> governed -> visual-approved`) may occur before executable activation and must keep the entry non-clickable throughout.

## Shell visual-change rule

The shell is shared infrastructure. Changes to Sidebar, Topbar, BottomBar, mobile navigation, shell width, global spacing, brand treatment or theme behavior are shell-wide changes and must be reviewed across all implemented views.

## Sidebar density rule

The desktop Sidebar represents business-application information architecture, not a tile launcher.

Therefore:

- entries use compact horizontal rows;
- icons remain subordinate to labels and must not dominate row height;
- group headings remain visible and stable;
- planned entries use the same rhythm but reduced emphasis and no click behavior;
- advanced planned entries may use a compact progress badge without becoming visually equivalent to active routes;
- the Sidebar may scroll independently when viewport height cannot contain the full map;
- planned modules must not inflate navigation into oversized cards.

## Standalone-route exception

Current explicit exception:

- `/acceso` (`UI-AUTH-001`) because it represents pre-session/session-entry presentation.

Standalone routes must be deliberate and documented.

## Current checkpoint

- total accepted Web interfaces: **19**;
- implemented standalone views: **1**;
- active shell routes: **6**;
- planned disabled shell options: **12**;
- planned with approved visual baseline: **2** (`WEB-008`, `WEB-009`);
- planned with governed/specification-ready boundary: **1** (`WEB-010`).

`/pos` remains the default shell route until a separate explicit product decision changes it.

## Non-negotiable rule

`Sidebar`, `Topbar` and `BottomBar` are reusable platform components, not per-screen artwork.

Future views extend the shell. Every accepted shell-hosted product option is visible in governed navigation, but only implemented options are interactive.
