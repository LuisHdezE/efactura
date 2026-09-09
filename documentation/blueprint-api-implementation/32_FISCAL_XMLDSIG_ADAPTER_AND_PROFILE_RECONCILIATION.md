# 32 — Fiscal XMLDSig Adapter and Profile Reconciliation

Status: CANDIDATE until the corresponding PR is merged.

## Purpose

Introduce a concrete XML Digital Signature adapter behind the already accepted
`IFiscalSignatureProvider` boundary without pretending that an obsolete 2012 algorithm statement is
the complete current DGI profile.

This slice signs the deterministic payload accepted in PR #56. That payload already contains the
durable `TmstFirma` and excludes any pre-existing `ds:Signature`.

## Current official DGI evidence

### Formato de los CFE v25.2 — 28/04/2026

Official source:
https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=

DGI currently states that:

- Zone I is mandatory advanced electronic signature for the CFE families;
- the signature covers the complete document except Zone J — Adenda;
- the certificate serial must correspond to a certificate that is valid, not revoked and enabled for
  electronic invoicing at signing time;
- the signature is an advanced electronic signature according to the XML Digital Signature standard.

The portal states that v25.2 is enabled in Production from 30/06/2026.

The current functional document does **not** publish, in that Zone I definition, a unique modern tuple
for `CanonicalizationMethod`, `SignatureMethod`, `DigestMethod`, `Reference URI` and transforms.

### DGI Preguntas Frecuentes

Official source:
https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=

Question 4.15 recommends SHA-2 certificates and states that SHA-1 certificates are not accepted from
01/01/2018.

This is certificate-policy evidence. It must not be silently rewritten as proof of one exact current
XMLDSig transform/profile tuple.

## Older official evidence retained as historical context

### DGI technical CFE document v1.1 — 01/07/2012

Official source:
https://www.efactura.dgi.gub.uy/files/documento-tecnico-de-division-informatica-archivo-pdf-514-kb?es=

Section 10.3 states RSA-SHA1 for the original CFE regime. This is valuable historical evidence but is
not treated as sufficient current production guidance because DGI later rejected SHA-1 certificates
and has published SHA-256 guidance in other DGI signing contexts.

### DGI Consumo de Web Services — 28/05/2018

Official source:
https://servicios.dgi.gub.uy/files/consumo-de-web-services-de-dgi?es=

The DGI web-services signing guidance demonstrates:

- RSA-SHA256 signature algorithm;
- SHA256 digest algorithm.

This is official DGI cryptographic evidence, but it is not itself the CFE Zone I specification.

## Operational compatibility evidence

A publicly available 2023 production-looking Uruguay CFE specimen contains:

- `CanonicalizationMethod = http://www.w3.org/TR/2001/REC-xml-c14n-20010315`;
- `SignatureMethod = http://www.w3.org/2001/04/xmldsig-more#rsa-sha256`;
- `Reference URI = ""`;
- enveloped-signature transform;
- `DigestMethod = http://www.w3.org/2001/04/xmlenc#sha256`;
- X509 issuer/serial evidence.

Source:
https://gist.github.com/pablohmontenegro/ebeff97ae3d2e357e2d79e0a43322c69

This is **not** normative DGI documentation. It is used only as compatibility evidence alongside the
official material.

## Implemented profile

The adapter exposes an explicit profile named:

`dgi-cfe-sha256-evidence-backed-v1`

Initial tuple:

| Field | Value |
| --- | --- |
| Canonicalization | `http://www.w3.org/TR/2001/REC-xml-c14n-20010315` |
| Signature method | `http://www.w3.org/2001/04/xmldsig-more#rsa-sha256` |
| Digest method | `http://www.w3.org/2001/04/xmlenc#sha256` |
| Reference URI | empty string |
| Reference transform | enveloped-signature |
| KeyInfo | X509 certificate + issuer/serial |

The profile name deliberately says **evidence-backed**, not `official-v25.2`.

SHA-1 XMLDSig profiles are fail-closed in this adapter. If DGI Testing later proves that a different
profile is required, that becomes an explicit governed change backed by captured test evidence.

## Adapter boundary

`src/Infrastructure/Fiscal/XmlDsigFiscalSignatureProvider.cs`

Responsibilities:

1. verify the SHA-256 hash of `SigningPayloadXml` against the Application request;
2. parse XML with external resolution disabled and preserved whitespace;
3. require the official `http://cfe.dgi.gub.uy` CFE root;
4. reject a pre-existing `ds:Signature`;
5. reject `Adenda` from the signing payload because Zone J is outside the fiscal signature scope;
6. require Release-1 `eTck`/`eFact` family consistency;
7. require first-child `TmstFirma` and exact equality with the durable signing timestamp;
8. obtain the certificate/private key only through the Infrastructure-only
   `IFiscalSigningCertificateSource`;
9. reject a certificate that is outside its validity interval at `TmstFirma`;
10. require a recognized SHA-2 certificate signature algorithm;
11. require an RSA private key;
12. compute the enveloped XMLDSig signature;
13. append `ds:Signature` to the CFE root after the family node;
14. cryptographically verify the produced signature before returning XML.

## Certificate status boundary

The current adapter verifies local facts available from the selected certificate:

- private key availability;
- RSA key availability;
- certificate validity interval at durable `TmstFirma`;
- SHA-2 certificate signature algorithm.

It does **not** claim to prove DGI-specific enablement or current revocation status. Those require an
explicit certificate trust/status strategy and remain outside this slice.

## Security and architecture constraints

- Domain never sees X509, thumbprints, paths, passwords or private keys.
- Application keeps only `IFiscalSignatureProvider` and the deterministic request/result contract.
- Certificate/private-key resolution lives in Infrastructure.
- XML external entity resolution is disabled.
- SHA-1 is not silently enabled from historical documentation.
- No PFX path/password is committed.
- No platform certificate store assumption is committed.
- No HSM/Key Vault implementation is committed.
- No signed-artifact persistence is added here.
- No DGI transport or acknowledgement processing is added here.
- No Blueprint 0.5.2 adoption changes are mixed into the fiscal slice.

## Verification introduced

Cross-cutting tests prove:

- generation of one verifiable XMLDSig signature with an ephemeral RSA/SHA-256 certificate;
- expected canonicalization/signature/digest/reference/transform profile;
- X509 certificate and issuer/serial evidence in `KeyInfo`;
- fail-closed behavior before certificate access when payload hash changes;
- fail-closed behavior if `Adenda` enters the signing payload;
- fail-closed behavior when the certificate is expired at durable `TmstFirma`;
- explicit rejection of an attempted SHA-1 profile.

Architecture tests guard:

- concrete signer remains in Infrastructure;
- Application and Domain remain certificate/private-key agnostic;
- transport and persistence do not leak into the signer;
- the XML cryptography package version is explicit.

## Dependency

`System.Security.Cryptography.Xml` is added at stable `10.0.12` for the .NET 10 Infrastructure
project.

## Explicit non-scope / next evidence gates

Before calling this profile production-ready for DGI, capture evidence from the DGI Testing environment
for at least one signed e-Ticket and one signed e-Factura, including acceptance/rejection detail.

Then continue with separate governed slices for:

1. production certificate source/selection and trust/status checks;
2. durable signed-artifact persistence and replay policy;
3. full signed CFE XSD validation;
4. DGI transport;
5. response/acknowledgement processing.
