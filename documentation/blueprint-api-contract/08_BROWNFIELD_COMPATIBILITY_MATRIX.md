# Brownfield API Compatibility Matrix

## Governing rule

`ALIGN, DO NOT REWRITE`.

The existing 69 `/api/...` endpoints are preserved during migration. The `/api/v1` contract is additive and becomes authoritative for new client work. No legacy endpoint is removed merely because a cleaner v1 operation exists.

## Family-level reconciliation

| Legacy family | Current behavior | v1 target | Migration disposition |
|---|---|---|---|
| `ContactDetail` | CRUD + by-customer | Party contact data | compatibility adapter into Party use cases after data reconciliation |
| `ContactType` | CRUD | `listContactTypes` reference/config metadata | preserve legacy CRUD until consumer usage known; do not copy generic CRUD into v1 |
| `Country` | CRUD | `listCountries` | country becomes reference metadata; legacy writes are not automatically reproduced |
| `Customer` | action-named CRUD/paging | `/parties` with CUSTOMER role | later legacy controller delegates to Party application use cases |
| `CustomerType` | action-named CRUD/paging | Party roles/commercial profile | preserve until data/consumer semantics reconciled |
| `Department` | CRUD | `listUruguayDepartments` | reference metadata in v1 |
| `DocumentType` | CRUD | `listFiscalIdentityTypes` | versioned fiscal identity metadata; legacy meaning reconciled before cutover |
| `Info/ping` | health-like | `getHealth` | retain legacy route during transition |
| `Info/version` | version | `getVersion` | retain legacy route during transition |
| `Info/fecha` | server-local DateTime | no canonical v1 equivalent | do not perpetuate server-local clock as business authority |
| `InvoiceIndicator` | CRUD | `listInvoiceIndicators` | controlled/versioned fiscal reference metadata |
| `PaymentMethod` | CRUD | `/payment-methods` | migrate to payment-medium configuration with historical immutability |
| `ProductCategory` | CRUD | `/item-categories` | compatibility adapter after data reconciliation |
| `Products` | action-named CRUD/paging | `/items` | PRODUCT maps to CommercialItem; services share target model |
| `Supplier` | CRUD | `/parties` with SUPPLIER role | later legacy controller delegates to Party use cases |
| `SupplierType` | empty shell | no required direct v1 CRUD | classification added only if requirements prove it |
| `TaxType` | empty shell | `/tax-profiles` / versioned tax metadata | do not invent CRUD to match an empty shell |
| `VoucherType` | CRUD | fiscal/reference catalog | versioned metadata; not user-authoritative issue selection |

## HTTP behavior migration

Legacy observations:

- mostly 200/400;
- generic `ResultObject`;
- inconsistent route styles;
- DELETE body/query in some actions;
- no endpoint authorization enforcement observed;
- no API version namespace.

v1 contract:

- RFC 9457 Problem Details;
- correct 201/202/204 semantics where applicable;
- explicit permission and scope per operation;
- `/api/v1` namespace;
- stable operationId;
- state-transition commands instead of unrestricted update/delete for fiscal/financial history;
- explicit idempotency and concurrency semantics.

## Coexistence implementation pattern

During migration of a vertical:

```text
LegacyController
      -> compatibility mapper
      -> accepted Application use case
      -> target domain/infrastructure
```

is preferred to duplicating business rules.

The legacy response shape may remain for the old route while v1 returns accepted v1 DTOs.

## Security compatibility

Compatibility is not permission to preserve unsafe behavior indefinitely. The accepted P1 finding that legacy endpoints lack observed authorization enforcement requires a separate, impact-aware remediation boundary.

If protecting a legacy endpoint changes behavior for an existing consumer, the change is still necessary security work but must be classified/tested rather than hidden inside an unrelated v1 implementation.

## Deprecation/removal conditions

A legacy route may be deprecated/removed only after:

1. known consumers are inventoried;
2. replacement v1 operation is implemented and validated;
3. behavior/data differences are documented;
4. affected consumers are migrated/revalidated;
5. deprecation communication/window is complete where applicable;
6. human approval exists;
7. after an initial API Gate exists, the Blueprint API-impact artifact records affected operationIds/cross-cutting changes.

## No reverse contamination

The new v1 design does not copy Brownfield route quirks such as action names, typo routes, delete-body patterns or universal ResultObject simply to preserve code similarity. Compatibility lives at adapters, not in the canonical contract.