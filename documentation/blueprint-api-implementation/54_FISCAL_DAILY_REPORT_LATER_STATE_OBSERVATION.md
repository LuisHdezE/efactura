# 54 — Fiscal Daily Report Later-State Observation

Status: GOVERNED IMPLEMENTATION CANDIDATE

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Purpose

This bounded increment adds durable observation of later DGI Reporte Diario states for an already-known `IdReceptor`.

It does not mutate the local transport lifecycle. The capability records what DGI currently reports through `EFACCONSULTARENVIOSREPORTE` and deliberately separates that external observation from any later business decision that could alter local fiscal state, authorize a reliquidation, or expose operational remediation.

## Authoritative DGI evidence

Two official sources govern this increment.

### Servicios Web Externos DGI

Current reviewed source:

- code `T-5.020.00.001-000005`;
- version `1.9`;
- date `13/05/2024`;
- service `ws_consultas`;
- method `EFACCONSULTARENVIOSREPORTE`.

The method accepts `FechaResumen`, optional `Secuencia` and optional `IdEmisor`, and returns `Ackconsultaenviosreporte / ColeccionDatosReporte / DatosReporte`. Each returned report row carries:

- `IdEmisor`;
- `IdReceptor`;
- `Estado`;
- `FechaHoraRecepcion`.

The accepted transport adapter from document 53 sends authoritative local `FechaResumen + Secuencia`, omits non-authoritative optional `IdEmisor`, preserves the raw response evidence, and keeps endpoint/SOAPAction in external configuration.

### DGI FAQ v22, section 9.7

The official FAQ defines the Reporte Diario verification states used by this capability:

- `AR` — **Reporte Recibido**: the file satisfies the initial validations;
- `BR` — **Reporte Rechazado**: the report is rejected and is not taken as a valid report for sequence purposes;
- `DR` — **Reporte Procesado**: no inconsistencies were identified in the consistency checks and the report moves from Recibido to Procesado;
- `ER` — **Reporte en Gestión**: inconsistencies were identified; the electronic issuer must analyze them and, when appropriate, send a new report that reliquidates the previous one;
- `FR` — **Reporte Reliquidado**: when a report with the same FechaResumen and sequence + 1 is received and accepted, the previous report changes from AR, DR or ER to FR.

The FAQ semantics are used only to type the observed state. They do not justify invented automatic remediation or state-transition commands.

## Target resolution

Observation starts from a durable DGI `IdReceptor` already known to the consumer.

The existing consultation target reader resolves that receiver id to exactly one:

1. root Reporte Diario submission; or
2. same-`SecEnvio` BR correction revision.

A receiver id learned through the accepted document-53 discovery evidence is also resolvable back to its original local target.

Missing or ambiguous local resolution fails closed before calling DGI.

## Exact external row matching

The use case queries `EFACCONSULTARENVIOSREPORTE` with the authoritative local `FechaResumen + Secuencia` of the resolved target.

Unlike receiver discovery, this capability does not subtract sets or infer which DGI row belongs to the local target. The target receiver id is already known.

Therefore the response must contain **exactly one** `DatosReporte` row whose `IdReceptor` equals the requested durable receiver id.

- zero matching rows: fail closed;
- duplicate matching rows: fail closed;
- unrelated rows in the same returned collection: ignored;
- time proximity and collection order are never association criteria.

## Governed later-state boundary

Only these returned state codes create a durable later-state observation:

- `DR` -> `Processed`;
- `ER` -> `InManagement`;
- `FR` -> `Reliquidated`.

`AR` and `BR` are not persisted as later-state observations. They mean DGI still reports an immediate/original state and the later-state capability returns `later_state.not_available`.

Any state code outside the governed `AR / BR / DR / ER / FR` set fails closed as unsupported external evidence.

## Durable evidence

Table:

`v1_fdr_later_state_observations`

Each append-only observation preserves:

- exactly one local target identity (`RootSubmissionId` or `BrCorrectionRevisionId`);
- organization, issuer RUC, FechaResumen and Secuencia;
- local correction revision when applicable;
- caller `OperationId`;
- returned DGI `IdEmisor`;
- exact DGI `IdReceptor`;
- typed later state plus original DGI state code;
- exact `FechaHoraRecepcion` text returned by DGI;
- raw `Ackconsultaenviosreporte` XML;
- SHA-256 hash of that XML;
- observation timestamp.

Organization + operation id is unique. Replaying the same operation id for the same receiver returns the durable observation and does not call DGI again. Reusing the operation id for another receiver fails closed.

Multiple observations of the same receiver are allowed because a valid report may evolve from AR to DR or ER, and a previous AR/DR/ER report may later become FR after an accepted sequence + 1 report.

## No automatic mutation

This increment does **not** rewrite:

- root submission transport state;
- BR correction revision transport state;
- immediate ACK evidence;
- original-response consultation evidence;
- receiver-discovery evidence;
- sequence authorization;
- retry policy.

In particular:

- observing `DR` does not automatically mark a local row as a new persisted transport enum;
- observing `ER` does not automatically create a reliquidation or correction command;
- observing `FR` does not invent local supersession/reversal semantics.

Those are separate governed decisions.

## Persistence boundary

The later-state record is registered in the canonical EF Core v1 model through a small model-customizer extension that delegates first to the existing `V1PersistenceModelCustomizer`. The observation repository uses `Set<V1FiscalDailyReportLaterStateObservationRecord>()`, no-tracking reads and tracked `AddAsync` writes only.

The repository does not issue raw SQL, call `SaveChanges`, or create its own transaction. Commit and transaction ownership remain with the existing `IUnitOfWork` / `ITransactionManager` boundary used by the Application use case.

Foreign keys to root submissions and BR correction revisions are restrictive. The model exposes no navigation from the observation evidence into a state-changing aggregate workflow, and no cascade is introduced that could turn observation into local fiscal-state mutation.

## Validation coverage

The increment requires:

- DR/ER/FR typing and append-only persistence;
- AR/BR `not_available` behavior;
- unsupported-state fail-closed behavior;
- exact receiver matching with zero/duplicate rejection;
- operation replay without repeated DGI query;
- root and BR-correction target support;
- PostgreSQL/MySQL provider-real round-trip;
- PostgreSQL/MySQL operation-id uniqueness;
- multiple observations for the same receiver;
- proof that source root/revision state and ACK remain unchanged;
- architecture guards preventing Application HTTP/X509 dependencies, raw-SQL persistence drift and update/delete semantics in the observation repository.

The exact final PR head must pass the complete Clean Architecture Guard before review or merge.

## Deliberate non-scope

This increment does not implement:

- local state mutation driven by observed DR/ER/FR;
- ER inconsistency-detail retrieval or interpretation;
- automatic creation of a reliquidating report after ER;
- automatic R05 sequence recovery;
- automatic retry from ambiguous `Unknown` delivery;
- independent cryptographic validation of DGI ACK/signature evidence;
- Sobre v05 packaging/submission;
- external DGI Testing acceptance;
- Production enablement.

Those remain separately governed capabilities.
