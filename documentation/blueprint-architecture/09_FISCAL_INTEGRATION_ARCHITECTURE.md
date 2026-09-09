# Fiscal Integration Architecture

## Goal

DGI/provider transport is an external adapter. Fiscal business rules and document lifecycle remain application/domain-owned.

## Port decomposition

```text
ApplicationCore Fiscal
  IFiscalRuleCatalog
  IFiscalDocumentSelector
  IFiscalNumberAllocator
  IFiscalXmlBuilder
  IFiscalValidator
  IFiscalSigner
  IFiscalTransportGateway
  IFiscalResponseInterpreter
  ICaeArtifactVerifier
  IFiscalArtifactStore
  IDailyFiscalReportBuilder
```

Infrastructure may provide:

```text
DirectDgiFiscalTransportGateway
AuthorizedProviderFiscalTransportGateway
FakeFiscalTransportGateway (test/homologation only)
CertificateStoreSigner / KeyVaultSigner / ProviderSigner
AzureBlobFiscalArtifactStore / other approved storage adapter
```

The frontend/mobile client never communicates directly with DGI/provider for authoritative issuance.

## Fiscal lifecycle

Separate state dimensions are required.

### Document generation state

The conceptual ordering is:

`REQUESTED -> NUMBER_RESERVED -> CONTENT_FROZEN -> UNSIGNED_BUILT -> SIGNED -> FULLY_VALIDATED -> ARCHIVED`

Exact persisted state names remain subject to bounded implementation ADRs. The ordering is not optional:

- the unsigned build boundary is deterministic and does not own the signing clock, certificate or private key;
- `TmstFirma` belongs to the signing act and must be durable/replay-safe once established;
- the active official CFE root schema requires `ds:Signature`, so complete official root-XSD validation occurs after signature insertion;
- pre-sign well-formedness, business, arithmetic, CAE and other structural checks may run before signing, but they must not be represented as complete official root-XSD validation;
- transport consumes an already signed and fully validated artifact and must not rebuild or resign opportunistically.

The detailed reconciliation and retry semantics are recorded in
`documentation/blueprint-api-implementation/29_CFE_BUILD_SIGN_VALIDATE_LIFECYCLE_RECONCILIATION.md`.

### Transport state

`NOT_QUEUED -> QUEUED -> SUBMITTED -> ENVELOPE_RECEIVED -> AWAITING_RESULT -> COMPLETED/FAILED`

### Fiscal result state

`PENDING -> ACCEPTED / REJECTED / REGULARIZATION_REQUIRED`

These concepts cannot be collapsed into one `estadoDgi` string.

## Numbering

`CaeAuthorization` and allocator enforce:

- applicable CFE type/series;
- validity;
- range bounds;
- operational allocation/subrange when configured;
- atomic next-number reservation;
- unique DB constraint on final fiscal identity;
- no number reuse after an issuance attempt that reached the applicable irreversible boundary.

Provider-specific sequence SQL is Infrastructure-only.

## XML generation, signing and validation

The builder consumes accepted fiscal identity plus immutable fiscal-content evidence and the active accepted fiscal specification version.

The builder must be deterministic for the same accepted inputs. It does not:

- read mutable Company, Location, Party, Catalog, Sale, Payment or Receivable state;
- call the system clock to manufacture `TmstFirma`;
- access certificates or private keys;
- contact DGI or an authorized provider;
- own transport or artifact-storage implementation concerns.

Validation is layered.

### Pre-sign validation/checks

Before signing, implementations may perform:

1. XML well-formedness and serializer invariants that do not require the final signature node;
2. fiscal business rules;
3. arithmetic/rounding rules;
4. receiver/document applicability;
5. CAE/range checks;
6. completeness checks against accepted immutable evidence.

These checks fail closed but are not equivalent to complete validation of the final CFE against the official root XSD.

### Signing boundary

Signing is reached only after the deterministic unsigned content exists.

The signing boundary:

- establishes `TmstFirma` as signing evidence rather than builder input from an ambient clock;
- persists/reuses that signing timestamp according to the accepted retry contract;
- invokes signing through an application port;
- keeps certificate/private-key custody and provider-specific signing mechanics in Infrastructure;
- produces the final XML digital-signature content, including `ds:Signature`.

Private keys never enter Domain or public API contracts.

### Post-sign full validation

The signed CFE is then validated against the complete active official DGI XSD set plus the applicable accepted fiscal invariants.

A full-XSD failure blocks archival-as-valid and transport. It does not authorize CAE renumbering, mutable-master rereads or a fresh `TmstFirma` merely to produce different bytes.

XMLDSig/signature verification may also be applied at this boundary according to the accepted signing implementation.

Reference-demo string/tag checks are never production validation.

## Cross-border selection

Fiscal integration consumes the result of:

`ReceiverIdentityResolver -> CrossBorderTaxTreatmentResolver -> FiscalDocumentSelector`

Foreign customer is not equal to export. Possession of a Uruguayan RUC, transaction nature, goods/services, location/use and active Article-34/export rules are evaluated before CFE family selection.

## Artifact storage

Persist metadata in relational DB and immutable artifact bytes through `IFiscalArtifactStore`.

Metadata includes:
- content hash;
- media/artifact type;
- fiscal document ID;
- schema/spec version;
- signing state/certificate reference, never private key;
- storage locator;
- created/received time;
- source/response type.

Artifact storage must preserve replay identity. A transport retry consumes the accepted stored signed/validated artifact rather than rebuilding it from mutable business state.

## Provider callbacks / asynchronous results

Inbound messages pass through authentication + inbox deduplication + response interpreter, then invoke an application use case. A callback never directly updates `fiscal_documents` through repository/SQL.

## Daily fiscal report

A scheduled application workflow builds the report from authoritative fiscal/CFC consumption, signs/submits through ports and persists report/acknowledgement state separately from management dashboards.

## Direct DGI vs provider

Still `OPEN` as a deployment/product decision. Architecture supports either without changing Domain/Application contracts.

No provider-specific DTO, status code or SDK type may leak into public domain contracts. Adapters map external responses to canonical result/value objects plus preserved raw artifact/reference for audit/support.
