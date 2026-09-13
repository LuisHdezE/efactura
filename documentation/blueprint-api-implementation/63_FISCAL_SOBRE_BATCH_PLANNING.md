# 63. Fiscal Sobre Batch Planning

Status: **GOVERNED IMPLEMENTATION CANDIDATE**

Baseline: `main@68c9ec84d39d5cbdca76702acf122322788f10e0`

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Purpose

This increment introduces a bounded local **product policy** for planning already-signed CFE into one or more Sobre batches before the existing packaging boundary is invoked.

The DGI envelope constraints already accepted by this repository remain unchanged:

- one Sobre contains `1..250` CFE/CFC;
- all CFE included in the same Sobre use the same electronic signing certificate.

Current DGI functional material and the current FAQ continue to state those two constraints. This increment does not infer any additional DGI batching algorithm from them.

## Product policy

The caller supplies an explicit ordered set of `FiscalDocumentId` values. The planner:

1. requires every candidate to have a durable signed CFE artifact;
2. fails closed if the durable artifact is missing, cross-organization or lacks certificate evidence;
3. rejects duplicate fiscal-document ids before repository reads;
4. groups candidates by the exact durable certificate thumbprint + serial-number pair;
5. orders certificate groups by the first appearance of that certificate in the caller-supplied candidate sequence;
6. preserves caller order inside each certificate group;
7. splits each certificate group deterministically into chunks of at most 250 CFE;
8. assigns only a local result `BatchOrdinal` for the returned plan.

`BatchOrdinal` is not a DGI identifier and has no fiscal meaning.

## Explicit non-scope

The planner deliberately:

- does not discover pending CFE;
- does not decide which business documents are eligible for submission;
- does not allocate `Idemisor`;
- does not create or reserve a DGI Sobre identity;
- does not generate `EnvioCFE` XML;
- does not persist a Sobre or a batch plan;
- does not call DGI;
- does not retry transport;
- does not interpret ACKSobre, `EstadoCFE`, S08 or any other DGI state;
- does not mutate sale, accounting, inventory or fiscal-document state.

Each returned batch must still pass through the already accepted `PackageFiscalCfeEnvelopeUseCase` and the later durable identity/transport boundaries with an explicit caller-supplied `Idemisor` and creation timestamp.

## Determinism

For the same organization, ordered fiscal-document ids and unchanged durable signing-certificate evidence, the planner returns the same ordered sequence of batches.

The policy intentionally does not reorder documents by fiscal number, timestamp, CFE family, customer, amount or any inferred DGI priority. Those policies are not established by this slice.

## Safety boundary

This increment is a local orchestration aid. It does not expand DGI protocol semantics and does not claim that every locally planned batch is automatically ready for Testing or Production submission.

Formal readiness therefore remains:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Verification

The governed verification target includes:

- cross-cutting tests for stable first-seen certificate grouping;
- preservation of caller order within a certificate group;
- deterministic `250 + 1` splitting for 251 same-certificate candidates;
- duplicate rejection before repository access;
- missing signed-artifact failure;
- fail-closed cross-organization/incomplete certificate evidence;
- architecture tests proving no transport, persistence, XML packaging or `Idemisor` allocation is introduced;
- normal repository Clean Architecture Guard execution on the exact PR head.

The human checkpoint `documentation/BLUEPRINT_CURRENT_STATE.md` is intentionally not reconciled in this functional PR. If this increment is accepted and merged, checkpoint reconciliation remains a separate governance-only change.
