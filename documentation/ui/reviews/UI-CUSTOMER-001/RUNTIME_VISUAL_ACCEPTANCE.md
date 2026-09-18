# UI-CUSTOMER-001 — Runtime Visual Acceptance

Status: `REVIEWED / VISUAL_RUNTIME_ACCEPTED`

Review result: `PASS`

Approved visual authority:

`documentation/ui/references/approved/UI-CUSTOMER-001/v2-theme-pair/customer-theme-pair.png`

## Review target

`https://efactura.eliasworks.uy/clientes`

Accepted deployed main:

`b38e7b2ca57ba2262a85786b12af5d1917b3c1d2`

Implementation evidence:

- merged PR: `#135` — `feat(webapp): implement approved UI-CUSTOMER-001 v2 theme pair`;
- implementation: `src/WebApp/src/features/customers/CustomersPage.tsx`;
- styles: `src/WebApp/src/features/customers/customers.css`;
- deployment workflow: `Deploy eFactura Demo #16` — success;
- frontend workflow: `Frontend Demo CI #38` — success;
- repository guard: `Clean Architecture Guard #537` — success.

## Runtime evidence

On 2026-09-17 Luis performed a direct runtime review of the deployed `/clientes` view in both approved theme variants:

- light theme;
- dark theme.

The reviewed deployment preserved the approved `v2-theme-pair` visual language and the intended master-detail structure.

Observed runtime acceptance covered:

- compact customer list with four demo records;
- visible customer search and result count;
- selected-row treatment;
- non-photographic initials/entity identifiers;
- person/company classification;
- customer/supplier role badges;
- active-state indicators;
- fiscal identity visibility;
- primary address and contact presentation;
- right-side detail panel;
- consistent light/dark composition;
- disabled `Nuevo cliente` and `Editar` actions in the current demo boundary;
- absence of fabricated balances, aging, credit limits or unsupported account-summary data.

No blocking visual defect was identified in the reviewed light or dark deployment.

## Human acceptance

Luis explicitly approved the deployed runtime result on 2026-09-17 with the statement:

> Apruebo UI-CUSTOMER-001 runtime visual

This closes the UI-CUSTOMER-001 visual/runtime implementation lane as `REVIEWED / VISUAL_RUNTIME_ACCEPTED`.

## Functional boundary retained

This runtime acceptance is presentation/runtime acceptance only. It does not expand backend capability or authorize the frontend to fabricate write support.

The current deployed demo keeps `Nuevo cliente` and `Editar` visibly disabled because the WebApp write path is not wired in its current gateway boundary. That intentional limitation is accepted as functionally honest for this visual increment.

The acceptance also does not authorize fabricated:

- balances;
- debt or aging;
- credit exposure or limits;
- account summaries;
- historical update timestamps;
- authoritative reference-data catalogs;
- unsupported customer-master write behavior.

## Why the inventory does not use final `ACCEPTED`

The governed specification and reconciliation still record an independent traceability requirement: no governed `US-*` customer-master artifact and no dedicated `UC-PTY-*`/`UC-CUSTOMER-*` lifecycle are currently evidenced in the repository.

For that reason, the visual lane is closed at `REVIEWED / VISUAL_RUNTIME_ACCEPTED`, while final repository-wide UI governance state `ACCEPTED` remains reserved until those independent traceability prerequisites are satisfied.

Those remaining governance gaps must not reopen this visual implementation lane or be filled by inventing requirements inside frontend work.
