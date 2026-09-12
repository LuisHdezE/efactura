# Fiscal Daily Report SecEnvio lifecycle and reliquidation lineage

## Regulatory boundary

This increment implements the sequence semantics already pinned in the byte-preserved DGI `ReporteDiarioCFE.xsd` used by the project. The schema annotation for `SecEnvio` states that the first daily submission uses sequence 1 and a correction must resend the complete file with `SecEnvio = previous sequence + 1`. The field is restricted to two digits.

The XSD itself permits numeric zero, but the semantic annotation says the first submission is 1. The application therefore fails closed on `1..99` and never allocates zero.

## Lifecycle model

A Reporte Diario version is scoped by:

- internal organization;
- issuer RUC;
- `FechaResumen`;
- `SecEnvio`.

`AllocateFiscalDailyReportVersionUseCase` accepts only a **complete** frozen document/annulment population. It does not expose a patch API.

The first version for one issuer/day is always:

- `SecEnvio = 1`;
- revision kind `Initial`;
- no previous version;
- fixed internal reason `initial`.

Every later version:

- must be `Correction` or `FxReliquidation`;
- uses exactly `previous SecEnvio + 1`;
- points to the immediately previous durable version;
- requires a caller-supplied auditable reason code;
- cannot be allocated once sequence 99 is reached.

## Idempotency and concurrency

Each allocation carries an organization-scoped `OperationId`. Replaying the same operation with identical immutable input returns the original version. Reusing the operation id with different input fails closed.

The EF repository acquires a transaction-scoped row lock on the organization's fiscal profile before reading the latest report version. This deliberately coarse lock serializes the very low-frequency daily-report sequence decision and prevents two concurrent first versions from both observing an empty history.

Database uniqueness is additionally enforced on:

- organization + RUC + summary date + sequence;
- organization + operation id.

## Correction prerequisite

A new sequence after the first requires a durable signed artifact for the immediately previous version. This prevents generating an arbitrary chain of unsigned corrections.

Transport is still not implemented. A later transport slice must strengthen the send-order rule by refusing to submit version N+1 unless the required submission/response evidence for version N exists.

## FX reliquidation

The existing source-fact model can mark a report as requiring future FX reliquidation. This lifecycle persists that marker per version.

A version explicitly classified as `FxReliquidation` is allowed only when:

1. the immediately previous version is marked as requiring FX reliquidation; and
2. the complete replacement snapshot no longer carries unresolved reliquidation evidence.

The lifecycle does not acquire BCU quotations and does not reinterpret the existing FX evidence rules.

## Audit and durability

Every new version persists:

- previous-version identity;
- operation id;
- revision kind and reason code;
- reconciliation fingerprint;
- pending-FX-reliquidation marker;
- creation timestamp;
- tamper-evident version fingerprint.

Allocation, audit evidence and outbox event are committed in one local transaction.

## Deliberate non-scope

This increment does **not** implement:

- `EFACRECEPCIONREPORTE` transport;
- DGI acknowledgement lifecycle;
- transport retry policy;
- Sobre v05 packaging;
- live BCU acquisition;
- automatic regulatory decision that a generic correction is required;
- Production enablement.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
