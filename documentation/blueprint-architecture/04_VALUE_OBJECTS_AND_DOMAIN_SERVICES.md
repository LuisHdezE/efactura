# Value Objects and Domain Services

## Value-object policy

Business primitives with validation/equality semantics are represented explicitly. Value objects are persistence/HTTP agnostic.

## Core value objects

| Value object | Purpose / invariant |
|---|---|
| `Money` | decimal amount + ISO currency; no binary floating point |
| `CurrencyCode` | ISO 4217-style validated code |
| `Quantity` | decimal quantity with unit-compatible scale |
| `UnitOfMeasure` | canonical unit identifier |
| `Percentage` | bounded percentage representation |
| `TaxRate` | exact regulatory rate representation/version context |
| `CountryCode` | ISO country code |
| `FiscalIdentity` | type + number + issuing country + validity metadata |
| `UruguayanRuc` | validated RUC representation when applicable |
| `Address` | structured postal address/country |
| `EmailAddress` | normalized validated email |
| `PhoneNumber` | normalized phone value where required |
| `DateRange` | inclusive/exclusive policy documented by constructor |
| `FiscalSpecificationVersion` | e.g. active DGI format identity |
| `FiscalDocumentTypeCode` | code from versioned catalog, not applicability logic |
| `FiscalDocumentNumber` | type/series/number identity |
| `CaeRange` | authorized first/last range and series context |
| `ExchangeRate` | exact rate + source/date context |
| `DocumentHash` | cryptographic artifact hash |
| `IdempotencyKey` | validated API idempotency identity |
| `ClientOperationId` | globally unique offline operation identity |
| `DeviceId` | registered/sync device identity |
| `CorrelationId` | cross-boundary observability identity |

## Identifier migration policy

The Brownfield database currently uses integer/bigint identifiers. Architecture does **not** require a destructive global re-key.

Rules:
- preserve existing keys while migrating existing aggregates;
- wrap identifiers in domain-specific types where practical to prevent accidental cross-entity use;
- new offline operation/device/idempotency identities use application-generated UUID/GUID-compatible identifiers;
- external API identifiers are opaque to clients; clients cannot infer authorization from numeric sequence.

## Domain services / policies

### `FiscalDocumentSelector`

Inputs:
- issuer profile;
- receiver fiscal profile;
- transaction jurisdiction;
- goods/services/mixed context;
- amount/currency;
- own-account/account-on-behalf;
- correction/reference context;
- contingency state;
- active fiscal rule version.

Output:
- eligible/required CFE family;
- required receiver fields;
- rule IDs/source/version;
- validation errors/warnings.

Client-supplied desired CFE type is at most an intent hint and cannot override the result.

### `ReceiverIdentityResolver`

Resolves typed national/foreign identities without collapsing nationality, residence, tax residence, issuing country and RUC possession.

### `CrossBorderTaxTreatmentResolver`

Determines domestic/export/special treatment from transaction facts and versioned rules. `customer.country != UY` is never sufficient evidence of export.

### `TaxCalculator`

Calculates line/tax/rounding results using versioned rules and exact decimals. The fiscal snapshot stores inputs/results/rule identity used at issuance.

### `FiscalNumberAllocator`

Domain/application policy protecting CAE applicability and next-number rules; concrete locking/storage is Infrastructure.

### `CreditPolicy`

Conditional Release-1 policy for credit limit/approval/overdue handling. Remains configurable and auditable.

### `InventoryAvailabilityPolicy`

Applies approved negative-stock/backorder rules. It must not be hard-coded in controllers.

### `CostingPolicy`

Supports approved PPP/FIFO method selection and effective policy version when enabled.

### `RetentionResguardoPolicy`

Determines when retention/perception is represented in supporting CFE vs standalone e-Resguardo, using regulatory rule provenance.

## Temporal rules

Use UTC instants for technical/audit ordering and explicitly modeled local business/fiscal dates where DGI/business rules require local date semantics. Do not infer fiscal date from server local timezone implicitly.
