# UI-DASHBOARD-001 v1-theme-pair — Approved visual authority

Status: `VISUAL_APPROVED / SOURCE_FINGERPRINTED / BINARY_PRESERVATION_PENDING`

Approved by Luis on 2026-09-19 with the explicit statement:

> aprobado

This approval applies to the `UI-DASHBOARD-001 v1-theme-pair` composition presented immediately before the approval statement.

## Approved source fingerprint

- source presentation filename: `a_clean_multi_panel_ui_mockup_collage_of_an_efactu.png`
- image generation id: `b5781a68-6c5a-4ef1-b7a5-e8d100c45dfd`
- dimensions: `1536 x 1024`
- source bytes: `1750916`
- SHA-256: `2b2c90f70a4e66055e7a7881af0932708e9288143cf7a34c6dc361cc06bd2a08`

The exact approved PNG source is the visual authority. It must not be silently regenerated, substituted or reinterpreted as a different approved version.

The current repository connector cannot transfer this generated binary directly into Git in this lane, so physical byte-identical preservation remains pending. The fingerprint above is not a substitute for the binary and the debt must remain visible until the exact source is committed.

## Governed interpretation

The approved composition establishes the feature-level visual direction:

- paired light and dark Dashboard presentation;
- dense but readable operational hierarchy;
- KPI cards, commercial trend, fiscal/CAE summary, operational alerts, upcoming obligations and financial summary;
- desktop and responsive/mobile treatment;
- Dashboard integrated into the reusable eFactura shell rather than introducing another shell.

The image contains illustrative future navigation entries and sample business values. Those are not executable-route or API authority.

Implementation MUST continue to obey `documentation/ui/WEBAPP_SHELL_POLICY.md`:

- Sidebar, Topbar, BottomBar and MobileNavigation remain the shared reusable components;
- executable navigation comes only from `shellRoutes`;
- at the first Dashboard implementation, only routes actually registered and executable may be presented as active navigation destinations;
- Dashboard-specific figures, dates, alerts and health/status values remain explicit demo/mock fixtures while the required API operations are not executable;
- no visual text may imply live DGI/integration/server authority that does not exist.

This means visual approval does not authorize frontend invention of backend capability, future routes or live operational state.

## Implementation checkpoint

The next governed step is implementation of the approved composition as `UI-DASHBOARD-001` inside the existing reusable shell, followed by Frontend Demo CI, repository gates, deployment and explicit runtime visual review.
