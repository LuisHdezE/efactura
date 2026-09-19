# W1.1A Reference Data Foundation

Status: `IMPLEMENTATION_CANDIDATE / GOVERNED_PR_REQUIRED`

Baseline source: `main@bcefca379158d951e59ad6cf1ee69247d92df010`.

Scope: `API-REF-002` and `API-REF-003` only.

## Public HTTP surface

- `API-REF-002 listUruguayDepartments` -> `GET /api/v1/reference-data/uruguay-departments` -> `AUTHENTICATED`.
- `API-REF-003 listFiscalIdentityTypes` -> `GET /api/v1/reference-data/fiscal-identity-types` -> `AUTHENTICATED`.

Both actions are protected by ASP.NET Core `[Authorize]`. No specialized business permission is added because the accepted contract says `AUTHENTICATED`. The Application use cases independently verify `ActorContext.IsAuthenticated` as defense in depth.

## Application boundary

`IReferenceDataCatalog` is the provider-neutral read port. W1.1A introduces two read use cases:

- `ListUruguayDepartmentsUseCase`;
- `ListFiscalIdentityTypesUseCase`.

The controller depends on those Application use cases only. It does not reference legacy `ApplicationCore`, Dapper, Npgsql or a database repository.

## Release-1 source

`Release1ReferenceDataCatalog` is deterministic and read-only.

### Uruguay departments

The source is the governed Release-1 projection of the 19 Uruguay department names already evidenced by the retained brownfield seed. No legacy numeric department/country IDs are exposed and no new department codes are invented.

### Fiscal identity types

The source is `DGI Formato_CFE` version `25-2`, using the accepted receiver identity metadata already frozen by the fiscal design:

- 1 NIE;
- 2 RUC (Uruguay);
- 3 C.I. (Uruguay);
- 4 Otros;
- 5 Pasaporte;
- 6 DNI, constrained to AR/BR/CL/PY;
- 7 NIFE.

Country-rule metadata is explicit and deterministic. W1.1A does not broaden the six still-blocked Reference Data contracts.

## QA boundary

The increment tests:

- exact 19-department projection;
- exact identity codes 1..7 and DGI source/version metadata;
- DNI country restriction evidence;
- Application rejection of anonymous actors;
- authenticated actors do not need an unrelated business permission;
- v1 authorization challenge produces HTTP 401 RFC 9457 response with `authentication_required`;
- exact controller routes and `[Authorize]` boundary;
- no direct controller/Application dependency on legacy reference repositories;
- matrix reconciliation from 41 to 43 implemented operations.

No persistence schema or transaction behavior changes in W1.1A. Existing provider-real PostgreSQL/MySQL CI remains a regression gate but no new persistence test is required for the static catalog itself.

## Matrix effect after governed merge

- global implemented: `41 -> 43`;
- global `MISSING_HTTP`: `151 -> 149`;
- contract-collision IDs: unchanged at `2`;
- global non-implemented: `153 -> 151`;
- Wave 1 implemented: `7 -> 9`;
- Wave 1 non-implemented: `23 -> 21`.

The six audited Reference Data prerequisites remain unchanged:

`REF-001`, `REF-004`, `REF-005`, `REF-006`, `REF-007`, `REF-008` remain `MISSING_HTTP / PREREQUISITE_REQUIRED`.

## Deployment gate

Because this increment changes WebApi runtime behavior, governed merge must be followed by the repeatable API demo deployment workflow and public verification of:

- Swagger UI;
- OpenAPI document;
- unauthenticated 401 on both new routes;
- authenticated 200 on both new routes;
- existing JWT + Neon smoke path.
