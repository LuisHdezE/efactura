# 29 - CFE BUILD / SIGN / VALIDATE Lifecycle Reconciliation

Status: **CANDIDATE / NOT ACCEPTED UNTIL MERGE**

Date: 2026-09-09

Accepted predecessor baseline: `main@caaf321cce61a539a0fccdb9f274771020487b18`
(merge of PR #50, Fiscal Payment Form Evidence Prerequisite).

Blueprint evaluator for this consumer remains:
`0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

## Why this reconciliation exists

After PR #50, the next planned slice was CFE XML BUILD followed by full XSD validation and later
signature. Before implementing that boundary, the active official DGI technical publication and the
current XSD set were rechecked.

Official registry rechecked on 2026-09-09:

- `Formato CFE v25.2` remains published for Testing and Production;
- `XSDs_FE_V1.44.2` remains published for Testing and Production.

Official registry:
`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

Official CFE format:
`https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=`

A bounded diagnostic branch/PR (#51) was used only to inspect the active schema contract. It was
closed without merge after producing the needed evidence. No diagnostic test or schema-inspection
code is accepted product code.

The evidence changes the lifecycle ordering, not the already-accepted fiscal identity or immutable
content evidence.

## Contract discovered

The active CFE root schema requires an XML Digital Signature element (`ds:Signature`) as part of the
CFE document. Therefore an unsigned CFE cannot satisfy the complete official root XSD contract.

The active CFE format defines `TmstFirma` as the date and time of the advanced electronic signature.
It is therefore signing evidence, not a deterministic business-content builder field.

The prior conceptual order:

`BUILD -> FULL XSD VALIDATE -> SIGN`

is not accepted going forward.

## Reconciled lifecycle

The canonical conceptual flow is:

```text
FiscalDocument IDENTITY_CREATED
        +
FiscalContentSnapshot
        |
        v
Deterministic unsigned CFE business-content build
        |
        +--> optional pre-sign structural/business checks
        |
        v
Signing boundary
        +--> establish durable TmstFirma
        +--> reuse the same TmstFirma on retry
        +--> apply XML digital signature
        +--> produce ds:Signature
        |
        v
Signed CFE artifact
        |
        v
Full validation against the active official DGI XSD set
        |
        v
Validated signed fiscal artifact
        |
        v
Archive-ready artifact
        |
        v
Later DGI/provider transport
```

The exact persisted state names remain an implementation decision for later slices. This document
fixes the ordering and ownership constraints that those states must preserve.

## Boundary ownership

| Concern | Owner | Rule |
| --- | --- | --- |
| Fiscal identity, CFE type, series, number, CAE provenance | accepted fiscal identity boundary | Never reallocated by XML/signing retry |
| Issuer/receiver/line/tax/payment-form evidence | `FiscalContentSnapshot` / accepted immutable evidence | Never reconstructed from mutable masters during build/retry |
| Unsigned CFE serialization | future deterministic builder | No system clock, certificate, private key, transport or mutable master lookup |
| Pre-sign checks | future builder/validator boundary | May fail closed before signing; never claimed as complete official root-XSD validation |
| `TmstFirma` | signing boundary | Created for the signing act, persisted durably, reused on replay/retry |
| XML digital signature / `ds:Signature` | signing adapter behind an application port | Certificate/private-key implementation remains Infrastructure-owned |
| Full official XSD validation | post-sign validation boundary | Runs against the signed CFE including required signature content |
| Artifact archival | artifact-store boundary | Preserves immutable bytes/hash/metadata; storage technology remains adapter-specific |
| DGI/provider submission | transport boundary | Cannot run before the signed artifact passes the required validation gate |

## Determinism and replay

The unsigned builder must be deterministic for the same accepted fiscal identity plus immutable
content snapshot and accepted specification/rule version. It must not call `DateTime.UtcNow`, read a
certificate store, access a private key, contact DGI/provider services or reread Company, Location,
Party, Catalog, Sale, Payment or Receivable mutable state.

`TmstFirma` is intentionally different. It belongs to the signing act. Once a signing attempt reaches
the durable signing-time boundary, replay/retry must reuse the same persisted signing timestamp for
that signing operation instead of silently manufacturing a new fiscal artifact identity through a
fresh clock read.

A later implementation slice must define the exact transaction/storage mechanism for that durable
signing evidence before real signing is introduced.

## Failure semantics

### Build failure

- no signing timestamp is created;
- no certificate/private-key boundary is crossed;
- the accepted fiscal number and immutable content evidence remain unchanged;
- retry rebuilds from the same accepted immutable inputs.

### Signing failure

- if failure occurs before durable signing evidence exists, a later signing attempt may establish the
  signing evidence according to the separately accepted signing contract;
- if `TmstFirma` has already been durably established for the signing operation, retry reuses it;
- no CAE number is reallocated and immutable business/fiscal content is not reread from mutable state.

### Full-XSD validation failure

- transport is blocked;
- the signed artifact and validation evidence must be preservable for diagnosis according to the
  future artifact-storage design;
- validation failure does not authorize rebuilding from newer master data, renumbering the CFE or
  changing `TmstFirma` merely to obtain a different validation result.

### Transport failure

Transport/retry behavior remains outside this slice. It must consume the already signed and validated
artifact rather than rebuild or resign it opportunistically.

## Relationship to accepted documents 26-28

This reconciliation does **not** invalidate their accepted evidence boundaries:

- pre-XML authoritative fiscal prerequisites remain required;
- `FiscalContentSnapshot` remains immutable and replay-safe;
- frozen fiscal settlement/payment-form evidence remains authoritative;
- historical compatibility rules remain unchanged.

It **does** supersede any lifecycle-order wording that implies complete official XSD validation occurs
before XML digital signature.

## Product code impact of this slice

None by design.

This candidate does not modify `FiscalDocument`, database schemas, repositories, use cases, XML
serialization code, signature code, certificate handling or transport. The purpose is to prevent the
next implementation slices from encoding an invalid lifecycle order.

## Explicit non-scope

This reconciliation does **not** implement:

- XML element/tag construction;
- namespace/canonicalization details;
- a concrete unsigned CFE builder;
- a concrete XSD validation engine;
- certificate discovery/selection;
- private-key custody;
- XMLDSig algorithms or canonicalization policy;
- signing provider/HSM/Key Vault integration;
- signing-artifact persistence schema;
- immutable artifact-store implementation;
- DGI/provider transport;
- synchronous/asynchronous DGI response interpretation;
- credit/debit/correction notes, contingency/CFC or daily fiscal reporting.

## Next bounded slices

After this reconciliation is accepted, implementation should proceed in independent reviewable
slices:

1. **Unsigned CFE Builder**
   - consumes only `FiscalDocument` identity plus `FiscalContentSnapshot` and accepted immutable
     fiscal evidence;
   - produces deterministic unsigned CFE content;
   - fails closed on missing/ambiguous required evidence.
2. **Signing Contract + Durable Signing Evidence**
   - defines the application port and signing-time ownership;
   - persists/reuses `TmstFirma` before real certificate/private-key integration is accepted.
3. **Signed Artifact Persistence**
   - defines immutable signed bytes/hash/metadata and replay behavior.
4. **Official Full-XSD Validation**
   - validates the signed artifact against the active accepted official DGI XSD set;
   - preserves actionable failure evidence and blocks transport on failure.
5. **DGI/provider transport**
   - remains later and must consume the already signed/validated artifact.

## Decision

Do not implement a builder that owns the clock or signing key, and do not claim complete official
root-XSD validation for an unsigned CFE.

The accepted target lifecycle is deterministic unsigned build, signing with durable `TmstFirma`, full
post-sign official XSD validation, artifact archival, and only then external transport.
