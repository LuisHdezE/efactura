# UI-SUPPLIER-001 — Runtime Visual Acceptance

Status: `REVIEWED / VISUAL_RUNTIME_ACCEPTED`

Review result: `PASS`

Approved visual authority:

`documentation/ui/references/approved/UI-SUPPLIER-001/v1-theme-pair/README.md`

## Review target

`https://eliasworks.uy/efactura/proveedores`

Accepted deployed WebApp commit:

`525a5be0bee9458220d0e464f0d2df8aa935497b`

Implementation evidence:

- implementation PR: `#171` — `feat(webapp): implement approved UI-SUPPLIER-001`;
- implementation merge commit: `49c80d7e33d79f2c1a36f43edf43b3cde374e8d0`;
- runtime-polish PR: `#172` — `fix(webapp): polish UI-SUPPLIER-001 runtime details`;
- accepted deployed polish commit: `525a5be0bee9458220d0e464f0d2df8aa935497b`;
- deployment workflow: `Deploy eFactura Demo #28` — success;
- frontend validation for polish PR: `Frontend Demo CI #64` — success;
- repository guard for polish PR: `Clean Architecture Guard #597` — success, including PostgreSQL and MySQL transaction tests.

After the accepted UI deployment, API PR `#170` advanced `main` to `a4b8386ad1eb5ad300f360a68ae5eb322625969f`. That later merge is API/reference-data work and does not change the accepted WebApp visual evidence recorded here.

## Runtime evidence

On 2026-09-19 Luis performed direct runtime review of `/efactura/proveedores` in both theme variants and supplied deployed screenshots for:

- dark theme;
- light theme.

The first runtime review identified three non-blocking presentation details:

1. `Nuevo proveedor` looked more operational than its disabled behavior warranted;
2. the right-side detail tabs exposed distracting scrollbar chrome;
3. planned Sidebar entries in dark mode were harder to read than intended.

PR `#172` addressed only those presentation details, without changing routes, capabilities, fixtures, gateway behavior, backend contracts or API behavior.

The final runtime review confirmed:

- compact reusable AppShell and governed complete Sidebar;
- `Proveedores` active under `Comercial`;
- planned modules visible but non-interactive;
- dense supplier list/table presentation;
- search field and country/state filters;
- selected-row treatment;
- person/organization supplier representation;
- visible fiscal/document identity and primary contact data;
- active/inactive state badges;
- master-detail panel on the right;
- information, fiscal identities, and addresses/contacts tabs;
- consistent light/dark hierarchy;
- `Nuevo proveedor` visually honest as disabled in the current demo boundary;
- no distracting internal scrollbar chrome in the accepted detail-panel presentation;
- improved dark-mode readability for planned Sidebar items without making them appear active;
- no fabricated supplier balances, aging, purchase totals, open-order counts or received-CFE summaries.

No blocking visual defect remained in the reviewed light or dark deployment.

## Human acceptance

Luis explicitly approved closure of the deployed runtime result on 2026-09-19 with the statement:

> Apruebo cierre runtime UI-SUPPLIER-001

This closes the `UI-SUPPLIER-001` visual/runtime implementation lane as `REVIEWED / VISUAL_RUNTIME_ACCEPTED`.

## Functional boundary retained

This acceptance is presentation/runtime acceptance only. It does not expand backend capability and does not authorize the WebApp to fabricate write integration or unsupported supplier projections.

The accepted runtime remains honest about the current WebApp boundary:

- supplier master data is represented over the unified `Party` model with `SUPPLIER` role;
- local/demo fixtures remain explicit where the WebApp is not yet wired live;
- the create action is visibly disabled in the current demo boundary;
- financial and procurement summaries remain absent until authoritative projections exist.

The acceptance does not authorize fabricated:

- payable balances;
- aging;
- purchase totals;
- open purchase orders;
- received CFE counts/history;
- unsupported supplier write behavior;
- unsupported account summaries.

## Why final repository-wide `ACCEPTED` is not claimed

The reconciliation/specification records an independent traceability gap: no dedicated governed `UC-SUPPLIER-*` lifecycle and no governed `US-*` supplier-master artifact are currently evidenced.

The approved visual source also remains fingerprinted with `BINARY_PRESERVATION_PENDING`; the exact approved PNG has not been inserted byte-for-byte through the current repository channel.

Neither item reopens the visual/runtime lane. They remain separate governance debts and must not be silently filled or represented as complete.

Therefore the runtime lane is closed at `REVIEWED / VISUAL_RUNTIME_ACCEPTED`, while final repository-wide `ACCEPTED` remains reserved for the appropriate governance work.