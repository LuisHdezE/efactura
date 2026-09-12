# Fiscal Daily Report BR same-SecEnvio correction lifecycle

## Scope

This increment closes the bounded Reporte Diario gap created when DGI returns immediate state `BR` for a submitted report and the rejection evidence authorizes correction with the same DGI `SecEnvio`.

The stable DGI identity remains:

`Organization + RUC + FechaResumen + SecEnvio`

The implementation does not mutate, replace or reinterpret the original rejected submission. Instead, it introduces an explicit local correction-revision lineage below that stable DGI identity.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Regulatory boundary

Current DGI response guidance distinguishes:

- `AR`: Reporte Recibido;
- `BR`: Reporte Rechazado;
- rejection reasons `R01` through `R06`;
- up to 30 rejection reasons per response;
- rejection `Glosa` up to 100 characters;
- optional rejection `Detalle` up to 500 characters.

For an ordinary `BR`, DGI does not count the rejected report as the accepted sequence and the issuer corrects and resends using the same `SecEnvio`.

`R05` is deliberately treated differently. It means the reported sequence is not correct. This increment therefore does not infer a same-sequence recovery policy for `R05`; it fails closed with explicit reconciliation required.

No behavior for `R05` is invented beyond that bounded safety rule.

## Immutable root evidence

The original submitted Reporte Diario remains immutable after `BR`:

- original submission identity;
- original signed-artifact identity;
- original signed XML bytes;
- original signed-content hash;
- original immediate DGI ACK bytes;
- original receiver evidence;
- original transport terminal state.

A correction never rewrites the rejected artifact and never changes its ACK to simulate success.

This separation is essential because the first rejected artifact is still part of the fiscal audit trail even when a later corrected revision is accepted.

## Local correction revision identity

Correction attempts use a local revision identity while retaining the same DGI sequence.

The first corrected artifact is `LocalRevision = 2`. Later corrected artifacts increment that local revision and form an explicit lineage through `PreviousRevisionId`.

Each local correction revision preserves:

- `RootSubmissionId`;
- `RootSignedArtifactId`;
- optional `PreviousRevisionId`;
- new `SigningEvidenceId`;
- new `SignedArtifactId`;
- stable Organization/RUC/FechaResumen/SecEnvio;
- local revision number;
- operation id;
- correction reason;
- source BR ACK hash and typed rejection-reason evidence;
- projection, unsigned and signed SHA-256 hashes;
- signing timestamp and original fiscal offset evidence;
- certificate/signature metadata;
- XSD validation metadata;
- signed XML;
- immutable revision fingerprint;
- independent transport lifecycle fields.

The persisted immutable fingerprint prevents later transport-state updates from altering the correction's fiscal identity or signing evidence.

## BR acknowledgement evidence

DGI response XML parsing stays behind the Infrastructure port `IFiscalDailyReportBrAckEvidenceParser`.

The parser:

- requires `ACKRepDiario` in the DGI CFE namespace;
- requires state `BR` for correction authorization;
- parses direct `Detalle/MotivosRechazo` evidence;
- accepts only `R01..R06`;
- enforces the bounded cardinality and field lengths;
- uses secure XML reader settings with DTD prohibited and external resolution disabled.

The Application layer consumes typed rejection evidence and persists it as durable JSON alongside the hash of the exact source ACK XML. The raw original ACK remains unchanged on the root submission.

## R05 fail-closed policy

If any durable rejection reason is `R05`, same-`SecEnvio` correction is blocked with:

`fiscal.daily_report.br_correction.r05_reconciliation_required`

The implementation does not allocate a replacement sequence, does not reuse the existing sequence automatically and does not infer DGI state. Sequence recovery belongs to a separate evidence-backed reconciliation capability.

## Preparation and replay

Preparation is idempotent by `OrganizationId + OperationId`.

On first execution, the use case:

