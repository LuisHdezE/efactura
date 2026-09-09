# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-09

Accepted functional baseline: `main@caaf321cce61a539a0fccdb9f274771020487b18`
(merge of PR #50, `feat(fiscal): freeze CFE payment form evidence`).

This file is the current human-readable checkpoint for the eFactura brownfield modernization.
It does not replace requirements, architecture, API-contract or numbered implementation records.
Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation evidence
and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Current workflow selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Deprecated/outdated package inventories remain advisory modernization evidence.

## Post-merge evidence for the accepted baseline

`Clean Architecture Guard` run #190 (`34305896396`) validated
`caaf321cce61a539a0fccdb9f274771020487b18` after PR #50 merged:

- runner: `efactura-ci-01` on `Elena`;
- .NET SDK: `10.0.400`;
- .NET runtime observed: `10.0.12`;
- restore: PASS;
- NuGet known-vulnerability gate: PASS, 0 known vulnerable packages across all 10 projects;
- Release build: PASS, 99 warnings, 0 errors;
- ArchitectureTests: 86/86 PASS;
- CrossCuttingTests: 85/85 PASS;
- legacy UnitTest: 21/21 PASS;
- PersistenceIntegrationTests: 145/145 PASS on PostgreSQL 16 and MySQL 8.4;
- total represented automated tests: 337/337 PASS;
- 5-minute per-test `blame-hang` guard did not trigger.

The warnings remain advisory legacy/modernization debt and are not security-gate failures.

## Implemented and accepted v1 boundaries

Accepted slices now include, among the major bounded capabilities:

1. Sales draft, validation and fiscal preview.
2. Inventory availability and controlled stock adjustment.
3. CAE authorization/allocation and atomic fiscal-number reservation.
4. Runtime/CI modernization to .NET 10 plus dependency/security gating.
5. Authoritative Release-1 tax treatment, VAT/CFE eligibility and CFE 25.2 arithmetic foundations.
6. Sale confirmation planning, settlement planning and Payment/Receivable persistence foundation.
7. Atomic local sale confirmation with tracked-stock and durable fiscalization effects.
8. Public `API-SAL-007 confirmSale`.
9. Fiscal Document Identity Foundation.
10. Organization Fiscal Issuer Profile Foundation.
11. Party address/contact contract completion needed by fiscal receiver evidence.
12. Immutable Fiscal CFE Content Snapshot Foundation.
13. Frozen CFE payment-form/settlement evidence accepted by PR #50.

Detailed bounded evidence is maintained in numbered files under
`documentation/blueprint-api-implementation/`.

## Cross-cutting capabilities now present

For the new v1 write path, accepted slices include:

- Clean Architecture guards;
- provider-neutral transactional persistence;
- durable audit and outbox evidence;
- actor/correlation foundations;
- idempotency/replay protection;
- optimistic and unique concurrency guards;
- PostgreSQL/MySQL integration coverage;
- authoritative sale confirmation with Payment/Receivable, tracked inventory and one durable
  `FiscalizationRequest`;
- deterministic fiscalization retry identity based on `FiscalizationRequestId`;
- atomic CAE-backed number reservation plus immutable `FiscalDocument` identity snapshot;
- rollback of number reservation, fiscal document, workflow state, audit and outbox as one local
  transaction;
- issuer fiscal master data through `CompanyFiscalProfile` and `FiscalLocation`;
- receiver address/contact evidence through the completed Party contract;
- immutable, tamper-evident fiscal content evidence that survives later mutable-master changes;
- authoritative frozen fiscal settlement/payment-form evidence for later CFE generation.

These capabilities do not imply that every legacy endpoint has been migrated to the same standards.

## Accepted fiscal boundary after PR #50

The accepted product flow reaches:

```text
Sale CONFIRMED
-> FiscalizationRequest PENDING
-> CAE number reserved
-> FiscalDocument IDENTITY_CREATED
-> immutable FiscalContentSnapshot available
-> fiscal settlement/payment-form evidence frozen
```

The accepted `FiscalDocument` identity owns server-assigned fiscal identity and accepted evidence,
including CFE type, series/number, CAE provenance, fiscal date, format version,
confirmation/settlement fingerprints and authoritative monetary totals.

The accepted immutable content evidence freezes the issuer/receiver/line/tax inputs required by later
CFE construction so that mutable Company, Location, Party, Catalog or Sale changes cannot silently
rewrite an already-created fiscal document.

No accepted product slice yet produces a built, signed, fully XSD-validated or submitted CFE.

## DGI technical baseline currently used by the consumer

Official DGI publication was rechecked on 2026-09-09 before selecting the next implementation slice:

- `Formato CFE v25.2` is published for Testing and Production;
- `XSDs_FE_V1.44.2` is published for Testing and Production.

Official registry:
`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

Official CFE format:
`https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=`

No XML element, namespace, mandatory-field rule or signature/validation ordering may be inferred from
memory, legacy demo code or provider examples where the official format/XSD is authoritative.

## CFE lifecycle reconciliation discovered before implementation

A bounded diagnostic inspection was performed after PR #50 because the prior architecture planned:

`BUILD -> FULL XSD VALIDATE -> SIGN`

The active official schema evidence shows that the CFE root requires `ds:Signature`, while the active
format defines `TmstFirma` as the timestamp of the advanced electronic signature. A complete unsigned
CFE therefore cannot satisfy the complete official root-XSD contract.

Diagnostic PR #51 was intentionally non-product evidence and was closed on 2026-09-09 **without
merge**. `main` remained at the accepted PR #50 baseline.

The reconciled conceptual order is now:

```text
FiscalDocument identity + immutable FiscalContentSnapshot
-> deterministic unsigned CFE build
-> optional pre-sign structural/business checks
-> signing boundary establishes durable/replay-safe TmstFirma
-> XML digital signature / ds:Signature
-> full validation against the active official DGI XSD set
-> immutable artifact archival
-> later DGI/provider transport
```

This ordering is documented in:

- `documentation/blueprint-architecture/09_FISCAL_INTEGRATION_ARCHITECTURE.md`;
- `documentation/blueprint-api-implementation/29_CFE_BUILD_SIGN_VALIDATE_LIFECYCLE_RECONCILIATION.md`.

The reconciliation is documentation/architecture only. It does not advance `FiscalDocument` state or
implement XML, signing, certificates, XSD validation or transport.

## Explicitly not complete

The following remain outside the accepted current product baseline:

- deterministic unsigned CFE XML generation;
- signing contract and durable signing-time evidence;
- XML digital signature implementation;
- certificate/private-key custody;
- immutable signed-artifact archival;
- full official XSD validation of the signed CFE;
- DGI or authorized-provider transport;
- direct-DGI-vs-provider production decision;
- synchronous receipt and asynchronous acceptance/rejection interpretation;
- fiscal regularization/retry lifecycle after external submission;
- `API-SAL-008 cancelSale`;
- `API-SAL-009 getSaleFiscalizationStatus`;
- public `API-FIS-*` document routes;
- credit/debit/correction notes;
- contingency/CFC lifecycle;
- daily fiscal reporting/homologation evidence;
- general receivable collection/payment allocation workflow;
- accounts payable/procurement/treasury/cash-management completion.

Regulatory decisions explicitly left open in accepted fiscal/tax documents remain open until
separately reviewed against current official evidence.

## Blueprint evaluator checkpoint

Accepted historical eFactura evidence remains governed by:

- Blueprint evaluator version: `0.5.1`;
- exact evaluator commit: `ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`;
- annotated evaluator tag: `v0.5.1`.

Blueprint Master was last reverified for this checkpoint lineage on 2026-09-08:

- repository: `LuisHdezE/SoftwareDevelopmentBlueprint`;
- then-current `main`: `737556e24195aa909117790f2d7ff0be2fe0a474`;
- root `VERSION`: `0.5.2`;
- annotated tag `v0.5.2` resolved to the same stable commit.

There is no automatic consumer upgrade from 0.5.1 to 0.5.2.

The dedicated consumer Compliance Review remains recorded in:
`documentation/BLUEPRINT_0_5_2_CONSUMER_COMPLIANCE_REVIEW.md`.

Current consumer classification remains **DEFER formal 0.5.2 adoption** until its separate runtime
migration items are deliberately executed and approved. That governance/portability work is not a
product blocker for the bounded fiscal implementation sequence.

Historical files that correctly identify evaluator 0.5.1 remain historical evidence and must not be
rewritten merely to display the newer Master version.

## Corrected next implementation sequence

After acceptance of the lifecycle reconciliation candidate, implementation should proceed through
small independent slices rather than a combined XML/signing/transport service:

1. **Unsigned CFE Builder**
   - consumes only accepted fiscal identity plus immutable fiscal content/evidence;
   - uses the active accepted fiscal specification;
   - has no ambient clock, certificate/private key or transport dependency.
2. **Signing Contract + Durable Signing Evidence**
   - defines application-owned signing orchestration;
   - establishes and durably reuses `TmstFirma` across retry/replay;
   - still keeps real certificate/private-key mechanics behind Infrastructure ports.
3. **Signed Artifact Persistence**
   - freezes signed bytes/hash/metadata independently from transport.
4. **Official Full-XSD Validation**
   - validates the signed artifact against the active accepted DGI XSD set;
   - blocks transport and preserves actionable evidence on failure.
5. **DGI/provider transport**
   - consumes the already signed and validated artifact;
   - does not rebuild or resign opportunistically.

The immediate candidate slice is therefore:

**CFE BUILD / SIGN / VALIDATE Lifecycle Reconciliation**

It is intentionally documentation-only and must receive the normal PR review/human acceptance before
its ordering becomes the accepted consumer checkpoint.

## Known non-blocking modernization debt

The current build is green but still reports legacy/advisory debt, including deprecated/outdated
dependencies, Application Insights legacy APIs, `Microsoft.AspNetCore.Http.Abstractions 2.2.0`,
legacy Npgsql extension/design packages, xUnit 2.x deprecation notices, nullable/analyzer warnings,
obsolete cryptography APIs and Windows-only `System.Drawing` usage.

GitHub Actions also reports Node 20 deprecation warnings for action versions currently being forced to
execute on Node 24.

These items are inventory for later bounded slices. They must not be upgraded wholesale without
compatibility analysis.

## Documentation interpretation

- `documentation/blueprint-brownfield/01..12`: historical AS-IS, gap and remediation artifacts.
- `documentation/blueprint-api-implementation/`: implementation evidence and bounded planning by
  numbered slice.
- `documentation/BLUEPRINT_0_5_2_CONSUMER_COMPLIANCE_REVIEW.md`: consumer evaluator/version review.
- this file: current accepted operational checkpoint plus the currently selected candidate slice.

Where an old gap matrix or implementation note conflicts with later merged evidence, the historical
row remains useful as provenance while this checkpoint plus later accepted records describe current
repository reality.

## Repository governance at this checkpoint

- PR #50 is merged.
- accepted `main`: `caaf321cce61a539a0fccdb9f274771020487b18`.
- post-merge Clean Architecture Guard #190: SUCCESS, 337/337 represented tests PASS.
- diagnostic PR #51: CLOSED WITHOUT MERGE.
- the Blueprint 0.5.2 adoption decision remains DEFER, not ADOPT.
- local `qa/` / Postman work is not represented as an accepted repository gate here.
- branch `blueprint/cfe-build-sign-validate-lifecycle` is the bounded candidate for lifecycle
  reconciliation and requires explicit human approval before merge.
