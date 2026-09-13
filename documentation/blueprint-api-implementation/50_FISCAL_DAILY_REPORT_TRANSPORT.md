# Fiscal Daily Report transport foundation

## Scope

This increment introduces the first bounded transport capability for a previously signed and durably persisted Reporte Diario. It does not create or mutate the fiscal report payload during transport.

The Application layer owns durable submission intent, idempotency, ordering and lifecycle decisions. Infrastructure owns DGI SOAP/WS-Security mechanics and external configuration.

## DGI transport boundary

The implementation targets the published DGI external web-service operation `EFACRECEPCIONREPORTE`.

The request shape is treated as:

- SOAP 1.1 envelope;
- DGI operation `WS_eFactura.EFACRECEPCIONREPORTE`;
- `Datain/xmlData` containing the already signed Reporte Diario XML;
- WS-Security X509v3 `BinarySecurityToken`;
- XML signature over the SOAP Body.

The DGI web-service specification historically documents exclusive canonicalization, RSA-SHA1 and SHA1 for this **SOAP transport signature**. Those legacy algorithms are isolated inside `DgiWsSecurityFiscalDailyReportTransportGateway` and are not fiscal-document signature policy. The Reporte Diario document XMLDSig remains governed separately by `XmlDsigFiscalDailyReportSignatureProvider`, which uses SHA-256 evidence and rejects SHA1.

Neither the endpoint nor SOAPAction is inferred from unofficial examples. They are mandatory external configuration:

- `FiscalTransport:DailyReport:Endpoint` must be an absolute HTTPS URI;
- `FiscalTransport:DailyReport:SoapAction` must be supplied explicitly;
- optional `FiscalTransport:DailyReport:TimeoutSeconds` is constrained to 5..180 seconds.

The transport adapter reuses the organization-scoped, externally configured PFX certificate source. No PFX, password or private key is committed.

## Durable submission lifecycle

A submission is uniquely tied to one durable signed artifact and the DGI report identity:

`Organization + RUC + FechaResumen + SecEnvio`

States in this increment are:

- `Prepared`: durable intent exists but no network attempt is in flight;
- `InFlight`: the attempt counter and dispatch timestamp were persisted before crossing the network boundary;
- `Received`: immediate `ACKRepDiario` returned `AR`;
- `Rejected`: immediate `ACKRepDiario` returned `BR`;
- `Unknown`: transport may have reached DGI, but no trustworthy immediate response was durably completed.

`OperationId` makes preparation idempotent. An existing identity cannot silently be rebound to different signed bytes.

## Concurrency boundary

The durable `Prepared -> InFlight` transition is serialized at the submission row before the network boundary. PostgreSQL and MySQL use an EF Core `FromSqlInterpolated` `SELECT ... FOR UPDATE` path while a transaction is active; normal reads remain no-tracking queries.

This prevents two independent dispatch workers from reading the same durable `Prepared` submission and both sending the same signed Reporte Diario. Provider-real tests deliberately hold the first dispatcher inside the gateway while a second dispatcher attempts the same identity. The second worker observes the already committed `InFlight` state and fails closed with `fiscal.daily_report.transport.reconciliation_required`; only one gateway call occurs and the persisted attempt count remains one.

The PostgreSQL lock parameter for `SummaryDate` is bound as `DateOnly`, preserving the database `date` semantics and avoiding accidental `timestamp with time zone` inference.

## Portable signing timestamp evidence

The signed Reporte Diario bytes contain `TmstFirmaEnv`, whose lexical value includes the Uruguay fiscal offset, for example `-03:00`. PostgreSQL `timestamp with time zone` requires a UTC instant and does not retain the original offset as independent evidence.

To keep replay byte-identical across PostgreSQL and MySQL, durable Reporte signing evidence and signed-artifact persistence store:

- the signing instant normalized to UTC in `SigningTimestamp`;
- the original fiscal offset separately as `SigningOffsetMinutes`.

Repository reads reconstruct the original `DateTimeOffset` before rebuilding or validating the deterministic Reporte. This preserves the exact fiscal signing offset used by `TmstFirmaEnv` without depending on provider-specific timestamp behavior.

Provider-real regression coverage verifies the same UTC-plus-offset persistence rule for the root signed report and for BR correction revisions.

## Ordering

`SecEnvio N+1` cannot be prepared until `N` has durable DGI `AR` evidence.

That acceptance can now come from either:

- the original submission for `N` in local `Received` state with `AckStateCode = AR`; or
- an accepted same-`SecEnvio` BR correction revision for `N`.

