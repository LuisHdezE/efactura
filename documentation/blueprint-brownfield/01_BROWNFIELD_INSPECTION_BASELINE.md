# Brownfield Inspection Baseline

## Evaluation baseline

- **Consumer repository:** `LuisHdezE/efactura`
- **Consumer branch inspected:** `main`
- **Consumer baseline SHA:** `a6c9bf96572b8a0a88efde2c68b0749a71020a18`
- **Inspection date:** 2026-08-28
- **Evaluator repository:** `LuisHdezE/SoftwareDevelopmentBlueprint`
- **Evaluator branch:** `main`
- **Evaluator exact SHA:** `ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`
- **Evaluator VERSION:** `0.5.1`
- **Evidence branch:** `blueprint/brownfield-inspection`

## Status

Blueprint 0.5.1 is used here as an **evaluation framework**. `efactura` has **not** yet formally adopted Blueprint 0.5.1 and no Blueprint gate is marked PASS by this document set.

## Technology Preservation Constraint

The Blueprint process does not authorize replacement of eFactura's existing technology stack. Improvements must remain primarily within the existing .NET/C#/ASP.NET Core ecosystem and its current architectural direction. Any future technology replacement requires a separate architectural decision, evidence of necessity and explicit human approval.

## Evidence discipline

Claims are classified as:

- `OBSERVED`: directly evidenced in repository content or executed verification.
- `INFERRED`: reasonable interpretation requiring further confirmation.
- `PROPOSED`: future change, never part of AS-IS.

Reference/demo repositories and later target-design documents are **not** AS-IS evidence for this baseline.

## Inspection boundary

This branch contains documentation only. No production source, configuration, tests, Dockerfile, database or runtime behavior is modified by the inspection.
