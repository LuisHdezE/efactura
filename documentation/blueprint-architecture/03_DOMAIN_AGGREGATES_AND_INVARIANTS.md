# Domain Aggregates and Invariants

## Aggregate design rule

Aggregates protect transactional invariants. They are not one-to-one mirrors of database tables or UI screens. Large workflows may coordinate several aggregates through application use cases.

## Organization

### `Company`

Owns issuer identity/configuration references and lifecycle.

Key invariants:
- fiscal identity changes do not rewrite historical fiscal snapshots;
- exactly one effective issuer profile is selected for an issuance context;
- configuration changes are audited.

### `Location`

Represents branch/establishment/operational location.

### `Terminal`

Represents POS/cash-register/device server-side registration and location association. A terminal does not create an independent fiscal numbering universe.

## IdentityAccess

### `UserAccount`

Owns application user status and external/local authentication identity link.

### `Role`

Owns permission composition. User-role/location assignments may be separate assignment entities/aggregates depending on concurrency needs.

Invariant: authentication identity never grants a business permission merely by existing.

## Parties

### `Party`

One canonical person/legal entity with roles such as CUSTOMER and SUPPLIER.

Contains/owns:
- names/legal name;
- fiscal identities;
- residence and tax-residence context;
- addresses;
- contact points;
- active roles.

Invariants:
- fiscal identity is typed and issuing-country-aware;
- one `IsForeign` boolean is insufficient and is forbidden as authoritative tax logic;
- historical transactions store snapshots instead of reading mutable current party data.

Optional separate aggregate: `CustomerCreditProfile` for credit limit/approval concurrency if Release-1 policy requires it.

## Catalog

### `CommercialItem`

Kinds: PRODUCT or SERVICE, with `TrackInventory` independent from sellability.

Owns:
- code/name/description;
- item kind;
- unit of measure;
- inventory behavior;
- tax-profile reference;
- active status.

Invariant: service-only businesses do not require stock setup; mixed sales are valid.

## Sales

### `Sale`

Owns:
- seller/location/terminal context;
- customer/receiver reference and immutable commercial snapshot;
- lines with item/description/unit/price/tax-input snapshots;
- discounts/surcharges;
- payment terms;
- totals;
- commercial state.

Proposed states:

`DRAFT -> VALIDATED -> CONFIRMED`

with explicit cancellation/void behavior only before the irreversible fiscal/financial boundary allowed by policy.

Invariants:
- confirmed line/price/customer snapshots are immutable except through explicit correction workflow;
- total equals deterministic sum/rounding policy;
- retry cannot confirm twice;
- fiscal document state is not stored as the Sale state machine itself.

## Fiscal

### `FiscalDocument`

Represents a CFE and its immutable issuance snapshot.

Owns:
- fiscal type/code/version;
- company-wide number identity;
- issuer/receiver snapshots;
- lines/taxes/totals;
- references/corrections;
- generation/signature/artifact metadata;
- fiscal lifecycle state;
- rule/specification provenance.

Transport/envelope state may be separate entities associated with the document so receipt and final acknowledgement remain distinct.

Invariants:
- fiscal number is never reused;
- accepted/non-rejected document is not destructively edited/deleted;
- corrections use permitted referenced document families;
- client cannot force an ineligible CFE type;
- fiscal decision has rule/version/source evidence.

### `CaeAuthorization`

Owns an authorized CFE range/series/type, validity, operational allocations/subranges and consumption state.

Invariants:
- company-wide uniqueness by CFE type/series/number;
- branch/cash allocation may partition operational consumption but cannot violate global uniqueness;
- expired/exhausted range cannot allocate a new number;
- allocation is concurrency-safe and audited.

### `FiscalContingencyDocument`

Represents CFC identity and recovery/reporting lifecycle.

Invariant: CFC identity is not replaced by an unrelated normal CFE number on reconnection.

### `ReceivedFiscalDocument`

Owns original received XML/hash/source, validation findings, duplicate identity and linkage to procurement/payables.

## Inventory

### `InventoryPosition`

Owns current quantity for `item + location`, with portable application-managed concurrency version.

### `StockMovement`

Immutable business fact for receipt/sale/adjustment/transfer/return/etc. It is append-oriented; corrections use compensating movement.

### `StockTransfer`

Owns transfer workflow: requested/approved/dispatched/received/discrepancy resolution.

## Procurement

### `PurchaseOrder`

Owns supplier, lines, requested quantities/prices, approval/status.

### `GoodsReceipt`

Owns actual received quantities and discrepancies. Posting creates inventory effects via the application orchestration boundary.

## Receivables

### `Receivable`

Owns original obligation, due date, adjustments and allocation references.

Invariant: balance is derived from obligation + valid adjustments - allocations; partial collections never overwrite history.

## Payables

### `Payable`

Supplier-side equivalent with source evidence and allocation history.

## Treasury

### `Payment`

Represents a customer collection or supplier payment instrument/transaction with amount, currency, medium, external reference and lifecycle.

### `PaymentAllocation`

May be an entity under Payment or obligation depending on chosen concurrency implementation. Must preserve many-to-many allocation history when one payment covers multiple obligations.

## CashManagement

### `CashShift`

Owns open/close times, operator/terminal, expected totals, counted totals, variances and reconciliation state.

Cash movements are append-only evidence linked to financial source where applicable.

## Sync

### `ClientOperation`

Owns `clientOperationId`, device/user context, command type, payload hash, processing state and canonical response identity.

Invariant: same operation ID + different material payload is a conflict, not a second command.

### `SyncBatch`

Tracks batch/resume/dependency result metadata; it is not required to make all contained commands one transaction.

## Audit

`AuditEvent` is immutable append-oriented evidence rather than ordinary CRUD aggregate state.

Required context when applicable:
- actor;
- organization/location/device;
- event type;
- entity/aggregate reference;
- before/after or immutable operation context;
- reason;
- occurred/recorded time;
- request/correlation/idempotency identifiers.
