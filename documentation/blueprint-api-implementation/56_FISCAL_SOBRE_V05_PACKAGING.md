# 56 — Fiscal Sobre v05 Packaging

Status: GOVERNED IMPLEMENTATION CANDIDATE

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Purpose

This bounded increment packages already-signed CFE artifacts into a deterministic DGI `EnvioCFE` / Sobre candidate and validates the complete package against a byte-pinned local XSD closure.

It deliberately stops before persistence and transport. It does not submit a Sobre to DGI, does not allocate `Idemisor`, does not compress/base64 transport content, does not parse an ACK and does not mutate fiscal state.

## Authoritative DGI evidence

The governing functional source is DGI **Formato de Sobre v05**:

`https://www.efactura.dgi.gub.uy/files/Formato_Sobre_v05_pdf?es=`

The current DGI **Documentos de interés** registry separately publishes `XSDs_FE_V1.44.2` for Testing and Production:

`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

Relevant functional rules revalidated for this slice are:

- a Sobre contains a Carátula plus CFE/CFC content;
- an envío incorporates **1..250** CFE/CFC;
- all CFE included in one Sobre must be signed with the **same certificate**;
- the Carátula identifies origin, destination, CFE count, generation timestamp and public certificate information.

No transport endpoint, SOAPAction, retry rule or ACK semantic is inferred from the functional Sobre format.

## Byte-pinned EnvioCFE XSD

The official DGI archive URL listed for `XSDs_FE_V1.44.2` did not return usable archive bytes to the governed runner in the prior schema-import work. The repository therefore uses the same immutable byte-recovery procedure already accepted for the CFE and Reporte Diario closures.

`EnvioCFE.xsd` was recovered from:

- repository: `olagopirez/factible`;
- immutable commit: `9a24a598e9470c4a1c5c34cb0cb253d08ffa3db3`;
- source path: `packages/cfe/spec/xsd/EnvioCFE.xsd`;
- Git blob: `3209b5b854bcbf23694c8ae2561509b568d5d263`;
- SHA-256: `38b411942eda7c229cf4048965654245ba6218f6079a00acfcbe1804e8daa58f`;
- bytes: `3395`.

The file committed in this branch has the exact same Git blob identity, proving byte-for-byte equality with the immutable recovery source. The mirror is not treated as the regulatory authority; DGI remains the authority for the published archive/version and functional format.

The XSD itself carries the historical internal comment `Version: 1.31`. That comment is preserved as source evidence and is not interpreted as replacing either the currently governed DGI XSD archive identity `1.44.2` or functional Sobre format `v05`.

## XSD contract used by this slice

The pinned `EnvioCFE.xsd` requires:

- root `EnvioCFE` in namespace `http://cfe.dgi.gub.uy`;
- root attribute `version="1.0"`;
- one required `Caratula` with `version="1.0"`;
- `RutReceptor`;
- `RUCEmisor`;
- `Idemisor`, integer with up to 10 digits and minimum 0;
- `CantCFE`, integer 1 through 250;
- `Fecha` using DGI `FechaHoraType`;
- `X509Certificate` as base64 binary;
- up to 250 `CFE` elements using `CFEDefType`.

The local Sobre schema closure is recorded in `envelope-schema-manifest.json` and consists of:

- `EnvioCFE.xsd`;
- `CFEType.xsd`;
- `DGITypes.xsd`;
- `xmldsig-core-schema.xsd`;
- `xenc-schema.xsd`;
- `version.txt`.

The validator resolves only embedded resources and prohibits external schema resolution and DTD processing.

## Application boundary

`PackageFiscalCfeEnvelopeUseCase` receives explicit:

- `OrganizationId`;
- `ReceiverRut`;
- `IssuerRuc`;
- `SenderEnvelopeId` (`Idemisor`);
- `CreatedAt`;
- ordered fiscal document ids.

The use case does not invent or allocate those values.

For each requested fiscal document it requires an already durable `StoredFiscalSignedArtifact`. Before packaging it revalidates:

