# 58 — Fiscal Sobre Transport

Status: ACCEPTED IMPLEMENTATION

Accepted baseline: `main@356249ed93938561bed22abbf21f3f87090b9a53`
(merge of PR #85, `feat(fiscal): add durable Sobre transport`).

Approved PR #85 head: `12ef57df698ddb08f0f2a035b11991380e5bf613`.
Post-merge Clean Architecture Guard #398 (`34742965004`) completed successfully for Build/Architecture/CrossCutting/Legacy and PostgreSQL/MySQL provider-real transaction tests.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Purpose

This bounded increment adds a durable, fail-closed transport lifecycle for an already persisted and validated CFE Sobre. It crosses the DGI `EFACRECEPCIONSOBRE` boundary without interpreting the returned `ACKSobre` business state.

The slice is intentionally split from ACK semantics. Its responsibility ends when a trustworthy `Dataout/xmlData` payload has been received and stored as opaque evidence, or when the delivery outcome becomes ambiguous and must be reconciled before any retry.

## Authoritative DGI evidence

Current DGI `Documentos de interés` continues to publish or link the governed material used by this slice:

- `Formato_Sobre_v05`;
- `XSDs_FE_V1.44.2`;
- `Web Services Externos Recepción`;
- `Web Services Externos Consultas`;
- `WS_eFactura.wsdl`.

Registry:

`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

The DGI **Web Services Externos Recepción** document remains linked from that current registry and defines the reception operation `EFACRECEPCIONSOBRE` with:

- input wrapper `Datain`;
- child `xmlData`;
- `EnvioCFE` placed directly inside `xmlData` CDATA;
- output wrapper `Dataout`;
- returned Sobre response inside `Dataout/xmlData`.

Reception document:

`https://www.efactura.dgi.gub.uy/files/DocumentoServiciosWebExternos?es=`

The separately published current **Web Services Externos Consultas** document contains a UTF-8 -> GZIP -> Base64 routine for consultation payload handling. That consultation-specific routine is not authoritative evidence that `EFACRECEPCIONSOBRE` must gzip or Base64-wrap the `EnvioCFE` request.

Consultation document:

`https://www.efactura.dgi.gub.uy/files/web-services-externos-consultas-pdf?es=`

Therefore this slice sends the exact durable `EnvioCFE` XML directly in CDATA and deliberately contains no `GZipStream` or reception Base64 framing.

## SOAP and WS-Security boundary

`DgiWsSecurityFiscalCfeEnvelopeTransportGateway` is an Infrastructure adapter only.

It:

- uses SOAP 1.1;
- invokes operation `WS_eFactura.EFACRECEPCIONSOBRE`;
- builds `Datain/xmlData` with a CDATA node containing the exact durable envelope string;
- uses an X509v3 `BinarySecurityToken`;
- signs the SOAP Body identified by `wsu:Id`;
- uses exclusive canonicalization;
- isolates DGI's legacy SOAP transport `RSA-SHA1` and `SHA1` algorithms to this adapter;
- never reuses those SHA1 algorithms as CFE/XMLDSig policy;
- requires endpoint and SOAPAction from external configuration rather than inventing environment URLs.

Configuration section:

`FiscalTransport:CfeEnvelope`

Required settings:

- `Endpoint`: absolute HTTPS URI;
- `SoapAction`: explicit configured SOAPAction.

Optional:

- `TimeoutSeconds`: 5..180, default 60.

The transport adapter reuses the existing infrastructure-only PFX certificate source already accepted for DGI WS-Security transport. Private-key access remains in Infrastructure.

## Exact durable bytes before dispatch

Application dispatch never rebuilds an envelope.

Before transport it reloads the durable `StoredFiscalCfeEnvelope`, verifies that:

- the durable envelope identity exists;
- `EnvelopeXml` is non-empty;
- `EnvelopeSha256` has SHA-256 shape;
- recomputing SHA-256 over the persisted UTF-8 string exactly matches `EnvelopeSha256`;
- the prepared submission still points to that same durable hash.

The gateway receives the persisted XML and hash. Infrastructure rechecks the request hash before building SOAP.

## Durable transport state

A separate submission record is persisted in `v1_fiscal_cfe_envelope_submissions`.

States are deliberately transport-only:

- `Prepared`;
- `InFlight`;
- `ResponseReceived`;
- `Unknown`.

`ResponseReceived` means only that a structurally trustworthy SOAP response supplied non-empty `Dataout/xmlData`. It does **not** mean DGI accepted the Sobre.

This accepted slice deliberately does not define `Accepted`, `Rejected`, `AS`, `BS`, `S08` or any other ACK business state inside transport.

## Prepare and replay identity

One transport intent is unique by durable `EnvelopeId`.

A separate `OrganizationId + OperationId` unique key supports operation replay.

Preparation:

1. requires an existing durable Sobre;
2. validates the durable envelope hash;
3. returns an existing matching operation replay when present;
4. returns the existing transport intent for the same durable envelope when present;
5. otherwise creates one `Prepared` row.

Provider unique constraints remain the final concurrent-prepare serialization guard. Provider-specific conflict recognition remains isolated in Infrastructure through the accepted envelope persistence conflict classifier.

## Dispatch safety

Dispatch uses the persisted submission row as the transactional serialization anchor.

Inside a local transaction it:

1. reloads the durable envelope;
2. obtains the transport row by `EnvelopeId` using provider-real `FOR UPDATE` when a transaction is active;
3. rejects `InFlight` and `Unknown` as `reconciliation_required`;
4. replays `ResponseReceived` without crossing the network;
5. changes `Prepared -> InFlight`;
6. increments attempt count;
7. persists the state before any HTTP call.

Only after that transaction commits does Application invoke the gateway.

This prevents two concurrent dispatchers from safely crossing the DGI network boundary for the same durable Sobre.

## Delivery outcomes

A trustworthy response with non-empty `Dataout/xmlData` is persisted as:

- `State = ResponseReceived`;
- exact returned payload in `ResponseXml`;
- SHA-256 in `ResponseSha256`;
- completion timestamp.

The response payload remains opaque at this accepted transport boundary. Its root, state, receiver id and rejection reasons are not interpreted here.

A pre-network request-build/configuration error marked non-ambiguous may return the intent to `Prepared` with a failure code.

Once network delivery may have occurred, any uncertain outcome becomes `Unknown`, including:

- HTTP/network failure after send begins;
- response-read failure;
- non-success HTTP status;
- malformed/empty SOAP response after DGI may have received the request;
- cancellation after dispatch;
- unexpected failure after crossing the dispatch boundary.

`Unknown` is never automatically retried because DGI may already have received the exact bytes.

## Opaque response boundary

The gateway parses only enough SOAP structure to locate `Dataout/xmlData` safely.

It does not inspect or require:

- `ACKSobre` root identity;
- `Estado`;
- `IDReceptor`;
- `AS` / `BS`;
- `S01..Sxx` reasons;
- `S08` duplicate semantics.

That separation remains deliberate. Pending PR #86 / document 59 proposes append-only semantic observation of the already durable response while preserving this transport evidence unchanged.

## Persistence model

`v1_fiscal_cfe_envelope_submissions` stores:

- internal submission id;
- durable envelope id;
- organization, issuer, receiver and explicit `Idemisor` correlation evidence;
- operation id;
- exact durable envelope SHA-256;
- transport state;
- attempt count;
- prepared/last-attempt/completed timestamps;
- opaque DGI response XML;
- response SHA-256;
- failure code.

Unique provider constraints:

- `UX_v1_fces_operation` on `OrganizationId + OperationId`;
- `UX_v1_fces_envelope` on `EnvelopeId`.

The transport row also has a `Restrict` FK to the durable Sobre so accepted transport evidence cannot become orphaned.

The repository owns no transaction and no `SaveChanges`; Application owns transaction/UoW policy.

## Validation coverage

Accepted validation proves that:

- the SOAP operation is exactly `WS_eFactura.EFACRECEPCIONSOBRE`;
- the exact durable `EnvioCFE` is present as one CDATA payload;
- X509 token and body signature are present and cryptographically valid;
- the response remains opaque even if it contains ACK-looking state/reason values;
- PostgreSQL and MySQL persist the opaque response and SHA-256;
- completed replay performs no second network call;
- ambiguous delivery becomes `Unknown`;
- a second dispatch from `Unknown` is refused before network;
- concurrent PostgreSQL/MySQL dispatchers cross the network boundary only once;
- provider repository locking is implemented for both Npgsql and MySQL;
- the submission-to-envelope FK works provider-real with `Restrict`;
- Application imports no HTTP, private-key, gzip or provider-specific dependencies;
- DI exposes the transport clock, gateway, repository and prepare/dispatch use cases.

Exact-head Guard #397 (`34742474493`) passed before merge. Accepted `main@356249ed93938561bed22abbf21f3f87090b9a53` then passed post-merge Guard #398 (`34742965004`) for both jobs.

## Deliberate non-scope

This accepted transport increment does not itself implement:

- semantic `ACKSobre` parsing;
- accepted/rejected CFE/Sobre business state inside the transport row;
- `AS` / `BS` interpretation;
- `S08` interpretation or recovery;
- DGI receiver-id semantic interpretation;
- automatic reconciliation query after `Unknown`;
- automatic retry of ambiguous delivery;
- automatic `Idemisor` allocation;
- CFE grouping/batching policy;
- ER inconsistency-detail retrieval;
- Reporte Diario `R05` recovery;
- external DGI Testing acceptance;
- Production enablement.

Pending PR #86 / document 59 is a separate append-only ACK observation capability. It does not rewrite this accepted transport lifecycle.

Local successful transport is not evidence of DGI certification or Production readiness.
