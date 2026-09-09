# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-09

Accepted functional baseline: `main@578ff84e2696c66fa36726e45f4069672a4c7aaa`
(merge of PR #54, `feat(fiscal): add deterministic unsigned CFE builder`).

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

PR #54 exact-head `Clean Architecture Guard` run #199 (`34371628285`) validated the deterministic
Unsigned CFE Builder candidate before merge:

- restore: PASS;
- NuGet known-vulnerability gate: PASS;
- Release build: PASS;
- ArchitectureTests: 87/87 PASS;
- CrossCuttingTests: 89/89 PASS;
- legacy UnitTest: 21/21 PASS;
- PersistenceIntegrationTests: 149/149 PASS on PostgreSQL 16 and MySQL 8.4;
- total represented automated tests: 346/346 PASS;
- 5-minute per-test `blame-hang` guard did not trigger.

Post-merge `Clean Architecture Guard` run #200 (`34372919507`) also completed successfully on the
accepted `main@578ff84e2696c66fa36726e45f4069672a4c7aaa`.

The remaining warnings are advisory legacy/modernization debt and are not security-gate failures.

## Accepted major v1 boundaries

Accepted slices include:

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
13. Frozen CFE payment-form/settlement evidence.
14. CFE build/sign/validate lifecycle reconciliation after active-XSD inspection.
15. Frozen DGI unit-of-measure evidence from catalog through SaleLine into immutable fiscal content.
16. Deterministic Unsigned CFE Builder for Release-1 e-Ticket (101) and e-Factura (111).

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/`.

## Accepted fiscal boundary after PR #54

The accepted product flow and capabilities now reach:

```text
Sale CONFIRMED
-> FiscalizationRequest PENDING
-> CAE number reserved
-> FiscalDocument IDENTITY_CREATED
-> immutable FiscalContentSnapshot
-> frozen settlement/payment-form and unit-of-measure evidence
-> deterministic unsigned CFE business-content BUILD capability
```

`FiscalDocument` owns server-assigned fiscal identity and accepted CAE/monetary evidence.
`FiscalContentSnapshot` freezes issuer, receiver, line, tax, payment-form and unit evidence so later
mutable master changes cannot rewrite an already-created fiscal document.

`DeterministicUnsignedCfeBuilder` consumes only accepted immutable evidence. It does not own a clock,
certificate, private key, mutable-master lookup, XSD root validation or external transport.

Release-1 builder support currently covers e-Ticket (101) and e-Factura (111). e-Factura
Exportación (121) fails closed until the export-specific immutable evidence required by its official
contract is separately accepted.

No accepted product slice yet establishes `TmstFirma`, produces `ds:Signature`, stores a signed
artifact, performs full official root-XSD validation or submits a CFE externally.

## DGI technical baseline currently used by the consumer

Official DGI publication was rechecked on 2026-09-09:

- `Formato CFE v25.2` is published for Testing and Production;
- `XSDs_FE_V1.44.2` is published for Testing and Production.

Official registry:
`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

Official CFE format:
`https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=`

No XML element, namespace, mandatory-field rule or signature/validation ordering may be inferred from
memory, legacy demo code or provider examples where the official format/XSD is authoritative.

## Accepted CFE lifecycle ordering

Diagnostic PR #51 was intentionally DO-NOT-MERGE and closed without merge after inspecting the active
schema contract. That evidence showed the CFE root requires `ds:Signature`, while `TmstFirma` belongs
to the advanced-signature act. The accepted order is therefore:

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

This ordering is recorded in:

- `documentation/blueprint-architecture/09_FISCAL_INTEGRATION_ARCHITECTURE.md`;
- `documentation/blueprint-api-implementation/29_CFE_BUILD_SIGN_VALIDATE_LIFECYCLE_RECONCILIATION.md`.

## Current candidate: Signing Contract + Durable Signing Evidence

Branch `blueprint/fiscal-signing-evidence` / PR #55 is the current bounded candidate.

Its intended contract is:

- build deterministic unsigned CFE from immutable accepted evidence first;
- hash the unsigned content;
- establish signing time only if no durable signing evidence already exists;
- bind durable signing evidence to FiscalDocument + snapshot fingerprint + unsigned-content SHA-256;
- on replay, rebuild and verify the same fingerprint/hash, then reuse the persisted signing timestamp;
- never obtain a fresh timestamp merely because signing is retried;
- keep certificate/private-key implementation behind a future Infrastructure signing adapter.

The candidate deliberately does not generate `TmstFirma` XML or `ds:Signature` yet and does not cross
a certificate/private-key boundary.

Candidate details are recorded in:
`documentation/blueprint-api-implementation/30_FISCAL_SIGNING_CONTRACT_AND_DURABLE_EVIDENCE.md`.

PR #55 remains non-accepted until exact-head CI is green and explicit human merge approval is given.

## Explicitly not complete

The following remain outside the accepted current product baseline:

- durable signing-time evidence until PR #55 is accepted;
- `TmstFirma` insertion into signing input;
- XML digital signature implementation;
- certificate discovery/selection and certificate-chain policy;
- private-key/HSM/Key Vault custody;
- immutable signed-artifact persistence;
- full official XSD validation of the signed CFE;
- DGI or authorized-provider transport;
- direct-DGI-vs-provider production decision;
- external acceptance/rejection interpretation and fiscal retry lifecycle;
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

Blueprint Master was last reverified for this lineage on 2026-09-08 at
`737556e24195aa909117790f2d7ff0be2fe0a474`, with root `VERSION = 0.5.2` and annotated tag `v0.5.2`
resolving to that same commit.

There is no automatic consumer upgrade. Current consumer classification remains **DEFER formal 0.5.2
adoption** until its separate runtime/label migration items are deliberately executed and approved.
The dedicated review remains in `documentation/BLUEPRINT_0_5_2_CONSUMER_COMPLIANCE_REVIEW.md`.

Historical files identifying evaluator 0.5.1 remain historical evidence and must not be rewritten
merely to display the newer Master version.

## Next bounded implementation sequence

After PR #55 is separately accepted, the current intended sequence remains:

1. **Real signing adapter + Signed Artifact Persistence**
   - consumes the already durable signing timestamp;
   - inserts the signing-time evidence required by the accepted CFE contract;
   - certificate/private-key policy must be explicitly reviewed before integration;
   - freezes resulting signed bytes/hash/metadata independently from transport.
2. **Official Full-XSD Validation**
   - validates the signed artifact against the accepted DGI XSD set;
   - preserves actionable failure evidence and blocks transport on failure.
3. **DGI/provider transport**
   - consumes the already signed and validated artifact;
   - never rebuilds or resigns opportunistically.

## Known non-blocking modernization debt

Current green builds still report legacy/advisory debt including deprecated/outdated dependencies,
Application Insights legacy APIs, `Microsoft.AspNetCore.Http.Abstractions 2.2.0`, legacy Npgsql
extension/design packages, xUnit 2.x deprecation notices, nullable/analyzer warnings, obsolete
cryptography APIs and Windows-only `System.Drawing` usage.

GitHub Actions also reports Node 20 deprecation warnings for action versions currently being forced to
execute on Node 24.

These items remain inventory for later bounded modernization slices and must not be upgraded wholesale
without compatibility analysis.

## Repository governance at this checkpoint

- PR #54 is merged.
- accepted `main`: `578ff84e2696c66fa36726e45f4069672a4c7aaa`.
- PR #54 exact-head Guard #199: SUCCESS, 346/346 represented tests PASS.
- post-merge Guard #200 on accepted main: SUCCESS.
- diagnostic PR #51: CLOSED WITHOUT MERGE.
- current candidate: PR #55, `blueprint/fiscal-signing-evidence`.
- PR #55 must not merge without final exact-head green CI and explicit human approval.
- Blueprint 0.5.2 consumer adoption remains DEFER.
