# eFactura WebApp Shell Policy

Status: `ACTIVE / GOVERNED`

## Purpose

Define one reusable application shell for every governed WebApp view so global navigation and chrome are not repainted or reimplemented per feature.

This policy applies to all shell-hosted WebApp views from `WEB-002` onward and retroactively governs existing shell views such as Dashboard, POS and Customers.

The complete planned product-navigation structure is governed by:

`documentation/ui/WEBAPP_NAVIGATION_MAP.md`

## Canonical shell components

The global WebApp chrome is owned by reusable layout components:

- `src/WebApp/src/layout/Topbar.tsx`
- `src/WebApp/src/layout/Sidebar.tsx`
- `src/WebApp/src/layout/BottomBar.tsx`
- `src/WebApp/src/layout/MobileNavigation.tsx`
- `src/WebApp/src/layout/AppShell.tsx` as composition/orchestration only

Feature pages must not recreate their own global sidebar, topbar or bottom bar.

## Unified navigation policy

Product-navigation metadata and executable route binding are centralized in:

`src/WebApp/src/app/routes.tsx`

The runtime registry contains two states:

- `active`: the feature exists and is executable;
- `planned`: the product option belongs in the governed information architecture but has no executable route yet.

`Sidebar` and `MobileNavigation` consume the complete grouped registry.

`shellRoutes` is derived only from `active` entries and drives React Router.

This gives us one canonical navigation source while preserving the distinction between **visible product structure** and **implemented functionality**.

There must never be a second manually-maintained Sidebar list.

## Product navigation groups

The governed shell groups are:

- `Inicio`;
- `Comercial`;
- `Inventario y Compras`;
- `Finanzas`;
- `Fiscal`;
- `Reportes`;
- `Administración`.

Every accepted shell-hosted `WEB-*` option is visible inside one of these groups from the beginning.

Feature pages must not decide their own Sidebar section.

## Planned versus executable navigation

The complete roadmap remains visible without fabricating routes.

Runtime rules:

- no dead links;
- no candidate route is exposed merely because it exists in planning;
- planned modules are visible but disabled/non-clickable;
- planned modules must not pretend to have an implemented page or backend capability;
- only active entries receive a route target and active-link behavior;
- only active entries are present in `shellRoutes`.

A disabled future module is therefore a product-architecture cue, not a placeholder page.

## Capability binding

Business/API capability metadata remains governed by:

`src/WebApp/src/app/capabilities.ts`

Only implemented/active shell entries require a governed `UiCapability`.

Planned navigation entries must not invent `UiCapability` identifiers, operations or backend contracts merely to appear in the Sidebar.

The policy separates responsibilities:

- `WEBAPP_NAVIGATION_MAP.md`: complete product navigation roadmap and grouping;
- `capabilities.ts`: implemented UI-to-API capability contracts;
- `routes.tsx`: complete navigation registry plus derived executable routes;
- `AppShell.tsx`: layout composition;
- feature pages: view-specific content only.

## New-view Definition of Done

For every planned WebApp view transitioning to active:

1. Reconcile the upstream `WEB-*` candidate and assign a governed `UI-*` identifier.
2. Confirm or revise its Navigation Map group and final route.
3. Add/update its `UiCapability` contract only from accepted repository evidence.
4. Implement the feature page without global shell duplication.
5. Convert the existing navigation entry from `planned` to `active`, binding its capability and component.
6. Confirm it is derived into `shellRoutes`.
7. Confirm direct URL navigation and active-link state.
8. Confirm Sidebar and mobile navigation retain the same label/group.
9. Preserve light/dark shell behavior.
10. Run Frontend Demo CI and repository gates.
11. Deploy and perform runtime visual review.
12. Update Navigation Map progress accounting.

A feature that renders but is not reachable through governed shell navigation is incomplete.

## Shell visual-change rule

The shell is shared infrastructure. Changes to Sidebar, Topbar, BottomBar, mobile navigation, global spacing, shell width, brand treatment or theme behavior are shell-wide changes and must be reviewed for impact across all implemented views.

Feature-specific visual work belongs inside the feature content area beneath `AppShell`.

## Sidebar density rule

The desktop Sidebar represents a business application information architecture, not a tile launcher.

Therefore:

- navigation entries use compact horizontal rows;
- icons remain subordinate to labels and must not dominate row height;
- group headings remain visible and stable;
- planned entries use the same spatial rhythm but reduced emphasis and no click behavior;
- the Sidebar may scroll independently when viewport height cannot contain the full map;
- adding planned modules must not inflate each row into oversized cards.

## Standalone-route exception

A route may live outside `AppShell` only when its product boundary requires a standalone experience.

Current explicit exception:

- `/acceso` (`UI-AUTH-001`) because it represents pre-session/session-entry presentation.

Standalone routes must be deliberate and documented. They do not create an alternative application shell.

## Current checkpoint

The current authoritative counts live in `WEBAPP_NAVIGATION_MAP.md`.

At the Dashboard checkpoint:

- total accepted Web interfaces: **19**;
- implemented standalone views: **1**;
- active shell routes: **3**;
- planned disabled shell options: **15**.

`/pos` remains the default shell route until a separate explicit product decision changes it.

## Non-negotiable rule

`Sidebar`, `Topbar` and `BottomBar` are reusable platform components, not per-screen artwork.

Future views extend the shell. They do not repaint it.

Every accepted shell-hosted product option is visible in governed navigation, but only implemented options are interactive.
