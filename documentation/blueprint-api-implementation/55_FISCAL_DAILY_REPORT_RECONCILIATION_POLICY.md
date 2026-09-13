# 55 — Fiscal Daily Report Reconciliation Policy

Status: GOVERNED IMPLEMENTATION CANDIDATE

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Purpose

This bounded increment interprets the latest durable `DR`, `ER` or `FR` observation accepted by document 54 and returns an explicit local reconciliation disposition.

It does not create or modify a Reporte Diario version, does not authorize a new `SecEnvio`, does not update transport evidence and does not infer inconsistency details that DGI has not supplied.

## Authoritative DGI evidence

The governing source remains DGI FAQ v22 section 9.7.

The official semantics relevant to this slice are:

- `DR` — **Reporte Procesado**: no inconsistencies were identified by DGI's consistency controls;
- `ER` — **Reporte en Gestión**: inconsistencies were identified; the electronic issuer must analyze them and, **if appropriate**, send a new report reliquidating the previous one;
- `FR` — **Reporte Reliquidado**: the previous report was reliquidated after DGI received a report for the same `FechaResumen` with `secuencia + 1` and the new report satisfied the initial validations.

The phrase "si corresponde" is a deliberate regulatory boundary. An `ER` observation alone proves that inconsistencies exist, but it does not prove their content and does not by itself authorize the consumer to allocate or dispatch a reliquidating report.

## Local reconciliation dispositions

The policy maps the latest durable later-state observation as follows:

- `DR` -> `Consistent`;
- `ER` -> `ManualReviewRequired`;
- `FR` -> `ReliquidatedExternally`.

Each result carries the exact source observation identity, local target identity, organization, issuer RUC, `FechaResumen`, `SecEnvio`, local BR correction revision when applicable, DGI receiver id, original DGI state code and observation timestamp.

The result also carries a stable interpretation code:

- `dgi_processed_no_inconsistencies`;
- `dgi_inconsistencies_require_analysis`;
- `dgi_prior_report_reliquidated`.

## Automatic-action policy

Every assessment produced by this slice has:

- `AutomaticReliquidationAuthorized = false`;
- `AutomaticLocalMutationAuthorized = false`.

Therefore:

- `DR` records that the latest observed DGI state is consistent, but does not rewrite the original submission/revision row;
- `ER` requires manual/operational analysis and does not authorize automatic reliquidation;
- `FR` records that DGI reports the prior report as reliquidated, but does not mutate local version lineage, supersession, accounting or transport evidence.

A future increment may introduce an explicit command after the required evidence for ER inconsistencies or another authoritative reconciliation source is available. That command must remain separately governed.

## Evidence selection

The policy reads the latest append-only observation for an exact:

`OrganizationId + DgiReceiverId`

The infrastructure reader orders by `ObservedAtUtc` descending and uses observation id descending only as a deterministic tie-breaker.

No network call is performed. No DGI state is inferred from timestamps, transport state or local sequence position.

If no durable `DR`/`ER`/`FR` observation exists for the requested receiver, the policy fails closed with a missing-prerequisite conflict.

If persisted evidence contains a later-state enum outside the governed set, the policy fails closed as invalid persisted evidence.

## Architecture boundary

The Application policy depends only on `IFiscalDailyReportLatestObservationReader` and the already accepted immutable observation model.

It does not depend on:

- HTTP or SOAP transport;
- certificates;
- `IUnitOfWork` or `ITransactionManager`;
- Reporte Diario version allocation repositories;
- signing or dispatch services;
- persistence-specific types.

The EF Core repository that already owns later-state observation persistence also implements the read-only latest-observation port using `AsNoTracking`.

No migration is required because this slice does not add mutable state or a new persistence table.

## Validation coverage

The increment requires tests proving:

- `DR` -> `Consistent`;
- `ER` -> `ManualReviewRequired`;
- `FR` -> `ReliquidatedExternally`;
- ER never authorizes automatic reliquidation;
- no disposition authorizes automatic local mutation;
- missing observation evidence fails closed;
- invalid persisted state fails closed;
- root submission and same-`SecEnvio` BR correction targets remain identifiable;
- the infrastructure reader selects the latest observation deterministically without update/delete behavior;
- architecture guards prevent the policy from reaching transport, Unit of Work or version-allocation boundaries.

## Deliberate non-scope

This increment does not implement:

- retrieval or interpretation of ER inconsistency details;
- a human approval workflow for ER remediation;
- automatic or manual command creation for a reliquidating `SecEnvio + 1` report;
- local supersession/accounting mutation after FR;
- R05 sequence recovery;
- automatic retry from ambiguous `Unknown` delivery;
- independent cryptographic validation of DGI ACK/signature evidence;
- Sobre v05 packaging/submission;
- external DGI Testing acceptance;
- Production enablement.