The original rejected submission is never rewritten into AR. Accepted correction evidence is read independently through `IFiscalDailyReportSameSequenceReceiptReader`.

This remains deliberately stricter than merely requiring a locally signed previous artifact. It prevents a locally allocated next sequence from being transmitted ahead of the report DGI actually accepted.

## Immediate acknowledgement

The gateway parses only the immediate `ACKRepDiario` returned by `EFACRECEPCIONREPORTE`:

- `AR` -> local `Received`;
- `BR` -> local `Rejected`.

`IDReceptor` and the raw `ACKRepDiario` XML are persisted as evidence.

For BR correction authorization, typed rejection reasons are parsed behind an Infrastructure port and preserved separately without modifying the original ACK bytes. The accepted same-sequence correction lifecycle is documented in `51_FISCAL_DAILY_REPORT_BR_SAME_SEQUENCE_CORRECTION.md`.

The accepted bounded consultation capability for a **known durable `IdReceptor`** is documented separately in `52_FISCAL_DAILY_REPORT_RESPONSE_CONSULTATION.md`. It retrieves the original ACK without redefining this root transport lifecycle.

The pending receiver-discovery capability for `Unknown` targets without a durable `IdReceptor` is documented separately in `53_FISCAL_DAILY_REPORT_RECEIVER_DISCOVERY.md`. It discovers receiver evidence but still does not rewrite this root transport lifecycle.

This transport foundation still does not implement later reconciliation states such as `DR`, `ER` or `FR`, and it does not independently validate the DGI XML signature inside the returned ACK. Raw ACK material therefore remains evidence whose state-changing interpretation is separately governed.

## Ambiguous delivery and retries

A timeout, connection loss, response-read failure, malformed successful response, HTTP error after dispatch, or cancellation after the durable `InFlight` transition is treated as `Unknown` when DGI may have received the message.

`Unknown` is never retried automatically.

If a trustworthy durable `IdReceptor` is available, the accepted consultation slice in document 52 can retrieve the original response as append-only evidence. If no receiver id is known, `EFACCONSULTARRESPUESTAREPORTE` alone cannot discover the submission. PR #80 proposes the separately governed `EFACCONSULTARENVIOSREPORTE` receiver-discovery evidence chain described in document 53.

Failures known to occur before network dispatch, such as invalid external transport configuration, certificate resolution failure or SOAP construction failure, can return the submission to `Prepared`.

## BR same-SecEnvio follow-up

The former BR same-sequence gap is addressed by the accepted lifecycle in:

`documentation/blueprint-api-implementation/51_FISCAL_DAILY_REPORT_BR_SAME_SEQUENCE_CORRECTION.md`

That lifecycle preserves the rejected root submission/artifact and ACK, creates immutable local correction revisions beneath the stable DGI identity, signs each corrected artifact with new evidence, keeps the same `SecEnvio` only when durable BR evidence permits it, and blocks `R05` fail-closed for separate reconciliation.

It does not redefine this root transport lifecycle and does not convert a rejected root row into a received row.

## Validation evidence

The accepted transport/correction/consultation lineage has Clean Architecture Guard coverage including:

- full solution build;
- Clean Architecture guards;
- API v1 cross-cutting tests;
- legacy unit tests;
- PostgreSQL and MySQL transactional persistence tests;
- portable Reporte signing timestamp round-trip with Uruguay `-03:00` evidence;
- AR/BR submission persistence;
- concurrent-dispatch serialization proving a single network-boundary call;
- BR correction revision lineage, replay and R05 fail-closed behavior;
- accepted-correction N+1 ordering;
- provider-real local-revision uniqueness;
- known-`IdReceptor` original-response consultation, replay and provider-real persistence.

PR #79 was accepted at `main@85fe095449f7b4e9bfb97b45e01ae283b8071913`, with post-merge Clean Architecture Guard #312 (`34727894835`) successful.

The receiver-discovery candidate in PR #80 must pass its own exact-final-head Clean Architecture Guard before review or merge.

## Deliberate non-scope

The accepted transport lineage still does not implement:

- automatic R05 sequence recovery;
- accepted receiver-id discovery through `EFACCONSULTARENVIOSREPORTE` when no durable `IdReceptor` exists; PR #80 is still pending;
- `DR` / `ER` / `FR` reconciliation lifecycle;
- automatic retry from `Unknown`;
- independent cryptographic validation of DGI ACK signatures;
- Sobre v05 packaging;
- live BCU quotation acquisition;
- Production enablement.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