1. transaction-locks the root submission identity;
2. identifies the latest authoritative BR source, either the root rejection or the latest rejected correction revision;
3. rejects AR, InFlight and Unknown sources that cannot authorize a new correction;
4. parses and validates durable BR rejection evidence;
5. blocks `R05`;
6. builds corrected unsigned report XML for the same `SecEnvio`;
7. creates new signing evidence and signs exactly once;
8. validates the resulting signed XML and schema evidence;
9. persists the new immutable correction revision.

Replaying the same operation returns the persisted correction revision. It validates durable evidence rather than signing again.

## Transport lifecycle

Each correction revision has its own transport lifecycle:

`Prepared -> InFlight -> Received | Rejected | Unknown`

The same durable transport rules used for the root submission remain in force:

- `InFlight` is persisted before crossing the network boundary;
- `AR` yields local `Received`;
- `BR` yields local `Rejected` and preserves typed rejection reasons;
- ambiguous post-dispatch failures become `Unknown`;
- `Unknown` is never automatically retried;
- terminal correction revisions do not call the gateway again on replay.

A rejected correction revision can authorize another local revision only when its own durable BR evidence permits it. Thus revision 2 may lead to revision 3 under the same `SecEnvio` without mutating revision 2.

## Concurrency and provider persistence

The existing root Reporte Diario submission row remains the serialization anchor. `GetByIdentityAsync` uses provider-real transactional `SELECT ... FOR UPDATE` for PostgreSQL and MySQL while a transaction is active.

Persistence also enforces safety constraints including unique:

- `OrganizationId + OperationId`;
- `OrganizationId + IssuerRuc + SummaryDate + Sequence + LocalRevision`;
- `PreviousRevisionId`;
- `SignedArtifactId`;
- `SigningEvidenceId`.

These constraints prevent parallel local branches from silently creating two different revision-2 artifacts for the same DGI identity even if an application-level race escapes before commit.

Provider-real tests exercise PostgreSQL and MySQL for:

- correction-revision round-trip;
- preservation and rehydration of the Uruguay signing offset;
- typed source rejection evidence and immutable fingerprints;
- accepted-correction receipt lookup;
- rejection of two competing local revision-2 rows for the same DGI identity.

## N+1 gate after corrected AR

`SecEnvio N+1` remains blocked until DGI has durably accepted `N`.

The gate now accepts either:

1. root submission `N` in `Received` with `ACK = AR`; or
2. a same-`SecEnvio` correction revision for `N` in `Received` with `ACK = AR`.

The root rejected submission is not rewritten into AR. The accepted correction is queried through `IFiscalDailyReportSameSequenceReceiptReader`, preserving both histories accurately.

Without either durable acceptance path, preparation of `N+1` continues to fail with the existing previous-sequence-not-received conflict.

## Validation coverage

Cross-cutting coverage includes:

- ordinary BR creates revision 2 under the same sequence;
- operation replay returns the same revision and never signs twice;
- R05 blocks same-sequence correction;
- an AR root cannot create a BR correction;
- Unknown requires reconciliation;
- rejected revision 2 can produce revision 3 with new signed-artifact and signing-evidence identities;
- accepted correction dispatch is terminal and replay does not cross the network boundary twice;
- the original rejected root submission and ACK remain unchanged;
- `N+1` can proceed after an accepted correction and remains blocked otherwise.

Provider-real persistence coverage runs against PostgreSQL and MySQL in the Clean Architecture Guard.

## Deliberate non-scope

This increment does not implement:

- automatic recovery semantics for `R05`;
- `EFACCONSULTARRESPUESTAREPORTE`;
- later `DR`, `ER` or `FR` reconciliation lifecycle;
- automatic retry from `Unknown`;
- independent cryptographic verification of DGI response signatures;
- Sobre v05 packaging;
- Production enablement.

Those capabilities must remain separate bounded increments backed by current authoritative DGI evidence.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
