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

`REQUESTED -> NUMBER_RESERVED -> BUILT -> VALIDATED -> SIGNED -> ARCHIVED`

### Transport state

`NOT_QUEUED -> QUEUED -> SUBMITTED -> ENVELOPE_RECEIVED -> AWAITING_RESULT -> COMPLETED/FAILED`

### Fiscal result state

`PENDING -> ACCEPTED / REJECTED / REGULARIZATION_REQUIRED`

Exact names may change in implementation ADRs, but these concepts cannot be collapsed into one `estadoDgi` string.

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

## XML generation and validation

Builder uses the active fiscal specification version and immutable snapshots.

Validation layers:

1. well-formed XML;
2. official XSD for active version;
3. fiscal business rules;
4. arithmetic/rounding rules;
5. receiver/document applicability;
6. CAE/range checks;
7. XMLDSig/signature verification where applicable.

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

## Provider callbacks / asynchronous results

Inbound messages pass through authentication + inbox deduplication + response interpreter, then invoke an application use case. A callback never directly updates `fiscal_documents` through repository/SQL.

## Daily fiscal report

A scheduled application workflow builds the report from authoritative fiscal/CFC consumption, signs/submits through ports and persists report/acknowledgement state separately from management dashboards.

## Direct DGI vs provider

Still `OPEN` as a deployment/product decision. Architecture supports either without changing Domain/Application contracts.

No provider-specific DTO, status code or SDK type may leak into public domain contracts. Adapters map external responses to canonical result/value objects plus preserved raw artifact/reference for audit/support.
