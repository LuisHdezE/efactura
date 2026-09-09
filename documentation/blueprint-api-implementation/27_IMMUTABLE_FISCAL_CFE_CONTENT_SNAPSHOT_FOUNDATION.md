# 27 - Immutable Fiscal CFE Content Snapshot Foundation

Status: **CANDIDATE / EXACT-HEAD CI VALIDATED**

Date: 2026-09-08

Accepted predecessor baseline: `main@8acc0d37c8cd57ea04b2dee21c7b01545eb467a5`
(merge of PR #48, Party Address / Contact Contract Completion).

Blueprint evaluator for this consumer remains:
`0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

## Goal

Implement the second prerequisite selected by
`26_PRE_XML_FISCAL_CONTENT_PREREQUISITES.md`: create an immutable, durable CFE content snapshot that
future XML generation can consume without rereading mutable Company, FiscalLocation, Party or
Catalog master data.

This slice does **not** build XML, validate XSD, sign, access certificates/private keys, archive
signed artifacts, submit to DGI/provider transports or interpret external fiscal responses.

## Accepted predecessor evidence

The accepted PR #48 baseline already provides Party addresses and contacts through the canonical
Party contract. Post-merge `Clean Architecture Guard` run #181 (`34287373497`) validated
`main@8acc0d37c8cd57ea04b2dee21c7b01545eb467a5` with:

- .NET SDK `10.0.400`;
- ArchitectureTests 79/79 PASS;
- CrossCuttingTests 77/77 PASS;
- legacy UnitTest 21/21 PASS;
- PersistenceIntegrationTests 135/135 PASS on PostgreSQL 16.15 and MySQL 8.4.11;
- 312/312 represented automated tests PASS;
- known-vulnerability gate PASS with 0 known vulnerable packages;
- Release build PASS with 0 errors.

## Lifecycle boundary

The accepted pre-slice boundary remains:

`Sale CONFIRMED`
`-> FiscalizationRequest PENDING`
`-> CAE number reserved`
`-> FiscalDocument IDENTITY_CREATED`

This slice adds one bounded post-identity fact:

`FiscalDocument IDENTITY_CREATED`
`-> FiscalContentSnapshot FROZEN`

No XML generation state is claimed.

## Why two immutable evidence moments are required

The existing sale confirmation planner produces authoritative `CfeArithmeticResult` and CFE
selection provenance, but its confirmation fingerprint also includes inventory availability/version
evidence. Inventory legitimately changes when confirmation consumes stock. Re-running the complete
confirmation plan later would therefore be semantically wrong and could create false drift.

For that reason this slice separates two concerns:

1. **FiscalConfirmationEvidence at Sale confirmation**
   - captures only the accepted fiscal selection/arithmetic facts that must never be recalculated
     from mutable tax profiles or changed inventory;
   - is stored inside the durable `FiscalizationRequest` together with a deterministic SHA-256
     evidence fingerprint;
   - preserves CFE family, receiver-identification requirement, format version, currency,
     arithmetic rule-pack version, line amounts, VAT liability/rate bucket/applied rate, totals and
     regulatory rule provenance.

2. **FiscalContentSnapshot after FiscalDocument identity exists**
   - combines that frozen fiscal evidence with confirmed Sale line commercial content and the
     current authoritative issuer/receiver masters exactly once;
   - records source master IDs/versions for provenance;
   - is persisted append-only and associated 1:1 with the already-created FiscalDocument;
   - receives its own deterministic SHA-256 content fingerprint.

Once the final content snapshot exists, retries read that immutable snapshot and do not reread
Company, FiscalLocation or Party.

## Immutable issuer snapshot

The content snapshot freezes:

- Company RUC in the already-accepted 12-digit structural form;
- legal name;
- optional commercial name;
- Company master version;
- FiscalLocation ID;
- four-digit DGI branch code preserving leading zeros;
- fiscal address;
- city;
- department;
- FiscalLocation master version.

No additional RUC checksum algorithm is introduced by this slice.

## Immutable receiver snapshot

When a Party exists, the snapshot can freeze:

- Party ID and version;
- name/denomination;
- residence and tax-residence countries;
- one selected typed fiscal identity and issuing country when identification is required;
- one receiver address when domicile content is required by the already-selected supported CFE
  family.

The implementation fails closed rather than inventing a receiver identity/address choice:

- e-Factura uses the existing accepted requirement for a Uruguayan RUC (`TypeCode = 2`, `UY`);
- e-Ticket reuses the already-accepted format-compatible identity table from CFE eligibility;
- export CFE does not invent a narrower identity-type policy that is not yet present in the accepted
  domain; if multiple active identities are candidates while identification is required, snapshot
  creation reports an ambiguity prerequisite conflict;
- required address selection accepts a single primary fiscal address, otherwise a single primary
  address, otherwise a single fiscal address, otherwise the sole address; unresolved multiplicity
  fails closed.

These rules select among already-stored Party facts only. They do not define XML element mappings or
new DGI address code tables.

## Immutable line and fiscal evidence

For each confirmed Sale line the final snapshot freezes:

- contiguous one-based snapshot sequence;
- SaleLine ID and Item ID;
- item/service code and name already copied into the confirmed Sale;
- goods/service kind;
- quantity and unit price;
- currently accepted discount/surcharge inputs (zero in the current Release-1 sale path);
- authoritative calculated item amount;
- VAT liability;
- VAT rate bucket;
- applied rate percent;
- rate rule-pack version;
- regulatory rule evidence.

Header fiscal evidence additionally freezes the accepted net/minimum/basic/export taxable buckets,
minimum/basic VAT amounts, VAT total and total amount. Domain invariants reconcile line evidence and
header totals with the accepted fiscalization summary and fail closed on drift.

## Persistence

Provider-neutral persistence uses:

- nullable `ConfirmationEvidenceFingerprint` + `ConfirmationEvidenceJson` columns on
  `v1_fiscalization_requests` so pre-slice historical rows remain readable;
- new append-only table `v1_fiscal_content_snapshots`;
- unique `UX_v1_fcs_document` to enforce one snapshot per FiscalDocument;
- relational foreign keys back to FiscalDocument, FiscalizationRequest and Sale;
- UTF-8 JSON payloads plus separately persisted SHA-256 fingerprints rather than provider-specific
  PostgreSQL/MySQL JSON operators/types.

Historical requests that do not contain the new line-level confirmation evidence remain readable,
but snapshot creation fails with `fiscal.snapshot.confirmation_evidence_missing` instead of
reconstructing fiscal provenance.

## Application transaction and replay

`CreateFiscalContentSnapshotUseCase` performs one local transaction:

1. load FiscalDocument;
2. replay an existing matching immutable snapshot if already present;
3. require `FiscalDocumentStatus.IdentityCreated`;
4. load and reconcile the linked FiscalizationRequest;
5. require durable FiscalConfirmationEvidence;
6. load the immutable confirmed Sale;
7. snapshot Company/FiscalLocation/Party through application ports;
8. create and integrity-check the final content snapshot;
9. append snapshot, audit event and outbox event;
10. flush once through the unit of work.

A failure after flush is expected to roll back snapshot + audit + outbox together. Concurrent duplicate
creation is translated by `EfUnitOfWork` to the canonical conflict
`fiscal.snapshot.already_created` using `UX_v1_fcs_document` on both supported providers.

## Audit/outbox evidence

Successful creation appends:

- audit event: `FISCAL_CONTENT_SNAPSHOT_CREATED`;
- integration event: `FiscalContentSnapshotCreatedIntegrationEvent`.

Evidence includes FiscalDocument/FiscalizationRequest/Sale IDs, content fingerprint, confirmation
evidence fingerprint, CFE type/format, master versions and line count. Private keys, certificates and
provider secrets do not enter these events.

## Automated proof added by this slice

The candidate adds tests for:

- deterministic FiscalConfirmationEvidence fingerprint;
- deterministic final content fingerprint;
- tamper detection;
- rejection of line tax evidence not owned by the frozen confirmation evidence;
- architecture isolation from EF/AspNet/XML/signing/transport concerns;
- proof that snapshot creation is a post-identity step and does not alter
  `PrepareFiscalDocumentIdentityUseCase`;
- provider-neutral append-only persistence and 1:1 uniqueness;
- PostgreSQL/MySQL create + read + replay;
- persistence of issuer, receiver, address, line, tax-rate and rule provenance;
- snapshot immutability after later Company/FiscalLocation/Party changes;
- rollback of snapshot/audit/outbox on injected post-flush failure;
- fail-closed behavior for historical identities that lack durable fiscal confirmation evidence.

Exact-head validation for the current PR candidate is `Clean Architecture Guard` run #183
(`34296321607`) for head `bc91e598f997946a5def2a62cf46cbc9aa4476f8`, with synthetic merge
`37e6a7625e2fe235a8f6b1df2a0fdb1c059e2718` against the accepted base. Results:

- .NET SDK 10.0.400 / runtime 10.0.12;
- Release build: 99 warnings, 0 errors;
- known-vulnerability gate: PASS, 0 known vulnerable packages across 10 projects;
- ArchitectureTests: 83/83 PASS;
- CrossCuttingTests: 80/80 PASS;
- legacy UnitTest: 21/21 PASS;
- PersistenceIntegrationTests: 141/141 PASS;
- PostgreSQL actual 16.15;
- MySQL actual 8.4.11;
- total represented automated tests: 325/325 PASS;
- 5-minute per-test blame-hang guard did not trigger.

## Explicit non-scope

Still not implemented by this slice:

- CFE XML construction;
- XML namespaces/tag mapping;
- official XSD loading/runtime validation;
- XML digital signature;
- certificate/private-key custody;
- unsigned/signed artifact archival;
- DGI/provider submission;
- response/acknowledgement interpretation;
- cancellation/correction/credit/debit notes;
- contingency/CFC;
- daily fiscal reporting.

## Next boundary after acceptance

Only after this snapshot foundation is merged and post-merge validated may the next product slice
start:

**CFE XML BUILD**, followed by a separately bounded **XML/XSD VALIDATE** slice using the accepted,
versioned official DGI artifacts.

`IFiscalXmlBuilder` must consume the immutable snapshot produced here and must not use mutable
Company, FiscalLocation, Party, Catalog or TaxProfile records as fiscal authority.
