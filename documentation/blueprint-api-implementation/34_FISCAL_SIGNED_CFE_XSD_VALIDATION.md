# 34 — Fiscal signed CFE XSD validation

## Decision

A signed CFE is not durable acceptance evidence until it passes the pinned DGI FE schema set used by this release.

Release-1 therefore adds an explicit post-signing XSD gate:

`immutable fiscal snapshot -> unsigned CFE -> durable signing evidence -> deterministic TmstFirma payload -> XMLDSig -> signed-root XSD validation -> durable signed artifact`

The validation boundary is `IFiscalSignedCfeSchemaValidator` in Application. The concrete v1.44.2 implementation and XML/XSD dependencies remain in Infrastructure.

## Regulatory baseline

The DGI e-Factura **Documentos de interés** registry was rechecked during this slice and continues to publish:

- `Formato CFE v25.2` for Testing and Production;
- `XSDs_FE_V1.44.2` for Testing and Production.

Official registry:

`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

DGI-listed archive URL:

`https://www.efactura.dgi.gub.uy/files/xsds_fe_1_44_2-zip?es=`

## Archive retrieval observation

On 2026-09-09 the governed self-hosted runner `efactura-ci-01` requested the DGI-listed archive with browser-compatible headers and the official registry as referer.

Observed response:

- HTTP status: `200`;
- `Content-Type: text/plain`;
- `Content-Length: 0`;
- body length: `0` bytes.

Therefore this candidate does **not** claim that the committed schema bytes were downloaded from DGI during this run. DGI remains the regulatory authority for the schema version and publication status, but its listed archive endpoint was not a usable byte source in the observed run.

## Governed byte recovery

The required schema bytes were recovered from the immutable public mirror:

- repository: `olagopirez/factible`;
- commit: `9a24a598e9470c4a1c5c34cb0cb253d08ffa3db3`;
- path: `packages/cfe/spec/xsd`.

The mirror is corroborative and byte-recovery evidence only. It is **not** the regulatory authority.

Before commit, each recovered file was required to match a previously observed Git blob identity. The committed `schema-manifest.json` additionally fixes SHA-256 for every file.

## Minimal signed-CFE schema closure

The runtime ships only the closure required by `CFEDGI.xsd` for a signed CFE root:

| File | SHA-256 |
| --- | --- |
| `CFEDGI.xsd` | `e08d9cd95f9128d065fdbfdcccd2d3eaef99604561c8711fdb5587362681af21` |
| `CFEType.xsd` | `81d718fa26bba9908d45cf05af47fa6d41bdedc0906f995628820f051f5f9ce5` |
| `DGITypes.xsd` | `5bbc462de995acc26c572e5392c5604539b76b6645e4677d06aba84347205769` |
| `xmldsig-core-schema.xsd` | `35cf8197da812c85e40d57891b35c94187569ed474a2dac813ce5090dafcd35c` |
| `xenc-schema.xsd` | `a7401e4126ea13975a444f418a173c1ed16785d85b70135728c39f2d74a3aaf9` |
| `version.txt` | `8386e71531b561bd75a2ec62c0b2d8d24fae42fd1cc318790a2f2585d5591d27` |

`version.txt` must contain exactly:

`version: 1.44.2`

`CFEDGI.xsd` defines the `{http://cfe.dgi.gub.uy}CFE` root and includes `CFEType.xsd`. `CFEType.xsd` requires one `ds:Signature` after the CFE family element, consistent with the signing adapter established in PR #57.

## Runtime integrity model

`DgiFeV1_44_2SignedCfeSchemaValidator`:

1. loads the embedded manifest and exact six-file closure;
2. recomputes SHA-256 for every embedded file;
3. refuses schema compilation if any hash, version, file list or root-schema identity differs;
4. compiles the XSDs using a custom in-memory resolver;
5. prohibits DTD processing;
6. refuses external schema resolution;
7. performs no HTTP/network access while validating a fiscal artifact;
8. returns separate `DocumentInvalid` and `SchemaSetInvalid` outcomes.

The schema set itself is therefore part of the governed runtime evidence rather than a mutable external dependency.

## Durable schema evidence

A newly accepted signed artifact now records:

- `SchemaSetId`;
- `SchemaVersion`;
- `SchemaSetFingerprint`.

The schema-set fingerprint is deterministic over the sorted filename/SHA-256 pairs of the pinned closure.

The same evidence is included in the signed-artifact audit payload and outbox integration event.

## First-sign gate

For a new signed artifact the use case performs:

1. immutable snapshot and signing-evidence reconstruction;
2. deterministic signing-payload reconstruction;
3. XMLDSig provider call;
4. signed-content hash and signature-structure validation;
5. DGI signed-root XSD validation;
6. only after all previous checks pass, durable artifact + audit + outbox persistence.

A document rejected by XSD is never persisted as an accepted signed artifact.

## Replay gate

Replay still does not cross the private-key boundary.

For an existing durable artifact the workflow:

1. rebuilds the deterministic unsigned/signing payload;
2. verifies artifact/evidence/hash associations;
3. verifies signed XML structure;
4. revalidates the stored signed XML against the pinned schema set;
5. requires persisted schema-set id/version/fingerprint to match the active pinned validator;
6. returns the existing artifact without calling the signature provider.

A historical row without schema-validation evidence is not silently upgraded. The migration adds the three new columns as nullable for compatibility, while Application fails closed when replay evidence is absent or inconsistent.

## Release-1 coverage

The current signing chain remains bounded to the families already supported upstream:

- e-Ticket (`eTck`);
- e-Factura (`eFact`).

Export CFE remains fail-closed upstream and is not broadened by this slice.

Cross-cutting tests exercise the full local chain using real ephemeral RSA/SHA-256 certificates:

`builder -> TmstFirma payload -> XMLDSig -> XSD v1.44.2`

for both supported families. Negative tests cover unsigned content, signature position and malformed `TmstFirma`.

## Explicit non-scope

This slice does not add:

- production PFX/store/HSM/Key Vault certificate source;
- private-key configuration or secret persistence;
- OCSP/CRL/revocation checking;
- DGI-specific certificate habilitation proof;
- Adenda reintegration;
- DGI envelope construction;
- DGI SOAP/transport calls;
- DGI response or acknowledgement handling;
- formal Blueprint adoption changes.

## Next gate

After signed-root XSD acceptance, the next recommended fiscal boundary is explicit production certificate-source selection/composition plus certificate trust/status policy, followed by DGI Testing acceptance evidence for at least one signed e-Ticket and one signed e-Factura before any production transport claim.
