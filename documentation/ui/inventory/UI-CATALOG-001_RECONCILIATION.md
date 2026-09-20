# UI-CATALOG-001 — Products and Services Catalog Reconciliation

Status: `RECONCILED / SPECIFICATION_READY_WITH_TRACEABILITY_GAPS`

Upstream interface scope: `WEB-006` — Products and Services Catalog

Reconciled against: `main@9da0e2c1a159b0a3c8175dee97105e608b76b62b`

## Decision

`WEB-006` is promoted to the governed UI boundary:

```text
WEB-006 -> UI-CATALOG-001
```

The governed route is reserved as:

```text
/catalogo
```

The first governed view covers the commercial product/service master, category assignment, tax-profile reference, inventory-behavior flag and unit-of-measure metadata already supported by executable contracts.

It does **not** invent pricing, cost, product media, barcode, live stock quantity, supplier linkage, variants or other catalog attributes that are not present in the current authoritative commercial-item contract.

## Upstream scope preserved

The accepted Interface Scope Baseline defines `WEB-006` with:

- module: `Catalog`;
- purpose: manage sellable products/services, inventory behavior, categories and fiscal/tax profile references;
- requirements: `FR-012`, `FR-013`, `FR-014`, `FR-015`;
- roles: administrator, seller and inventory operator;
- authoritative data: commercial items.

The corresponding requirements establish:

- `FR-012`: products and services share a common commercial concept while preserving type-specific behavior;
- `FR-013`: stock tracking is optional by item and service-only companies must not require inventory configuration;
- `FR-014`: sales may mix stock-tracked products and non-stock services;
- `FR-015`: catalog tax assignment references versioned tax/fiscal configuration rather than magic percentages in controllers.

## Use-case and user-story gap

No dedicated governed `UC-CATALOG-*` lifecycle and no governed `US-*` catalog-master artifact are currently evidenced in the repository.

This reconciliation records that independent traceability gap instead of fabricating missing lifecycle or story artifacts inside frontend work.

## Executable catalog dependencies

Wave 2 currently records `API-CAT-001..008` as implemented WebApi paths.

| API ID | operationId | Route | Permission | UI disposition |
| --- | --- | --- | --- | --- |
| `API-CAT-001` | `listItems` | `GET /api/v1/items` | `catalog.read` | `SUPPORTED` |
| `API-CAT-002` | `createItem` | `POST /api/v1/items` | `catalog.manage` | `SUPPORTED` |
| `API-CAT-003` | `getItem` | `GET /api/v1/items/{itemId}` | `catalog.read` | `SUPPORTED` |
| `API-CAT-004` | `updateItem` | `PATCH /api/v1/items/{itemId}` | `catalog.manage` | `SUPPORTED` |
| `API-CAT-005` | `deactivateItem` | `POST /api/v1/items/{itemId}/deactivate` | `catalog.manage` | `SUPPORTED` |
| `API-CAT-006` | `listItemCategories` | `GET /api/v1/item-categories` | `catalog.read` | `SUPPORTED` |
| `API-CAT-007` | `createItemCategory` | `POST /api/v1/item-categories` | `catalog.manage` | `SUPPORTED` |
| `API-CAT-008` | `updateItemCategory` | `PATCH /api/v1/item-categories/{categoryId}` | `catalog.manage` | `SUPPORTED` |
| `API-CAT-009` | `listTaxProfiles` | `GET /api/v1/tax-profiles` | `catalog.read` | `SUPPORTED / READ_ONLY_REFERENCE` |
| `API-REF-008` | `listUnitsOfMeasure` | `GET /api/v1/reference-data/units-of-measure` | `catalog.read` | `SUPPORTED / SUGGESTION_REFERENCE` |

`API-CAT-009` is implemented through `TaxProfilesController`. `API-REF-008` entered the executable reference-data surface through W1.1D.

## Commercial-item projection actually available

`CommercialItemDto` currently exposes:

- id;
- version;
- active;
- code;
- name;
- description;
- kind: `PRODUCT` or `SERVICE`;
- unit;
- `trackInventory`;
- `taxProfileId`;
- `categoryId`.

The list endpoint supports server-side filters for:

- text search;
- kind;
- active state;
- inventory-tracking flag;
- category;
- page/pageSize.

## Domain constraints that must remain visible in the UI

### Product vs service

`CommercialItemKind` is exactly:

- `PRODUCT`;
- `SERVICE`.

A service cannot be configured with `trackInventory=true`. The backend enforces `catalog.service_inventory_forbidden`; the UI should prevent the invalid combination before submit without replacing backend authority.

A product may be either stock-tracked or non-stock-tracked.

### Code and text normalization

- item code is required, normalized uppercase and limited to 80 characters;
- name is required and limited to 250 characters;
- description is optional and limited to 1000 characters;
- unit is required, normalized uppercase and limited to 40 characters;
- duplicate item code within an organization is rejected.

### Category assignment

A category is optional.

When assigned:

- it must exist inside the organization;
- it must be active;
- categories have code, name, active state and optimistic version;
- category create/update are executable catalog operations.

### Tax-profile assignment

A tax profile is optional but, when assigned, must be authoritatively validated as assignable for the organization/business date.

The catalog UI may read and display tax-profile metadata through `API-CAT-009`, including:

- code;
- name;
- treatment code;
- rate percent when the authoritative profile exposes one;
- effective dates;
- source metadata;
- active state.

The first catalog UI does not administer tax-profile definitions because only the read operation is part of this catalog surface.

### Unit of measure

`CommercialItem.Unit` is commercial master data, not a closed DGI enumeration.

`API-REF-008` returns distinct units actually configured on active items visible to the actor and exposes whether a value is compatible with the DGI CFE 25.2 four-character field constraint.

Therefore the UI must treat units as editable text with suggestions/autocomplete from current configured values, **not** as a closed select that invents an official DGI unit catalog.

## Mutation semantics

Create/update/deactivate require:

- `catalog.manage`;
- organization scope;
- idempotency key;
- optimistic version where applicable.

The current contract supports item deactivation but no dedicated item-reactivation operation. The UI must not promise reactivation unless a future executable contract adds it.

## Explicitly unsupported catalog claims

The current authoritative `CommercialItemDto` does not contain:

- sale price or price list;
- purchase cost;
- currency-specific pricing;
- barcode/GTIN;
- product image/media attachment;
- available/on-hand stock quantity;
- reserved/available-to-sell quantity;
- reorder point or EOQ/ROP values;
- supplier-product relationship;
- variants/options;
- product dimensions/weight;
- accounting account mapping.

Sale `unitPrice` exists on sale-line commands/projections, but it is not an authoritative item-master price field. Consequently `UI-CATALOG-001` v1 must not present a mock price as catalog truth.

Images currently used by the POS belong to frontend visual metadata and are not part of the catalog API contract. They must not be represented as authoritative product-media management.

Stock quantities and movement history belong to the future `WEB-007` inventory boundary. `UI-CATALOG-001` may show only the item's `trackInventory` behavior flag.

## Reconciliation outcome

`UI-CATALOG-001` is sufficiently bounded for functional specification and visual drafting.

The supported first version is a compact commercial master experience with:

- item list/search/filtering;
- product/service distinction;
- active/inactive state;
- code, name and description;
- unit;
- inventory-tracking behavior;
- category assignment and category maintenance;
- tax-profile read/assignment reference;
- create/update/deactivate semantics;
- optimistic concurrency and idempotent mutation behavior.

The route remains `PLANNED_DISABLED` until visual approval and frontend implementation are completed through the normal governed sequence.
