# Blueprint Candidate Findings from eFactura Brownfield Pilot

## Rule

A problem observed while applying Blueprint to eFactura does **not** automatically change the Master. Findings are candidates until they prove reusable beyond this consumer and do not damage the active Greenfield flow.

Current Master baseline: `SoftwareDevelopmentBlueprint 0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

Current relevant Master checks already include:

- `brownfield.technical_inventory`
- `brownfield.functional_reconstruction`
- `brownfield.gap_analysis`
- `brownfield.target_definition`
- `brownfield.alignment_roadmap`
- Requirements use cases/business rules/traceability
- API idempotency matrix
- client async/error/offline
- integration QA offline when applicable

Therefore a candidate is opened only when these controls are insufficiently explicit for a recurring situation.

## BCF-001 — Reference Capability Ingestion

**Status:** CANDIDATE / evidence accumulating.

**Observed problem:** eFactura is not only being aligned from AS-IS. A second working repository supplies desired capabilities/know-how that are absent from AS-IS. If an agent treats this reference as existing behavior or blindly copies its stack/regulatory assumptions, Target Definition is corrupted.

**Potential generic model:**

```text
OBSERVED_CURRENT
REFERENCE_CAPABILITY
EXTERNAL_NORMATIVE_REQUIREMENT
NEW_PRODUCT_REQUIREMENT
PROPOSED_TARGET
```

**Potential Blueprint improvement:** explicit evidence classification/check/template for Brownfield target ingestion from external/reference systems.

**Do not change Master yet.** First validate whether current `brownfield.target_definition` + requirements traceability can express this adequately when applied rigorously.

## BCF-002 — Regulatory Provenance and Effective-Version Control

**Status:** CANDIDATE / strong domain evidence, generic applicability under review.

**Observed problem:** a reference implementation can contain technically convincing but outdated/simulated regulatory behavior. eFactura requires each fiscal rule/schema to carry authority/source/version/effective dates.

**Generic relevance:** finance, tax, health, legal/compliance and other regulated systems.

**Potential Blueprint improvement:** conditional regulated-system check requiring normative-source provenance, effective dates and revalidation strategy.

**Risk to Greenfield:** should be `CONDITIONAL`, not impose regulatory paperwork on ordinary projects.

## BCF-003 — Offline Business Operation vs Regulated Contingency

**Status:** LEARNING, probably project/domain-specific.

Blueprint already has client offline/idempotency/QA checks. eFactura demonstrates that generic offline queueing may be insufficient in regulated workflows where a legally valid contingency artifact exists.

Likely resolution: project Requirements/Architecture pattern rather than new universal Blueprint phase.

## BCF-004 — Evidence Depth Contract for Brownfield Inspection

**Status:** CANDIDATE / strong evidence from prior agent attempt.

**Observed problem:** a Brownfield inspection can superficially satisfy filenames/check names while producing an “API inventory” with no endpoint-level inventory and a “data model” with only a few examples.

**Potential Blueprint improvement:** strengthen Brownfield evidence templates/schemas with minimum evidence granularity, e.g.:

- technical inventory must enumerate projects/runtime/dependencies;
- API inventory must enumerate route/method/input/output/auth/error;
- data inventory must enumerate entities/tables/relationships/ownership;
- architecture reconstruction must show actual dependency graph;
- claims must be OBSERVED/INFERRED/PROPOSED.

This candidate is highly relevant beyond eFactura and should be evaluated against the Master after the eFactura inspection is properly hardened.

## BCF-005 — Multi-provider persistence conformance

**Status:** PROJECT-SPECIFIC REQUIREMENT, not Blueprint gap.

Blueprint already requires data architecture and QA. Supporting PostgreSQL + MySQL is an eFactura target constraint. It should generate consumer-specific architecture/tests, not a universal Master requirement.

## Current Master decision

**NO BLUEPRINT MASTER CHANGE AUTHORIZED YET.**

Before proposing a Master PR:

1. complete/accept eFactura Brownfield evidence;
2. complete target/requirements evidence;
3. demonstrate the candidate cannot be adequately represented with existing checks/templates;
4. cross-check impact on the active CUSA-Digital Greenfield consumer;
5. propose the smallest backwards-compatible Master change;
6. require explicit human approval.
