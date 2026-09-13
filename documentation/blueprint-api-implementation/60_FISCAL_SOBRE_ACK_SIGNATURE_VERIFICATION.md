# 60 — Fiscal Sobre ACK XMLDSig Verification

Status: GOVERNED IMPLEMENTATION CANDIDATE

Candidate PR: #87 `feat(fiscal): verify ACKSobre XMLDSig`

Base accepted baseline: `main@63c63f44b91d6fea6fb073af8c3e3d7841aa4c63`
(merge of PR #86, `feat(fiscal): add ACKSobre observation`).

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Purpose

This bounded increment verifies the cryptographic XMLDSig integrity of an already durable DGI `ACKSobre` response and persists append-only verification evidence.

It does not call DGI again, does not rewrite transport or semantic ACK evidence and does not mutate any CFE, sale, inventory, finance or fiscal business state.

The capability proves only that the XMLDSig signature math is valid for the exact durable response bytes under the public X.509 certificate embedded in the response and that the signature reference covers the whole XML document under a constrained local safety profile.

It deliberately does **not** claim that the embedded certificate chains to a trusted DGI authority, that the certificate was legally valid at a particular instant, or that external revocation/habilitation checks passed.

## Why this slice follows PR #86

PR #86 accepted structural `Signature` presence only and explicitly deferred cryptographic validation.

The current DGI `Formato de Mensajes de Respuesta v19` requires public certificate evidence and an advanced electronic signature over the response. The official `ACKSobre` reception example also shows:

- one XMLDSig `Signature`;
- `Reference URI=""`;
- enveloped-signature transform;
- canonicalization transform;
- RSA signature method;
- digest method;
- embedded `X509Certificate` evidence.

Authoritative sources reviewed for this slice:

- current `Formato de Mensajes de Respuesta v19`:
  `https://www.efactura.dgi.gub.uy/files/formato_mensajes_respuesta_v19-pdf?es=`;
- DGI reception-service example containing the signed `ACKSobre`:
  `https://www.efactura.dgi.gub.uy/files/DocumentoServiciosWebExternos?es=`.

## Consultation-token evidence gap

The accepted ACK observation may preserve `ParamConsulta/Token + FechaHora`. Revalidation of the current `Servicios Web Externos DGI` Consultas document, version 1.9 dated 13/05/2024, did **not** establish a governed web-service operation whose input is that ACKSobre token and whose output is the second/document-level CFE response.

The current consultation contract exposes methods such as `EFACCONSULTARESTADOCFE`, `EFACCONSULTARENVIOSCFE` and `EFACCONSULTARENVIOSSOBRE`, which may return consultation parameters, but this is not authority to invent a token-input endpoint.

Therefore PR #87 does not implement second-response consultation. That path remains blocked until an authoritative operation, input shape and correlation rule can be proven.

## Source prerequisite

Verification requires the already accepted durable chain:

```text
Sobre durable
-> ResponseReceived transport submission
-> exact ResponseXml + ResponseSha256
-> ACKSobre append-only semantic observation
-> XMLDSig cryptographic verification
```

Application revalidates:

- durable Sobre integrity;
- transport submission integrity;
- exact response SHA-256;
- presence and integrity of the accepted ACK observation;
- source ids and hashes across all three rows.

No network access occurs in this slice.

## Infrastructure verification profile

The XML cryptography adapter is `DgiFiscalCfeEnvelopeAckSignatureVerifier` behind `IFiscalCfeEnvelopeAckSignatureVerifier`.

The local verification profile id is:

`dgi-acksobre-xmldsig-math-v1`

The verifier:

- prohibits DTD processing;
- disables external XML resolution;
- requires root `ACKSobre` in `http://cfe.dgi.gub.uy`;
- requires exactly one direct XMLDSig `Signature`;
- requires exactly one `Reference`;
- requires `Reference URI=""` so the signature target is the whole document rather than an external or fragment target;
- requires the enveloped-signature transform;
- allows only canonicalization transforms already supported by the governed XMLDSig implementation;
- rejects XPath/XSLT and other ungoverned transforms;
- requires exactly one embedded X.509 certificate;
- requires an RSA public key for the accepted RSA signature methods;
- calls `SignedXml.CheckSignature(certificate, verifySignatureOnly: true)`;
- returns non-secret certificate/signature evidence only.

## Algorithm boundary

The official historical DGI `ACKSobre` reception example uses RSA-SHA1/SHA1. Current response-format material requires the advanced signature but does not establish one unique modern algorithm tuple for all current responses.

For external verification only, the local safety profile accepts a bounded set of RSA signature and SHA digest algorithms supported by the platform, including the legacy RSA-SHA1/SHA1 tuple shown by DGI and SHA-2 variants.

This does not weaken the consumer's own fiscal signer. The accepted outbound signer continues to forbid SHA-1 and keeps its separately governed SHA-2 policy.

Algorithm acceptance here is a local compatibility/safety rule for verifying external DGI evidence, not a claim that DGI mandates every allowed tuple.

## Certificate trust boundary

A mathematically valid signature proves possession of the private key corresponding to the embedded public certificate. It does not, by itself, prove that the certificate is an authorized DGI certificate.

Accordingly every persisted row from this slice records:

`CertificateTrustValidated = false`

PR #87 performs no:

- X.509 trust-chain construction;
- trust-anchor pinning;
- DGI-specific subject/issuer authorization policy;
- certificate validity-time coercion from ACK timestamps;
- OCSP or CRL processing;
- certificate habilitation lookup.

A future trust-chain slice must use authoritative certificate/trust-anchor evidence and must not reinterpret this flag retroactively.

## Durable append-only evidence

Successful verification is persisted in:

`v1_fiscal_cfe_envelope_ack_signature_verifications`

The row preserves:

- internal verification id;
- source ACK observation id;
- source submission id;
- source envelope id;
- organization id;
- exact response SHA-256;
- verification profile id;
- certificate SHA-256 fingerprint;
- certificate thumbprint and serial;
- certificate subject and issuer text;
- canonicalization method;
- signature method;
- digest method;
- reference URI;
- reference transforms;
- explicit `CertificateTrustValidated = false`;
- local verification timestamp.

Referential integrity uses `Restrict` FKs to the ACK observation, transport submission and durable Sobre. `AckObservationId` is unique so an exact replay converges to the same durable verification row.

The repository is read/add-only and owns no transaction or `SaveChanges`.

## Fail-closed behavior

No durable verification row is created when:

- XML is malformed or uses prohibited DTD/external resolution;
- ACK root/signature structure is invalid;
- signature algorithm/digest/reference policy is outside the governed allowlist;
- the signature targets anything other than the whole document;
- required enveloped-signature transform is missing;
- the embedded certificate is missing/invalid/non-RSA for the accepted profile;
- cryptographic signature checking fails;
- source response hash or lineage no longer matches persisted transport/ACK evidence.

A failed signature does not rewrite the already durable raw transport response or ACK semantic observation. Those remain forensic source evidence.

## Replay and concurrency

An exact replay returns the existing verification after revalidating its persisted evidence and source lineage.

Concurrent PostgreSQL/MySQL verifiers converge through the unique `AckObservationId` constraint and the accepted provider-specific uniqueness conflict classifier.

The source Sobre, submission and ACK observation remain immutable.

## Validation coverage

This candidate requires proof that:

- a real ephemeral X.509 + whole-document XMLDSig validates successfully;
- byte/content tampering causes cryptographic failure;
- an external URI reference is rejected before evaluation;
- missing embedded X.509 evidence is rejected;
- DTD/external-entity input fails closed;
- persisted successful evidence always has `CertificateTrustValidated = false`;
- invalid signature evidence creates no verification row;
- exact replay returns one durable row;
- concurrent PostgreSQL/MySQL verification converges to one row;
- accepted transport and ACK semantic evidence remain unchanged;
- Application owns no XMLDSig/X509 implementation details;
- Infrastructure owns XML cryptography and provider persistence.

## Deliberate non-scope

PR #87 does not implement:

- X.509 chain/trust-anchor validation for DGI;
- OCSP/CRL or certificate habilitation;
- document-level CFE response consultation using the ACK token;
- document-level CFE acceptance/rejection mutation;
- automatic S08 recovery or retransmission;
- ambiguous Sobre `Unknown` reconciliation;
- automatic `Idemisor` allocation;
- business grouping/batching policy;
- external DGI Testing acceptance;
- Production enablement.

Local cryptographic verification is not evidence of DGI certification or Production readiness.