- exact organization and fiscal-document association;
- signed XML SHA-256 against `SignedContentHash`;
- signed-CFE validity against the already accepted pinned CFE validator;
- persisted CFE schema id/version/fingerprint against the current pinned validator;
- non-empty certificate thumbprint and serial evidence.

Duplicate document ids fail validation. Missing signed artifacts or corrupt persisted evidence fail closed.

## Same-certificate rule

The Application layer requires all selected artifacts to carry the same durable certificate thumbprint and serial number.

The Infrastructure builder independently parses the public `ds:X509Certificate` embedded by the accepted XMLDSig signer and verifies that:

- every signed CFE contains exactly one embedded certificate;
- certificate thumbprint/serial agree with the durable signed-artifact metadata;
- all included CFE contain the same DER certificate bytes;
- each CFE `RUCEmisor` matches the requested Sobre issuer RUC.

No private key is accessed by this slice.

## Signed subtree preservation

A persisted signed CFE is a standalone XML document and may carry its own XML declaration. A nested CFE inside `EnvioCFE` cannot carry a second XML declaration.

The builder therefore removes only the standalone XML declaration and leading document-level whitespace before embedding. The complete `CFE` root fragment is then written raw with no parse-and-reserialize cycle. Fiscal content and `ds:Signature` bytes inside the CFE root fragment are not rewritten by the packager.

The source `StoredFiscalSignedArtifact.SignedXml` is never modified or persisted again.

## Deterministic Sobre output

For the same explicit command and immutable signed CFE source artifacts, the builder deterministically emits:

- UTF-8 XML declaration;
- `EnvioCFE version="1.0"`;
- `Caratula version="1.0"`;
- `RutReceptor`;
- `RUCEmisor`;
- `Idemisor`;
- `CantCFE`;
- `Fecha` as `yyyy-MM-dd'T'HH:mm:sszzz`;
- `X509Certificate` as base64 DER bytes;
- signed CFE root fragments in the requested order.

The result returns the generated XML, SHA-256, common certificate identity and XSD schema evidence.

No `xsi:schemaLocation` is invented because the pinned XSD does not require it and the historical source filename/version identifiers are not promoted into a current runtime contract.

## Architecture boundary

Application depends only on:

- `IFiscalSignedArtifactRepository`;
- `IFiscalSignedCfeSchemaValidator`;
- `IFiscalCfeEnvelopeBuilder`;
- `IFiscalCfeEnvelopeSchemaValidator`.

The packaging use case has no dependency on:

- `IUnitOfWork`;
- `ITransactionManager`;
- HTTP/SOAP gateways;
- private-key/certificate sources;
- DGI ACK parsers;
- mutable fiscal-document repositories.

Infrastructure packaging uses only public certificate parsing, deterministic XML construction and local schema validation.

## Validation coverage

This increment requires proof that:

- a valid source set is packaged without persistence or transport side effects;
- 1..250 CFE are accepted as the governed count range;
- more than 250 is rejected before repository reads;
- duplicate source ids are rejected;
- missing/corrupt signed artifacts fail closed;
- persisted signed XML hash drift fails closed;
- mixed certificates fail closed;
- actual `ds:X509Certificate` evidence matches durable metadata;
- CFE issuer RUC must match the Sobre Carátula issuer;
- only the standalone XML declaration is removed from each embedded signed CFE;
- generated XML is UTF-8;
- the real builder output validates against the byte-pinned `EnvioCFE.xsd` closure;
- architecture guards prevent persistence, transport and private-key drift into the slice.

## Deliberate non-scope

This increment does not implement:

- durable Sobre persistence;
- `Idemisor` allocation/replay semantics;
- grouping/batching policy deciding which business CFEs share an envelope;
- gzip/base64 transport framing;
- DGI web-service submission;
- transport certificate policy;
- DGI Sobre ACK parsing or durable submission state;
- retry/idempotency behavior for a submitted Sobre;
- ER inconsistency-detail retrieval/interpretation;
- automatic R05 sequence recovery;
- external DGI Testing acceptance;
- Production enablement.

Those are separate governed capabilities. In particular, successful local XSD validation is not evidence that a Sobre has been accepted by DGI Testing or Production.
