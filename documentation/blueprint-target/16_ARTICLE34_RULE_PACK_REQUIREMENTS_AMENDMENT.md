# Article 34 Release-1 Rule Pack Requirements Amendment

## Purpose

Temporary controlled amendment pending reconciliation into the main Requirements/Acceptance baseline after human approval of the implementation slice.

## Functional requirements

- **FR-091** The system shall resolve VAT territoriality from explicit transaction facts before evaluating export-of-services eligibility.
- **FR-092** Release 1 shall support Article 34 numeral 11(a-d) through explicit, versioned rule evidence and shall fail closed for unsupported Article 34 families.
- **FR-093** Article 34 numeral 11 shall not infer `person of the exterior`, foreign economic relation, exclusive use abroad, free-zone installation or provider origin solely from nationality, residence country, tax residence country or destination country.
- **FR-094** Numeral 11(a) shall require explicit evidence/status for recipient-abroad, qualifying foreign activity/asset/right relation and exclusive use abroad.
- **FR-095** Numeral 11(b-d) shall support both the exterior-recipient/exclusive-use path and the current free-zone path from non-free national territory, with explicit evidence/status for the selected path.

## Business rules

- **BR-021** Unknown regulated facts never default to export treatment; they produce `REQUIRES_REVIEW`.
- **BR-022** A negative result for one Article 34 path does not imply another alternative path is false unless its required facts are also known.
- **BR-023** Tax-treatment classification does not contain or imply a VAT percentage; rates remain a separate TaxProfile/rate-resolution responsibility.
- **BR-024** Tax-treatment resolution does not choose a CFE family; CFE selection remains a subsequent versioned policy.

## Acceptance criteria

- **AC-041** A foreign customer receiving advisory services in Uruguay does not qualify under 11(a) when exclusive use abroad is explicitly not met.
- **AC-042** A qualifying 11(a) decision records the exact `UY-IVA-D220-ART34-11-A` source evidence.
- **AC-043** Custom software under 11(b) qualifies through the exterior path only when person-abroad and exclusive-use-abroad facts are explicitly confirmed.
- **AC-044** Software licence/rights operations under 11(c-d) can qualify through the free-zone path only when recipient free-zone installation and provider non-free national origin are explicitly confirmed.
- **AC-045** Unknown provider origin in a claimed free-zone path returns insufficient evidence, not qualified export treatment.
- **AC-046** An Article 34 family outside Release-1 support returns unsupported/review instead of being mapped to numeral 11.
- **AC-047** Rule-pack resolution for a date before the verified Release-1 support boundary is rejected and cannot silently reuse the current pack.
- **AC-048** Architecture tests prove the Release-1 regulatory policy contains no VAT-rate calculation, `IsForeign` shortcut, CFE selection, EF Core or database-provider dependency.

## Human reconciliation

After approval, FR-091..095, BR-021..024 and AC-041..048 must be folded into the canonical Requirements/Acceptance artifacts rather than remaining permanently parallel.
