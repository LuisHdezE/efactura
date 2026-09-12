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

## Ordering

`SecEnvio N+1` cannot be prepared until the durable submission for `N` has immediate DGI state `AR`.

This is deliberately stricter than merely requiring a locally signed previous artifact. It prevents a locally allocated correction sequence from being transmitted ahead of the report DGI actually received.

## Immediate acknowledgement

The gateway parses only the immediate `ACKRepDiario` returned by `EFACRECEPCIONREPORTE`:

- `AR` -> local `Received`;
- `BR` -> local `Rejected`.

`IDReceptor` and the raw `ACKRepDiario` XML are persisted as evidence.

This increment does **not** yet implement later consultation/reconciliation states such as `DR`, `ER` or `FR`, and it does not independently validate the DGI XML signature inside the returned ACK. The raw ACK is therefore transport evidence, not the final certification evidence model.

## Ambiguous delivery and retries

A timeout, connection loss, response-read failure, malformed successful response, HTTP error after dispatch, or cancellation after the durable `InFlight` transition is treated as `Unknown` when DGI may have received the message.

`Unknown` is never retried automatically. A later reconciliation slice must query DGI using the durable receiver/report evidence before another send is allowed.

Failures known to occur before network dispatch, such as invalid external transport configuration, certificate resolution failure or SOAP construction failure, can return the submission to `Prepared`.

## Important BR / same-SecEnvio gap

DGI documentation states that a Reporte Diario rejected as `BR` does not count as a received sequence and, except for the sequence-specific R05 condition, must be corrected and resent with the **same `SecEnvio`**.

The current signed-report durability model created before transport has one immutable signing evidence/artifact per:

`Organization + RUC + FechaResumen + SecEnvio`

That model cannot yet create corrected signed bytes for the same `SecEnvio` after a `BR` without weakening its immutability guarantees.

Therefore this increment deliberately persists `BR` and stops. It does **not** claim the same-sequence correction/resubmission capability is complete. A subsequent bounded increment must introduce a local artifact-attempt/revision identity beneath the stable DGI `SecEnvio`, preserve the rejected artifact and ACK, and allow a newly signed corrected artifact to reuse the same DGI sequence only when the rejection evidence authorizes that behavior.

## Deliberate non-scope

This increment does not implement:

- automatic corrected resend after `BR`;
- R05-specific sequence recovery;
- `EFACCONSULTARRESPUESTAREPORTE` reconciliation;
- `DR` / `ER` / `FR` lifecycle;
- independent cryptographic validation of DGI ACK signatures;
- Sobre v05 packaging;
- live BCU quotation acquisition;
- Production enablement.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
