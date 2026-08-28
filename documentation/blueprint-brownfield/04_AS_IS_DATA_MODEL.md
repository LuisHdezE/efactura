# AS-IS Data Model

## Entity-file inventory

`src/ApplicationCore/Entities` contains exactly 24 entity/model files at the inspected baseline:

1. `CashTransactions.cs`
2. `ContactDetail.cs`
3. `ContactType.cs`
4. `Country.cs`
5. `Customer.cs`
6. `CustomerType.cs`
7. `CustomerTypes.cs`
8. `Customers.cs`
9. `Department.cs`
10. `DocumentType.cs`
11. `InvoiceIndicator.cs`
12. `Invoices.cs`
13. `PaymentMethod.cs`
14. `Payments.cs`
15. `Product.cs`
16. `ProductCategories.cs`
17. `ProductCategory.cs`
18. `Products.cs`
19. `PurchaseOrders.cs`
20. `Supplier.cs`
21. `SupplierTypes.cs`
22. `Suppliers.cs`
23. `TaxTypes.cs`
24. `VoucherType.cs`

## Persistence DBContext inventory

`Infrastructure.DataBase.Context.DBContext` exposes 12 DbSets:

| DbSet/model | Key observed | Important fields/shape | Mapping notes |
|---|---|---|---|
| CashTransactions | Id | TransactionDate, Amount, TransactionType, Description, RelatedInvoiceId, audit fields | Amount `decimal(10,2)`; sequence default. |
| CustomerTypes | Id | Name, audit fields | soft-delete timestamp shape. |
| Customers | Id | Name, Email, Phone, Address, CustomerTypeId, audit fields | Name required, size limits. |
| Invoices | Id | OrderId, InvoiceDate, AmountDue, AmountPaid, DueDate, audit fields | amounts `decimal(10,2)`. |
| PaymentMethod | Id | Name, audit fields | master table. |
| Payments | Id | InvoiceId, PaymentDate, Amount, PaymentMethodId, audit fields | Amount `decimal(10,2)`. |
| ProductCategories | Id | Name, audit fields | master table. |
| Products | Id | Name, Description, Price, Stock, ProductCategoryId, audit fields | Price `decimal(10,2)`. |
| PurchaseOrders | Id | **CustomerId**, OrderDate, TotalAmount, Status, audit fields | `CustomerId` on a purchase-order-shaped model is OBSERVED; business meaning is UNKNOWN/inconsistent-looking. |
| SupplierTypes | Id | Name, audit fields | master table. |
| Suppliers | Id | Name, ContactName, Phone, Email, Address, SupplierTypeId, audit fields | master data. |
| TaxTypes | Id | Name, Rate, audit fields | Rate `decimal(5,2)`. |

## Persistence characteristics

- Scaffold-like models and `DBContext` use namespace `ApplicationCore.Entites` (spelling differs from `ApplicationCore.Entities`).
- PostgreSQL-specific defaults use `nextval('"...IdSeq"'::regclass)`.
- Timestamps are mapped as `timestamp(6) without time zone`.
- Both lowercase and PascalCase sequence names are declared for several concepts, suggesting scaffold/schema drift that must be reconciled before portability work.
- `CreatedBy`, `UpdatedBy`, `DeletedAt`, `DeletedBy` patterns are common in persistence models.
- No `HasOne/HasMany` relationship configuration was observed in the DBContext mapping reviewed. Scalar `...Id` properties suggest intended references, but relational integrity/foreign-key implementation must be verified against the actual database/scripts before being declared present.

## Parallel/duplicate model families

`OBSERVED` parallel concepts include:

- `Customer` vs `Customers`
- `CustomerType` vs `CustomerTypes`
- `Product` vs `Products`
- `ProductCategory` vs `ProductCategories`
- `Supplier` vs `Suppliers`

The singular models and scaffold-like plural models differ in namespace/style and fields. Their historical intent cannot be proven from code alone.

**Classification:** `DUPLICATE_OR_PARALLEL / ORIGIN_UNKNOWN`.

Do not delete or consolidate either side before tracing repository/service usage and database compatibility.

## Important capability distinction

The persistence model contains invoices, payments, purchase orders, cash transactions and tax types, but those tables/classes do not prove completed business lifecycles. Current controllers expose mostly master CRUD and do not expose full invoice/payment/purchase/cash workflows.

## Data risks/gaps

- PostgreSQL-specific sequence/default syntax prevents current DBContext mapping from being provider-neutral.
- Relationship constraints are not demonstrated by the reviewed mapping.
- `PurchaseOrders.CustomerId` requires domain clarification.
- Financial amounts have exact decimal precision, which is positive, but target fiscal rounding/precision rules require explicit specification.
- soft-delete/audit columns exist, but they are not equivalent to immutable business audit history.
