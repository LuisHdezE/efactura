# W1.1D Contact Type and Unit-of-Measure Scope

Status: `IMPLEMENTED / PENDING_EXACT_HEAD_CI_AND_MERGE_GATE`

Baseline: `main@950a2ec681458c00e3370fb8be2b7de2b5c8ecc2`.

Scope:

- `API-REF-007 listContactTypes`
- `API-REF-008 listUnitsOfMeasure`

This increment closes the two remaining Reference Data prerequisites without copying legacy CRUD and without inventing a DGI unit catalog that does not exist in the accepted evidence.

## REF-007 Contact Types

Contract:

- operationId: `listContactTypes`
- route: `GET /api/v1/reference-data/contact-types`
- permission: `parties.read`

The modern Party aggregate deliberately stores `PartyContact.TypeCode` as configurable text. W1.1D preserves that design and does not introduce a closed domain enum.

The Release-1 reference endpoint publishes the four retained brownfield compatibility defaults:

- `PHONE` / Phone
- `MOBILE` / Mobile
- `EMAIL` / Email
- `FAX` / Fax

These values are application defaults and compatibility metadata. They do not prevent a future governed configuration capability from adding other contact codes.

Metadata:

- SourceName: `Legacy ContactType compatibility / eFactura Release-1 defaults`
- SourceVersion: `release-1`

## REF-008 Units of Measure

Contract:

- operationId: `listUnitsOfMeasure`
- route: `GET /api/v1/reference-data/units-of-measure`
- permission: `catalog.read`

The accepted catalog/fiscal model contains two distinct facts:

1. `CommercialItem.Unit` is commercial master data, required and normalized, with an accepted maximum length of 40 characters.
2. DGI CFE 25.2 `UniMed` is fiscal content constrained to a maximum of four characters for the relevant detail line. The accepted evidence explicitly does not define a canonical DGI enumeration and forbids the application from manufacturing `N/A`.

W1.1D therefore defines `listUnitsOfMeasure` as a runtime projection of the application's actual configured commercial units rather than a static fiscal catalog.

The reader:

- only reads active commercial items;
- only reads organizations present in the authenticated actor's `company_scope` set;
- selects the stored `Unit` value;
- returns distinct values;
- sorts deterministically;
- returns an empty collection when the actor has no company scopes;
- does not create, truncate or rewrite units.

Each returned row also exposes `DgiCfe25_2Compatible = true` only when the configured commercial value has length `<= 4`. That flag is compatibility evidence, not a claim that DGI publishes or recognizes an enumerated unit code table.

Metadata:

- SourceName: `Configured active commercial item units visible to current actor`
- SourceVersion: `release-1/runtime-projection`

## Architecture boundary

Application owns the provider-neutral `IUnitOfMeasureReferenceReader` port. Infrastructure implements it using the existing `V1PersistenceDbContext` commercial-item projection. WebApi depends only on Application use cases and DTOs.

No new table, migration or legacy `ApplicationCore` dependency is introduced.

## Authorization

- REF-007 requires exact permission `parties.read`.
- REF-008 requires exact permission `catalog.read`.
- both continue to require an authenticated bearer token through the controller authorization boundary.
- missing permission must produce the existing v1 `403` Problem Details behavior.

## QA gate

The increment adds proof for:

- exact routes and operationIds;
- exact specialized permissions;
- Release-1 contact defaults without a Party contact enum rewrite;
- provider-neutral Application boundary;
- scoped/distinct/sorted active unit projection;
- no fabricated DGI UOM enumeration;
- PostgreSQL and MySQL provider-real execution of the UOM reader;
- updated 194-operation matrix arithmetic.

Runtime acceptance after deployment must verify 401/403/200 behavior, exact contact payload metadata and company-scope isolation for units.

## Resulting completion baseline

After this executable increment is accepted:

- Reference Data: `8 / 8` implemented;
- Wave 1: `15 / 30` implemented;
- global public v1: `49 / 194` implemented;
- remaining `MISSING_HTTP`: `143`;
- remaining contract-collision IDs: `2`;
- remaining non-implemented IDs: `145`.

The next bounded Wave 1 increment is W1.2: `API-IAM-001 getCurrentActor` plus `API-IAM-011 listPermissions`.
