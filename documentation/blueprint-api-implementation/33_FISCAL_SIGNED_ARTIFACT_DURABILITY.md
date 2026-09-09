# Fiscal signed artifact durability

## Status

This slice follows the accepted deterministic signing-payload boundary and the evidence-backed XMLDSig adapter. It adds durable signed-artifact orchestration and persistence without enabling DGI transport or inventing a production certificate source.

## Goal

Persist exactly one signed CFE artifact per fiscal document and durable signing-evidence record, preserving enough non-secret evidence to prove which deterministic payload was signed and to make retries replay-safe.

## Stored evidence

`StoredFiscalSignedArtifact` persists:

- artifact id;
- organization id;
- fiscal document id;
- signing evidence id;
- immutable fiscal-content fingerprint;
- unsigned-content SHA-256;
- deterministic signing-payload SHA-256;
- signed-content SHA-256;
- durable `TmstFirma` timestamp;
- XMLDSig profile id;
- certificate thumbprint;
- certificate serial number;
- exact signed XML bytes represented as UTF-8 text.

Certificate metadata is public identification evidence only. No private key, PFX password, certificate path, store location, HSM handle or Key Vault identifier is persisted here.

## Replay rule

`SignFiscalDocumentUseCase` always rebuilds the unsigned CFE and deterministic `TmstFirma` signing payload from immutable snapshot + durable signing evidence before considering a new signing operation.

It then checks `IFiscalSignedArtifactRepository`.

If a signed artifact already exists:

1. document, organization and signing-evidence associations must still match;
2. fiscal fingerprint, unsigned hash and signing-payload hash must still match;
3. persisted signing timestamp must still match;
4. persisted signed XML must still hash to the stored signed-content SHA-256;
5. the signed XML must contain exactly one root-level `ds:Signature` as the final CFE child;
6. removing that signature must reconstruct the deterministic signing payload structurally;
7. the signature provider is **not called**.

Any mismatch fails closed as `fiscal.signed_artifact.replay_mismatch` or a more specific structural conflict.

## First signing rule

When no durable signed artifact exists, Application calls `IFiscalSignatureProvider` using the already-established deterministic signing payload. The provider result must include:

- signed XML;
- signed-content SHA-256;
- signature profile id;
- certificate thumbprint;
- certificate serial number.

Application re-hashes the returned XML and verifies that adding `ds:Signature` did not mutate the fiscal payload before persistence.

The artifact, audit event and outbox event are persisted within the same application transaction boundary.

## Persistence

New table:

`v1_fiscal_signed_artifacts`

Constraints:

- unique `(OrganizationId, FiscalDocumentId)`;
- unique `SigningEvidenceId`;
- restrictive foreign key to `v1_fiscal_documents`;
- restrictive foreign key to `v1_fiscal_signing_evidence`.

These constraints make one durable signing act map to one durable signed artifact.

## Composition boundary

The persistence composition registers:

- `IFiscalSigningPayloadBuilder`;
- `IFiscalSigningEvidenceRepository`;
- `IFiscalSignedArtifactRepository`;
- `PrepareFiscalSigningEvidenceUseCase`;
- `SignFiscalDocumentUseCase`.

It deliberately does **not** register `IFiscalSigningCertificateSource`. Production certificate resolution remains an explicit future composition concern so the application cannot silently fall back to a guessed PFX, platform store, HSM or cloud-secret configuration.

## Explicit non-scope

This slice does not add:

- a production certificate source;
- certificate revocation / OCSP / CRL validation;
- proof that the certificate is currently enabled by DGI for e-invoicing;
- DGI test-environment acceptance evidence;
- full signed-CFE XSD validation;
- Adenda reintegration;
- DGI SOAP/HTTP transport;
- envelope creation;
- acknowledgement or rejection handling;
- retry queues for DGI transport;
- Blueprint 0.5.2 formal adoption changes.

## Next gate

Before DGI transport, the recommended next steps are:

1. signed-root XSD validation against the official CFE v1.44.2 schema set already evidenced by the project;
2. explicit production certificate-source composition and certificate policy;
3. DGI Testing acceptance proof for signed e-Ticket and signed e-Factura;
4. only then add the DGI transport boundary.
