# 30 - Fiscal Signing Contract + Durable Signing Evidence

Status: **CANDIDATE / NOT ACCEPTED UNTIL MERGE**

Date: 2026-09-09

Accepted predecessor baseline: `main@578ff84e2696c66fa36726e45f4069672a4c7aaa`
(merge of PR #54, deterministic Unsigned CFE Builder).

## Purpose

Implement the second bounded slice from the accepted CFE build/sign/validate lifecycle without yet
integrating certificates, private keys or XMLDSig.

The official DGI Formato CFE v25.2 defines Zone H as the date/time of the advanced electronic
signature and requires it for the supported CFE families. Its format is
`AAAA-MM-DDTHH:MM:SS-hh:mm`; the active document also constrains the upper date to DGI server date
plus one day. The exact signing instant is therefore signing evidence, not unsigned-builder data.

## Accepted boundary in this candidate

The signing workflow establishes one immutable evidence record containing:

- `FiscalDocumentId`;
- immutable `FiscalContentSnapshot.ContentFingerprint`;
- SHA-256 of the deterministic unsigned CFE XML;
- `SigningTimestamp`, normalized to whole-second precision for later `TmstFirma` rendering.

The timestamp is established only after unsigned BUILD succeeds and only when no prior durable
signing evidence exists.

Replay/retry:

1. reloads `FiscalDocument` and the immutable snapshot;
2. deterministically rebuilds unsigned CFE content;
3. recomputes the unsigned SHA-256;
4. loads the existing signing evidence;
5. requires snapshot fingerprint and unsigned hash to match;
6. returns the already persisted signing timestamp;
7. does **not** consult the signing clock again.

If replay evidence differs, the workflow fails closed as inconsistent state.

## Ports

`IFiscalSigningTimeSource`

- owns acquisition of the signing timestamp;
- preserves the UTC offset supplied for the signing act;
- is called only before first durable signing evidence.

`IFiscalSignatureProvider`

- is the future XMLDSig port;
- receives the deterministic unsigned XML, its SHA-256 and the already durable signing timestamp;
- certificate/private-key implementation remains Infrastructure-only;
- this candidate deliberately provides no implementation and does not invoke the port.

## Persistence

`v1_fiscal_signing_evidence` is separate from `v1_fiscal_documents` and contains one row per
organization + fiscal document.

Database guarantees:

- primary key on signing evidence id;
- restrictive FK to the fiscal document;
- unique `(OrganizationId, FiscalDocumentId)`;
- 64-character snapshot fingerprint;
- 64-character unsigned SHA-256;
- signing timestamp persisted at second precision in the EF model.

There is no backfill. Historical fiscal documents without signing evidence remain unsigned until an
eligible signing-evidence workflow executes successfully.

## Failure semantics

### BUILD failure

No signing clock call and no signing evidence row.

### First evidence persistence failure

No certificate/private-key boundary is crossed. The transaction rolls back signing evidence, audit
and outbox state together.

### Replay mismatch

No fresh timestamp is generated. The mismatch is an inconsistent replay and must be investigated.

### Later signing failure

Once this durable evidence exists, later signing retries must reuse its `SigningTimestamp`; they may
not silently create a new `TmstFirma` to obtain different signed bytes.

## Explicit non-scope

This candidate does not implement:

- `TmstFirma` XML insertion;
- XMLDSig or `ds:Signature` generation;
- certificate discovery/selection;
- certificate chain policy;
- private-key/HSM/Key Vault custody;
- signed artifact storage;
- full official XSD validation;
- DGI/provider transport;
- export-family signing enablement.

## Next slice

After acceptance, the next bounded slice is **Signed Artifact Persistence / real signing adapter
preparation**, with exact certificate/key policy still requiring an explicit reviewed contract before
private-key integration.
