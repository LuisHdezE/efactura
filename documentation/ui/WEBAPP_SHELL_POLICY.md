# eFactura WebApp Shell Policy

Status: `ACTIVE / GOVERNED`

## Purpose

Define one reusable application shell for every governed WebApp view so global navigation and chrome are not repainted or reimplemented per feature.

This policy applies to all shell-hosted WebApp views from `WEB-002` onward and retroactively governs existing shell views such as POS and Customers.

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
5. feature icon/label presentation.

This prevents a route from being added to the application while being forgotten in the Sidebar.

## Capability binding

Business/API capability metadata remains governed by:

`src/WebApp/src/app/capabilities.ts`

Every shell route must reference an existing governed `UiCapability` by `uiId`. `routes.tsx` fails fast when a requested capability registration is missing.

The policy therefore separates responsibilities:

- `capabilities.ts`: UI-to-API capability contract and permissions;
- `routes.tsx`: executable feature component binding and shell navigation;
- `AppShell.tsx`: layout composition;
- feature pages: view-specific content only.

## New-view Definition of Done

For every new governed WebApp view:

1. Reconcile the upstream `WEB-*` candidate and assign a governed `UI-*` identifier.
2. Add/update its `UiCapability` contract only from accepted repository evidence.
3. Implement the feature page without global shell duplication.
4. Register the feature in `shellRoutes` with its component and icon.
5. Confirm desktop Sidebar and mobile navigation expose the same route.
6. Confirm direct URL navigation and active-link state.
7. Preserve light/dark shell behavior.
8. Run Frontend Demo CI and repository gates.
9. Deploy and perform runtime visual review.

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
- it must appear in Sidebar and mobile navigation automatically from that registry;
- after explicit product approval it may replace `/pos` as `defaultShellRoute`.

## Non-negotiable rule

`Sidebar`, `Topbar` and `BottomBar` are reusable platform components, not per-screen artwork.

Future views extend the shell. They do not repaint it.
