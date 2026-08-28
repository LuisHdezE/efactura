# Data Architecture: PostgreSQL and MySQL

## Decision

The application supports **PostgreSQL or MySQL selected per deployment**, with equivalent business semantics.

Provider differences belong to Infrastructure. Domain/Application code cannot branch on database product.

## Persistence strategy

- EF Core: authoritative write model, transactions, mappings, migrations.
- Dapper: read/reporting queries and measured hot paths; never an alternate business-rule layer.
- one logical domain model;
- provider-specific EF migrations where SQL/provider behavior differs;
- persistence contract tests execute against both engines.

## MySQL EF Core provider decision

Architecture baseline selects **Oracle `MySql.EntityFrameworkCore` on the EF Core 8 line** for the MySQL implementation.

Rationale:

- the Brownfield solution already carries `MySql.Data`, so this choice minimizes provider-family churn;
- Microsoft lists `MySql.EntityFrameworkCore` as an EF Core provider supporting EF Core 8;
- Oracle/MySQL publishes maintained 8.0.x provider packages for .NET 8;
- provider-specific behavior remains isolated, so this choice does not leak into Domain/Application.

Evidence reviewed on 2026-08-28:

- Microsoft EF Core provider catalog: https://learn.microsoft.com/en-us/ef/core/providers/
- NuGet `MySql.EntityFrameworkCore` 8.0.x line: https://www.nuget.org/packages/MySql.EntityFrameworkCore

Exact package patches are an implementation lock-file/compatibility concern. All Microsoft EF Core 8 packages, Npgsql EF Core 8 provider and MySQL EF Core 8 provider must be pinned to a mutually compatible tested set. The current Brownfield `8.0.10` packages are not treated as an eternal version requirement.

Pomelo was also evaluated as a valid alternative, but is not the baseline because the current solution already uses Oracle `MySql.Data` and the architectural goal is minimum Brownfield provider churn. Switching later requires an ADR and persistence-equivalence evidence, not a domain rewrite.

## Provider isolation

Suggested Infrastructure organization:

```text
Infrastructure/Persistence/
  Common/
    Configurations/
    Repositories/
    Transactions/
  PostgreSql/
    Migrations/
    ProviderServices/
  MySql/
    Migrations/
    ProviderServices/
```

Do not put PostgreSQL `nextval(...)`, `::regclass`, provider-specific timestamp SQL or quoted-identifier assumptions inside ApplicationCore.

## Naming and type policy

Use portable lower `snake_case` physical names unless migration compatibility requires preserving an existing table during transition.

Canonical precision targets, subject to regulatory field-level validation:

| Concept | Logical precision target |
|---|---|
| monetary amount/total | decimal(19,4) |
| quantity | decimal(19,6) |
| tax/rate percentage | decimal(9,6) |
| exchange rate | decimal(19,8) |

Final CFE serialization applies the exact scale/rounding required by the active DGI specification. Storage precision must not silently pre-round a value needed by that calculation.

Temporal policy:
- UTC instant/date-time for recorded/occurred technical events;
- explicit local fiscal/business date fields where rules require them;
- no reliance on database server local timezone.

## Portable concurrency

Do not base core semantics solely on SQL Server `rowversion`, PostgreSQL `xmin`, or MySQL-specific locking extensions.

Use an application-visible `version` bigint/integer concurrency token for contested aggregates where practical. Provider adapters may additionally use native locking internally.

## Core relational model by module

### Organization / IAM

- `companies`
- `locations`
- `terminals`
- `user_accounts`
- `roles`
- `permissions`
- `role_permissions`
- `user_role_assignments`
- `user_scope_assignments`

### Parties / Catalog / Tax

- `parties`
- `party_roles`
- `party_fiscal_identities`
- `party_addresses`
- `party_contacts`
- `commercial_items`
- `item_categories`
- `tax_profiles`
- `tax_rule_versions`

### Sales / Fiscal

- `sales`
- `sale_lines`
- `sale_adjustments`
- `fiscal_documents`
- `fiscal_document_lines`
- `fiscal_document_references`
- `fiscal_transport_envelopes`
- `fiscal_transport_items`
- `fiscal_acknowledgements`
- `cae_authorizations`
- `cae_allocations`
- `fiscal_contingency_documents`
- `fiscal_rule_versions`
- `fiscal_artifacts`

### Inventory / Procurement

- `inventory_positions`
- `stock_movements`
- `stock_transfers`
- `stock_transfer_lines`
- `purchase_orders`
- `purchase_order_lines`
- `goods_receipts`
- `goods_receipt_lines`
- costing tables/projections when PPP/FIFO is enabled.

### Financial / Cash

- `receivables`
- `receivable_adjustments`
- `payables`
- `payable_adjustments`
- `payments`
- `payment_allocations`
- `cash_shifts`
- `cash_movements`

### Reliability / Audit / received documents

- `idempotency_records`
- `inbox_messages`
- `outbox_messages`
- `client_operations`
- `sync_batches`
- `audit_events`
- `received_fiscal_documents`
- `received_fiscal_validation_findings`

## Required uniqueness examples

- `party_fiscal_identities(type, issuing_country, normalized_number)` according to allowed sharing rules;
- `commercial_items(company_id, code)`;
- `fiscal_documents(company_id, cfe_type, series, number)`;
- CAE range identity and non-overlap validation;
- `idempotency_records(scope, key)`;
- `client_operations(device_id, client_operation_id)` or globally unique operation identity;
- received CFE canonical identity/hash uniqueness according to document type.

## Append/correction semantics

The following are never routine hard-delete histories:

- fiscal documents/artifacts/acks;
- audit events;
- stock movements;
- payment allocations;
- receivable/payable adjustments;
- cash reconciliation movements;
- idempotency/inbox/outbox evidence while retention requires it.

Corrections append compensating/linked facts.

## Migration strategy from current schema

1. preserve existing tables/routes while consumers are unknown;
2. introduce target tables/mappings additively;
3. migrate one functional slice at a time;
4. backfill/normalize with verified scripts and reconciliation evidence;
5. compatibility controllers/services delegate to the target model when safe;
6. remove legacy structures only after usage/data reconciliation and human approval.

No mass drop/recreate is authorized by this architecture.
