# 31 - Fiscal TmstFirma Signing Payload Boundary

Status: **CANDIDATE / NOT ACCEPTED UNTIL MERGE**

Date: 2026-09-09

Accepted predecessor baseline: `main@c67b42e0007bd5d8560f79996e9627104ab1c48a`
(merge of PR #55, durable signing evidence contract).

## Purpose

Close the deterministic boundary between the accepted unsigned CFE artifact and a future concrete
XMLDSig adapter without yet accessing certificates/private keys or producing `ds:Signature`.

The active DGI Formato CFE v25.2 defines Zone H `TmstFirma` as signing-act data and renders it with
date, time and UTC offset. The previously accepted durable signing evidence therefore remains the
single source for this value. Retries must never obtain a new timestamp merely to obtain different
signed bytes.

For Release 1, the supported signing-payload families remain:

- e-Ticket (`101`);
- e-Factura (`111`).

Export (`121`) stays fail-closed until the export-specific frozen evidence and signing path are
explicitly accepted.

## Accepted candidate boundary

`DeterministicFiscalSigningPayloadBuilder` consumes only:

- the fiscal-document id being prepared;
- the deterministic `UnsignedCfeArtifact`;
- its already durable `FiscalSigningEvidence`.

Before producing any signing payload it verifies:

1. the durable evidence belongs to the same fiscal document;
2. the immutable fiscal-content fingerprint equals the unsigned artifact fingerprint;
3. SHA-256 of the unsigned XML equals the durable unsigned-content hash;
4. the root is the official `http://cfe.dgi.gub.uy` `CFE` element;
5. exactly one expected Release-1 family element exists;
6. `TmstFirma` is not already present;
7. `ds:Signature` is not already present.

Any mismatch fails closed before a future certificate/private-key boundary can be crossed.

## Deterministic `TmstFirma`

The builder inserts `TmstFirma` as the first child of the family element, before `Encabezado`, using
only the persisted `FiscalSigningEvidence.SigningTimestamp`.

Rendering is:

`yyyy-MM-ddTHH:mm:sszzz`

Example:

`2026-09-09T13:14:15-03:00`

The durable evidence already normalizes the timestamp to whole-second precision while preserving its
offset. The signing-payload builder does not consult a clock and does not change the offset.

The original unsigned XML is never mutated. A new XML string is produced and SHA-256 hashed as the
signing-payload content hash.

## XMLDSig provider contract

`IFiscalSignatureProvider` remains an Application-owned port and is still not invoked in this slice.
Its request is reconciled so a future Infrastructure implementation receives:

- fiscal document id;
- CFE family and format version;
- immutable fiscal-content fingerprint;
- durable unsigned-content SHA-256;
- deterministic signing-payload XML with `TmstFirma` already inserted;
- signing-payload SHA-256;
- the same durable `SigningTimestamp`.

A concrete provider may add `ds:Signature`, but it must not silently replace `TmstFirma` or rebuild the
business content.

Certificate discovery, private-key access and XMLDSig implementation remain Infrastructure-only.

## Why the concrete XMLDSig algorithm is still deferred

The current DGI material establishes advanced electronic signature requirements and current SHA-2
certificate policy, while older historical DGI XMLDSig material documents an RSA-SHA1 profile. This
candidate deliberately does **not** freeze an obsolete algorithm, canonicalization method, Reference
URI or transform chain by inference.

Before a real signer is accepted, the active XMLDSig profile must be reconciled against current DGI
evidence. If the official current material does not prescribe a unique profile, the chosen interoperable
profile must be recorded explicitly and tested against DGI testing before production use.

## Explicit non-scope

This candidate does not implement:

- certificate discovery or selection;
- certificate-chain/revocation policy;
- private-key access;
- PFX, OS certificate store, HSM or Key Vault adapters;
- concrete canonicalization/signature/digest algorithms;
- `ds:Signature` generation;
- signed-artifact persistence;
- cryptographic signature verification;
- complete validation against the official `CFEDGI.xsd` root;
- DGI/provider transport;
- DGI response/acknowledgement processing.

## DGI evidence baseline

Current project baseline remains:

- Formato CFE v25.2;
- XSDs FE v1.44.2;
- namespace `http://cfe.dgi.gub.uy`;
- root `CFE`;
- Release-1 family ordering with `TmstFirma` before `Encabezado`;
- full root-XSD validation only after `ds:Signature` exists.

Official registry:

`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

The diagnostic XSD evidence already captured in the repository history remains diagnostic-only and is
not merged into product code.

## Next slice

After acceptance, the preferred next slice is **concrete XMLDSig adapter/profile reconciliation**.
Signed-artifact persistence remains separate unless the implementation proves that atomic persistence
is required to make signing replay-safe without re-signing.
