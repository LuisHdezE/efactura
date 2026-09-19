# UI-DASHBOARD-001 Runtime Refinement Notes

Status: `RUNTIME FEEDBACK APPLIED / PENDING REDEPLOY REVIEW`

## Runtime feedback

After the density/typography refinement was deployed, runtime review found two remaining shell/presentation issues:

1. The desktop Sidebar still behaved like a tile launcher: oversized vertical navigation items and large icons consumed too much space, and the complete planned product navigation was not visible.
2. The `Inventario · Demo` card used a placeholder square glyph (`□`) instead of a meaningful product/package icon.

## Accepted correction

- Sidebar becomes a compact horizontal-row business navigation.
- All 18 shell-hosted product options remain visible in their governed groups.
- Implemented views are interactive.
- Future views are visible but disabled/non-clickable; no candidate/fake routes are created.
- Sidebar width is adjusted to support readable labels while row height is reduced substantially.
- The stock placeholder glyph is visually replaced with a package/product icon treatment.
- Backend, API contracts, auth and feature routes are unchanged.

## Governance reconciliation

This runtime decision supersedes the older `hidden until implementation` visibility wording in `WEBAPP_NAVIGATION_MAP.md` and `WEBAPP_SHELL_POLICY.md` while preserving the no-dead-links rule.

The unified navigation registry in `routes.tsx` now distinguishes `active` and `planned` entries. `shellRoutes` remains executable-only.
