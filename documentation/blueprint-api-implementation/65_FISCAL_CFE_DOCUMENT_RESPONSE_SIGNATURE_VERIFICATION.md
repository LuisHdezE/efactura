# 65 — Fiscal CFE document-response XMLDSig verification

Status: GOVERNED IMPLEMENTATION CANDIDATE

## Purpose

Add a bounded, append-only cryptographic verification boundary for the exact `ACKCFE` XML already persisted by the accepted `IdReceptor + Token` document-response consultation.

This increment verifies XMLDSig signature mathematics only. It deliberately does not validate PKI Uruguay trust, DGI legal signer identity/habilitation, `ACKCFE_det/Estado` semantics, response completeness, or any local fiscal lifecycle transition.

## Authoritative DGI evidence

The implementation is bounded by the current official DGI response-format and external-service material already used by this consumer:

1. **Formato Mensajes Respuesta v19**, printed 25/11/2025, section `3.3. B.- Detalle de Respuesta: Respuesta a Comprobantes`, defines the per-document response area for the CFE/CFC response message.
2. The same current format, section `3.3. C.- Firma Electrónica`, marks the public electronic certificate and the advanced electronic signature as mandatory and states that the advanced signature covers the whole message.
3. **Servicios Web Externos Factura Electrónica**, code `T-5.020.00.001-004`, version 1.1, printed 23/05/2013 and still published through the current DGI eFactura portal, proves that `WS_eFactura.EFACCONSULTARESTADOENVIO` returns an `ACKCFE` in `DataOut/xmlData`; the official example includes an XMLDSig `Signature` carrying `SignatureValue` and embedded `X509Certificate` evidence.

These sources are sufficient to require cryptographic verification of the whole durable `ACKCFE` before later trust or business-state interpretation. They are not sufficient to collapse signature mathematics, PKI trust and DGI signer authorization into one claim.

## Accepted input boundary

The new use case starts only from an already durable `StoredFiscalCfeEnvelopeDocumentResponseConsultation` created by the accepted PR #95 boundary.

The caller supplies:

- `OrganizationId`;
- the existing consultation `OperationId`.

The use case resolves that exact append-only consultation and revalidates:

- consultation structural integrity;
- exact `ResponseXml` presence;
- exact `ResponseSha256` syntax;
- `SHA-256(ResponseXml) == ResponseSha256`.

No new DGI network request is made.

## XMLDSig verification profile

Infrastructure adapter:

`DgiFiscalCfeEnvelopeDocumentResponseSignatureVerifier`

Profile id:

`dgi-ackcfe-xmldsig-math-v1`

The verifier fails closed unless all of the following hold:

- XML parsing prohibits DTD processing;
- external XML resolution is disabled;
- root is exactly `ACKCFE` in `http://cfe.dgi.gub.uy`;
- exactly one direct child XMLDSig `Signature` exists;
- exactly one embedded `X509Certificate` exists;
- `SignedInfo` uses a bounded local canonicalization/signature algorithm set;
- exactly one `Reference` exists;
- the reference URI is empty (`URI=""`), so no external or fragment target is accepted;
- the transform chain uses only a bounded local set and includes the enveloped-signature transform;
- the embedded certificate exposes an RSA public key;
- `SignedXml.CheckSignature(certificate, verifySignatureOnly: true)` succeeds.

The bounded compatibility set includes the legacy XMLDSig RSA-SHA1/SHA1 tuple because official DGI response examples use that external-response signature lineage. SHA-2 RSA/digest algorithms are also accepted by the verifier. This compatibility allowance applies only to validation of external DGI response evidence and does not weaken the consumer's own fiscal-document signing policy.

## Persisted append-only evidence

Successful verification persists one row in:

`v1_fiscal_cfe_document_response_signature_verifications`

The row preserves:

- verification id;
- source consultation id;
- source ACKSobre observation id;
- source submission id;
- source Sobre id;
- organization id;
- exact source `ResponseSha256`;
- verification profile id;
- embedded-certificate SHA-256;
- certificate thumbprint and serial number;
- certificate subject and issuer;
- canonicalization, signature and digest algorithms;
- reference URI;
- serialized transform list;
- `CertificateTrustValidated = false`;
- whole-second verification timestamp.

`ConsultationId` is unique. Concurrent verification attempts therefore converge to one append-only durable verification instead of producing competing evidence.

All source foreign keys use `Restrict`; signature verification cannot cascade-delete or rewrite the consultation, ACKSobre observation, submission or Sobre.

## Explicitly not claimed

This increment does **not**:

- build or validate an `X509Chain`;
- establish PKI Uruguay trust;
- perform revocation validation;
- prove DGI-specific signer identity or signer habilitation;
- set `CertificateTrustValidated = true`;
- interpret `ACKCFE_det/Estado` as a local state transition;
- reinterpret the separately accepted `EstadoCFE` consultation taxonomy;
- infer that one ACKCFE response completes every CFE in the Sobre;
- poll or reconsult automatically;
- retry an ambiguous Sobre `Unknown`;
- recover S08;
- allocate `Idemisor`;
- mutate FiscalDocument, sale, accounting, inventory, Sobre, submission, ACKSobre or consultation evidence;
- expose a public REST endpoint;
- establish DGI Testing or Production readiness.

A successful row means only that the exact durable ACKCFE XML passed the bounded whole-document XMLDSig signature-math profile with the embedded public certificate.

## Testing boundary

The increment adds tests for:

- valid whole-document ACKCFE verification while trust remains false;
- signed-byte tampering failure;
- external-reference rejection;
- missing embedded-certificate rejection;
- DTD rejection;
- append-only provider-real persistence/replay on PostgreSQL and MySQL;
- invalid-signature fail-closed persistence behavior;
- provider-real concurrent verification convergence;
- architecture separation between Application policy, Infrastructure XML crypto and persistence.

## Readiness

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

The natural next separately governed boundary, if authoritative trust material remains sufficient, is PKI Uruguay trust validation for the already verified ACKCFE certificate. That future boundary must not rewrite this signature-math record and must continue to keep DGI-specific signer identity/habilitation explicit and separate.
