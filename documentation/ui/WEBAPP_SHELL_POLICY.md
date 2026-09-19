# eFactura WebApp Shell Policy

Status: `ACTIVE / GOVERNED`

## Purpose

Define one reusable application shell for every governed WebApp view so global navigation and chrome are not repainted or reimplemented per feature.

This policy applies to all shell-hosted WebApp views from `WEB-002` onward and retroactively governs existing shell views such as POS and Customers.

The complete planned product-navigation structure is governed by:

`documentation/ui/WEBAPP_NAVIGATION_MAP.md`

The Navigation Map defines where current and future views belong. `shellRoutes` defines which of those views exist now.

## Canonical shell components

The global WebApp chrome is owned by reusable layout components:

- `src/WebApp/src/layout/Topbar.tsx`
- `src/WebApp/src/layout/Sidebar.tsx`
- `src/WebApp/src/layout/BottomBar.tsx`
- `src/WebApp/src/layout/MobileNavigation.tsx`
- `src/WebApp/src/layout/AppShell.tsx` as composition/orchestration only

Feature pages must not recreate their own global sidebar, topbar or bottom bar.

## Single navigation policy

Executable shell navigation and route binding are centralized in:

`src/WebApp/src/app/routes.tsx`

A shell-hosted feature is not considered implemented until it is registered there.

The same `shellRoutes` registry drives:

1. React Router route creation;
2. desktop Sidebar links;
3. mobile navigation links;
4. active-route highlighting;
5. feature icon/label presentation;
6. navigation-group placement once grouping metadata is activated.

This prevents a route from being added to the application while being forgotten in the Sidebar.

There must never be a second manually-maintained list of Sidebar links.

## Product navigation groups

The governed navigation map organizes shell-hosted views into these product groups:

- `Inicio`;
- `Comercial`;
- `Inventario y Compras`;
- `Finanzas`;
- `Fiscal`;
- `Reportes`;
- `Administración`.

A future view may be planned in one of these groups while remaining hidden from runtime navigation.

Before the third shell route is implemented, route metadata must support a navigation-group field consumed by both `Sidebar` and `MobileNavigation`.

Feature pages must not decide their own Sidebar section.

## Planned versus executable navigation

`WEBAPP_NAVIGATION_MAP.md` may contain the complete future information architecture, including candidate routes that are not yet executable.

Runtime navigation is stricter:

- no dead links;
- no placeholder links merely to expose the roadmap;
- no disabled future modules pretending to be implemented;
- no route becomes visible until it is registered in `shellRoutes`.

The activation event for a shell-hosted view is its governed registration in `shellRoutes` after reconciliation, specification, visual approval when required, and React implementation.

## Capability binding

Business/API capability metadata remains governed by:

`src/WebApp/src/app/capabilities.ts`

Every shell route must reference an existing governed `UiCapability` by `uiId`. `routes.tsx` fails fast when a requested capability registration is missing.

The policy therefore separates responsibilities:

- `WEBAPP_NAVIGATION_MAP.md`: complete product navigation roadmap and grouping;
- `capabilities.ts`: UI-to-API capability contract and permissions;
- `routes.tsx`: executable feature component binding and live shell navigation;
- `AppShell.tsx`: layout composition;
- feature pages: view-specific content only.

## New-view Definition of Done

For every new governed WebApp view:

1. Reconcile the upstream `WEB-*` candidate and assign a governed `UI-*` identifier.
2. Confirm or revise its planned Navigation Map group and route.
3. Add/update its `UiCapability` contract only from accepted repository evidence.
4. Implement the feature page without global shell duplication.
5. Register the feature in `shellRoutes` with its component, icon and navigation group.
6. Confirm desktop Sidebar and mobile navigation expose the same route under the same group.
7. Confirm direct URL navigation and active-link state.
8. Preserve light/dark shell behavior.
9. Run Frontend Demo CI and repository gates.
10. Deploy and perform runtime visual review.
11. Update Navigation Map progress accounting when the route becomes active.

A feature that renders but is not reachable through governed shell navigation is incomplete.

## Shell visual-change rule

The shell is shared infrastructure. A feature PR must not casually repaint global navigation.

Changes to Sidebar, Topbar, BottomBar, mobile navigation, global spacing, shell width, brand treatment or shell theme behavior must be treated as shell-wide changes and reviewed for impact across all implemented views.

Feature-specific visual work belongs inside the feature content area beneath `AppShell`.

## Standalone-route exception

A route may live outside `AppShell` only when its product boundary requires a standalone experience.

Current explicit exception:

- `/acceso` (`UI-AUTH-001`) because it represents pre-session/session-entry presentation.

Standalone routes must be deliberate and documented. They do not create an alternative application shell.

## Dashboard progression

`WEB-002 — Operational Dashboard` is the next governed shell feature.

Once `UI-DASHBOARD-001` is reconciled, visually approved and implemented:

- it must be registered in `shellRoutes`;
- it must be classified in the `Inicio` navigation group;
- it must appear in Sidebar and mobile navigation automatically from that registry;
- after explicit product approval it may replace `/pos` as `defaultShellRoute`.

The Dashboard implementation is also the point at which executable route metadata must gain group support, because it becomes the third live shell route.

## Progress visibility rule

UI progress reporting must remain tied to navigation reality.

Every roadmap/checkpoint should report:

- total accepted Web interfaces;
- implemented standalone views;
- active shell routes;
- next shell route;
- remaining shell candidates.

The current authoritative counts and full matrix live in `WEBAPP_NAVIGATION_MAP.md`.

## Non-negotiable rule

`Sidebar`, `Topbar` and `BottomBar` are reusable platform components, not per-screen artwork.

Future views extend the shell. They do not repaint it.

A future view may exist in the roadmap without appearing in the Sidebar. A live view may not exist in the shell without appearing in governed navigation.
