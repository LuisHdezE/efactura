# 62 - Fiscal Sobre ACK Certificate Trust

**Status: GOVERNED IMPLEMENTATION CANDIDATE**  
**Baseline:** `main@bf7d97e350192302cd7cc681d6c7feecfa8589ab`  
**Formal traditional DGI Testing readiness:** **BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Purpose

This increment adds a bounded, append-only trust-validation step for the X.509 certificate already extracted from an `ACKSobre` whose XMLDSig mathematics have been verified by the accepted PR #87 boundary.

The capability answers only this narrower question:

> At the validation instant, does the exact certificate embedded in the durable ACKSobre build a valid PKI Uruguay chain to an explicitly configured and SHA-256-pinned trust root while the platform performs online revocation checking for the entire chain?

A positive answer does not prove DGI legal identity. It also does not prove DGI-specific certificate habilitation, a particular DGI organizational unit, or Production acceptance. This increment therefore records `DgiIdentityValidated = false` even when PKI Uruguay trust succeeds.

## Authoritative basis rechecked 2026-09-13

The boundary is based on current official material rather than provider conventions or legacy demo behavior:

- DGI response-format documentation defines the response electronic certificate as the public-certificate evidence required to validate the advanced electronic signature over the response and requires a validly issued electronic certificate:
  `https://www.efactura.dgi.gub.uy/files/formato_mensajes_respuesta_v19-pdf?es=`
- DGI FAQ / eFactura definitions link recognized electronic certificates to certification providers accredited before Uruguay's Unidad de Certificación Electrónica (UCE):
  `https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=`
- UCE defines the Autoridad Certificadora Raíz Nacional (ACRN) as the root of trust for PKI Uruguay and publishes the national root-certificate/CRL material:
  `https://www.gub.uy/unidad-certificacion-electronica/comunicacion/publicaciones/autoridad-certificadora-raiz-nacional`
- UCE publishes the Uruguay trust list containing ACRN and accredited certification authorities/providers:
  `https://www.gub.uy/unidad-certificacion-electronica/datos-y-estadisticas/datos/lista-confianza-autoridades-certificadoras`
- UCE's ACRN Certification Policy v2.2 establishes the trust-chain role of ACRN and the obligation of relying parties to verify certificate validity and revocation when relying on certificates:
  `https://www.gub.uy/unidad-certificacion-electronica/sites/unidad-certificacion-electronica/files/documentos/publicaciones/cp_acrn_v2.2.pdf`

The reviewed material is sufficient to govern PKI Uruguay chain/revocation validation. It does not establish a separate authoritative rule that lets this consumer infer that every successfully chained end-entity certificate is specifically DGI's legal ACK signer. That stronger identity claim remains fail-closed.

## Accepted source lineage required by the candidate

Trust validation may only start after all of the following durable evidence already exists and remains internally consistent:

```text
FiscalCfeEnvelope
-> FiscalCfeEnvelopeSubmission(ResponseReceived)
-> FiscalCfeEnvelopeAckObservation
-> FiscalCfeEnvelopeAckSignatureVerification(SignatureValid)
-> candidate PKI Uruguay certificate-trust validation
```

The candidate reuses the exact durable `ResponseXml` and its SHA-256 lineage. It re-extracts the single embedded `ds:X509Certificate` from those exact response bytes and requires its SHA-256 to match the certificate hash already recorded by the accepted XMLDSig verification. A trust validation can therefore not silently switch to another certificate.

## Trust-anchor configuration

Trust roots are deliberately **not** hardcoded from remembered fingerprints or copied from third-party stores.

The adapter reads public CA certificates from external configuration under:

`FiscalTransport:AckCertificateTrust`

Each configured root/intermediate requires:

- an absolute public-certificate file path;
- an expected SHA-256 certificate hash;
- successful SHA-256 pin comparison before the certificate enters the trust store;
- CA basic constraints;
- for configured roots, a self-issued subject/issuer relationship.

At least one root is required. The official UCE/Agesic ACRN public certificate should be acquired operationally from the authoritative government source and pinned out of source control. This PR does not claim or embed an ACRN fingerprint because the authoritative certificate endpoint is binary and its bytes were not independently pinned through this governed repository workflow.

No private key is involved in this capability.

## Chain and revocation policy

Infrastructure uses .NET `X509Chain` with the following explicit policy:

