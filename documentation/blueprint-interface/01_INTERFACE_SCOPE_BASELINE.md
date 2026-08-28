# Interface Scope Baseline

## Status

`DRAFT / SCOPE_BASELINE / BROWNFIELD`

This boundary exists because Blueprint 0.5.1 requires an early interface scope after Requirements Ready and before Architecture/API Contract Design. It is **not frontend implementation**, visual design, mockup approval or executable interface inventory.

Canonical schema:

`SoftwareDevelopmentBlueprint/schemas/interface-inventory.schema.json` component version `0.5.0`, maturity `SCOPE_BASELINE`.

## Sources

- current eFactura AS-IS: no frontend/mobile client is evidenced in the repository;
- `FacturacionElectronicaBases`: reference capability/UX know-how only;
- target requirements in `documentation/blueprint-target/`;
- explicit requirement that future web/mobile clients support safe offline operation where applicable.

Every item in the machine-readable baseline is therefore classified `PROPOSED`, not `OBSERVED`.

## Why this is needed now

The API must be shaped by real client journeys without allowing the client to invent business semantics. This scope baseline identifies which future interfaces need authoritative server data/actions so Architecture and API Contract Design can provide them intentionally.

It deliberately does **not** invent:

- OpenAPI `operationId` values;
- API IDs;
- final route names;
- final permission identifiers;
- UI component/design-system decisions;
- committed functional-slice boundaries.

Those bindings are resolved later after the API contract and API Gate.

## Web scope

The proposed web client covers:

- authentication/session entry;
- operational dashboard;
- POS sale and fiscalization status;
- parties/customers/suppliers;
- products/services/catalog;
- inventory/transfers;
- procurement/receipts;
- receivables/collections;
- payables/supplier payments;
- cash shifts/reconciliation;
- fiscal documents and corrections;
- CAE management;
- contingency/offline sync supervision;
- received CFE/XML validation;
- reports/fiscal calendar;
- audit/security/configuration.

## Android scope

The proposed Android client is operational rather than a duplicate of the entire administration portal. Initial scope includes:

- authentication;
- POS/sale operation;
- offline queue and synchronization;
- party/customer lookup;
- product/service lookup;
- inventory lookup/operational movement support;
- cash-shift operation;
- fiscal-document/result lookup.

Administrative CAE/security/tax-rule configuration remains web-first unless future requirements explicitly add mobile administration.

## Offline baseline

`offline` appears as an interface state only where business use requires it. That does not mean the client becomes authoritative. Offline clients queue proposed commands with client operation identity; server synchronization later applies authorization, tax, fiscal, stock and financial invariants.

Formal DGI CFC contingency is a distinct business/fiscal workflow and is not represented as generic local queueing.

## Readiness rule

`interface_scope_ready` may be accepted only when:

1. machine-readable baseline validates against the Blueprint schema;
2. every item traces to target requirements;
3. missing API capabilities are recorded as unresolved needs rather than fabricated operations;
4. web vs Android scope is explicit;
5. no item claims executable/API-ready maturity.

The later `EXECUTABLE_INVENTORY` will reconcile every baseline ID as `COMMITTED`, `DEFERRED` or `DROPPED` after API Gate.
