# W1.5 Terminals runtime closure

Status: `IMPLEMENTED / MERGED / CI_ACCEPTED / DEPLOYED / PRODUCTION_MIGRATED / RUNTIME_ACCEPTED / CLOSED`

Closure date: 2026-09-21.

Scope: `API-ORG-007..010` plus the accepted active-terminal dependency invariant on `API-ORG-006 updateLocation`.

## Governed implementation and merge evidence

- Implementation PR: #195, `feat(api): implement W1.5 terminals`.
- Approved implementation HEAD: `f954c7e21268bf9fc1a6e3f3fbcdb0cd87850c71`.
- Implementation merge commit: `11139c7c4adf4037c2c2c279dedd302a6b4c7958`.
- Post-merge Clean Architecture Guard #675, run `35560423073`, accepted on `bc138d6076a6d9f2f8f948171082511e9385b39f`.
- Guard result: SUCCESS, including restore, NuGet vulnerability gate, inventories, build, Clean Architecture guard, API v1 cross-cutting tests, legacy unit tests, and PostgreSQL/MySQL transactional integration tests.

## Accepted deployment

- Cloud Run service: `efactura-api` in `us-east5`.
- Deployment workflow run: `35560340904`.
- Accepted revision: `efactura-api-d22-11139c7-54-1`.
- Accepted image digest: `sha256:5486db57ede9881050d360d56b94521d5b991fb3917dd9f1de0a503365f00b37`.
- Runtime acceptance verified that this revision still carried 100% production traffic before any W1.5 QA mutation.

## Production migration

Migration `20260921024500_V1Terminals` was explicitly approved and promoted to Neon production before runtime acceptance.

Independent production verification confirmed:

- migration history row exists exactly once;
- ProductVersion is `8.0.30`;
- terminal schema, keys, FK and indexes match the governed migration;
- application-role CRUD permissions are present;
- prerequisite Wave 1 tables remain present.

## Runtime acceptance

Final governed runtime workflow:

- run id: `35612527016`;
- merge/main SHA that triggered the accepted run: `77bce93936d6ea21d0ab7e7b1d819950cf8b6bc1`;
- result: SUCCESS;
- accepted Cloud Run revision: `efactura-api-d22-11139c7-54-1`.

The workflow passed:

- Swagger/OpenAPI availability;
- exact four Terminal operationIds and no public DELETE;
- 401 without JWT and with malformed JWT;
- 403 permission and organization-scope boundaries;
- governed Company Fiscal Profile prerequisite provisioning through public HTTP only;
- fiscal-location creation and lifecycle prerequisites;
- terminal registration, normalization and deterministic ordering;
- deterministic idempotent replay;
- same-key changed-payload conflict;
- duplicate normalized-code conflict;
- missing and cross-organization location masking;
- cross-organization Terminal masking;
- inactive-location registration and reactivation rejection;
- reassignment;
- optimistic concurrency stale-version conflict;
- inactive Terminal rename while its Location is inactive;
- active-terminal protection when deactivating a Location;
- final active/inactive list behavior;
- DELETE not exposed;
- Wave 1 regression reads for actor, permissions, roles, users, parties and governed reference data.

Final accepted QA projection:

- organization A: `w15-qa-35612527016-a`;
- organization B: `w15-qa-35612527016-b`;
- Location A1: `eda0fda25b964e06a36f2370fa08302e`, inactive, version 2;
- Location A2: `e786120819e1409d9d30688e5559782b`, inactive, version 4;
- Location B1: `9170cdd710814338bb203f265cf08e25`, inactive, version 2;
- Terminal T1: `6ea871509b4f4fc68d22eb6544ad44a7`, code `W15.35612527016.B`, Location A2, inactive, version 4;
- Terminal T2: `f60bb7666b9342abbd022f4d0072d2a5`, code `W15.35612527016.A`, Location A1, inactive, version 2;
- successful Terminal mutations: 6.

## Independent Neon evidence

Read-only production inspection after the successful workflow confirmed:

- exactly two W1.5 QA Terminals exist in organization A and both are inactive;
- T1 is version 4 and assigned to A2;
- T2 is version 2 and assigned to A1;
- all three QA Locations are inactive with expected versions A1=2, A2=4, B1=2;
- both isolated QA Company Fiscal Profiles exist at version 1;
- Terminal audit evidence in organization A is exactly 6 events:
  - `ORGANIZATION_TERMINAL_REGISTERED`: 2;
  - `ORGANIZATION_TERMINAL_UPDATED`: 4;
- all six Terminal audit events were written by `w15-runtime-actor-a`;
- Terminal outbox evidence in organization A is exactly 6 messages of type `EFactura.Application.Organizations.TerminalChangedIntegrationEvent`;
- Terminal idempotency evidence in organization A is exactly 6 completed records:
  - register scope: 2;
  - T1 update scope: 3;
  - T2 update scope: 1;
- zero non-completed Terminal idempotency residue remains for organization A;
- organization B contains zero Terminal rows, zero Terminal audit events and zero Terminal outbox messages;
- migration `20260921024500_V1Terminals` remains present with ProductVersion `8.0.30`.

## Wave 1 closure

Wave 1 is therefore formally closed at `30 / 30` implemented operations.

The global v1 implementation accounting remains:

- implemented: `64 / 194` (`32.99%`);
- missing HTTP: `128`;
- contract collisions: `2`;
- non-implemented: `130`.

The next API-completion work begins from Wave 2. This closure does not imply that later waves are production-ready.

## Operational cleanup

The W1.5 runtime workflow and its two one-shot Python runners were intentionally temporary acceptance machinery. Once this evidence was captured, they became unnecessary production-write capability and are removed by the same closure PR. The durable evidence is this document plus GitHub Actions history and Neon production state.