- `TrustMode = CustomRootTrust`;
- only externally configured, SHA-256-pinned roots enter `CustomTrustStore`;
- optional configured intermediates enter `ExtraStore`;
- `RevocationMode = Online`;
- `RevocationFlag = EntireChain`;
- `VerificationFlags = NoFlag`;
- validation time is the whole-second UTC instant persisted with the trust result;
- URL retrieval is bounded by configuration, default 15 seconds and allowed range 1..60 seconds;
- certificate downloads remain enabled so the platform may follow standard chain/revocation locations;
- the end certificate must permit `DigitalSignature` use;
- DTD processing and external XML resolution remain prohibited while extracting the embedded certificate.

If the chain cannot be built, the root is not one of the configured pins, revocation is unavailable, the certificate hash no longer matches the previously verified signature evidence, or any chain status remains nonzero, the operation fails closed and no positive trust record is persisted.

This uses standards-based platform chain/revocation processing. The increment does not invent a DGI-specific OCSP endpoint, CRL URL, proprietary revocation protocol or fallback policy.

## Application boundary

`ValidateFiscalCfeEnvelopeAckCertificateTrustUseCase` owns the local policy and depends on the Infrastructure validator through `IFiscalCfeEnvelopeAckCertificateTrustValidator`.

The command identifies the already durable Sobre using:

- `OrganizationId`;
- issuer RUC;
- receiver RUT;
- sender envelope id;
- caller-supplied `OperationId`.

The trust operation is append-only and replay-safe by `OrganizationId + OperationId`. Reusing the same operation for the same durable source returns the persisted result without a second trust evaluation. A new operation id may be used for a later explicit validation because certificate status/revocation is time-sensitive evidence.

A successful validator response must prove all of the following to Application:

- `IsTrusted = true`;
- the end-certificate SHA-256 matches the accepted XMLDSig verification evidence;
- a SHA-256-pinned root is recorded;
- at least leaf + root chain hashes are recorded;
- `ChainBuilt = true`;
- `RevocationChecked = true`;
- `RevocationMode = Online`;
- `DgiIdentityValidated = false`.

Any validator response that overclaims DGI identity is rejected rather than persisted.

## Persistence

Successful evidence is stored in:

`v1_fiscal_cfe_envelope_ack_certificate_trust_validations`

The record preserves:

- source signature-verification id;
- source ACK observation id;
- source submission id;
- source envelope id;
- organization and operation ids;
- exact response SHA-256;
- validation profile id;
- end-certificate SHA-256;
- trusted-root SHA-256;
- ordered chain certificate SHA-256 evidence;
- revocation mode;
- `PkiUruguayTrustValidated`;
- `DgiIdentityValidated`;
- validation instant.

Foreign keys to all accepted source evidence use `Restrict`. The repository is append-only. It does not rewrite the ACK, transport, envelope, sale, CFE, accounting or inventory evidence.

Crucially, the accepted PR #87 signature-verification record remains unchanged and continues to say `CertificateTrustValidated = false`; this new table is a later and independently timestamped trust observation rather than a historical rewrite.

## Explicitly not claimed

This candidate does not claim:

- DGI legal signer identity for the end-entity certificate;
- DGI-specific certificate habilitation;
- a hardcoded DGI certificate subject, serial number or thumbprint;
- a DGI-specific OCSP/CRL endpoint;
- that a trust result remains valid forever after its recorded validation instant;
- token-input CFE response consultation;
- `EstadoCFE` lifecycle semantics;
- Sobre `Unknown` recovery;
- `S08` recovery semantics;
- automatic `Idemisor` allocation;
- CFE batching/grouping policy;
- DGI Testing acceptance;
- Production readiness or Production transport enablement.

The formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Verification plan

The governed CI candidate must prove:

- configuration fails closed for missing roots, SHA-256 pin mismatch and non-CA roots;
- the real adapter rejects a certificate whose bytes do not match the accepted XMLDSig certificate hash;
- unsafe DTD input fails closed;
- provider-real PostgreSQL 16 and MySQL 8.4 persist/replay append-only positive trust evidence without rewriting accepted source rows;
- provider-real concurrent same-operation validation converges through the unique operation key;
- architecture tests keep chain/revocation code in Infrastructure and preserve `DgiIdentityValidated = false`.

## Next boundary after this candidate

If this increment is accepted, the next bounded work should continue to follow authoritative evidence. The strongest next candidates remain the token-input document-level CFE response contract or an authoritative `EstadoCFE` taxonomy if DGI publishes enough material; otherwise the product-governed CFE grouping/batching policy is a safe independent path.
