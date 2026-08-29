# Tax Treatment Requirements Amendment

Status: `DRAFT_COMPANION` to `12_REQUIREMENTS_DOMAIN_BASELINE.md` and `13_ACCEPTANCE_AND_TRACEABILITY_DRAFT.md`.

Baseline: 2026-08-29.

This amendment captures the requirements made explicit by the Tax Treatment Decision Engine slice. It does not mark the requirements gate PASS. After human acceptance it should be reconciled into the main requirements/acceptance baseline rather than maintained as a permanent parallel source.

## Additional functional requirements

- **FR-087 — Tax classification separation.** System shall resolve tax-treatment classification independently from the applicable tax rate/profile and independently from final CFE-family selection.
- **FR-088 — Fail closed for regulated uncertainty.** When legally relevant transaction facts, evidence or an implemented effective regulatory rule are insufficient, unsupported or ambiguous, the tax decision shall return `REQUIRES_REVIEW` and shall block irreversible tax/fiscal confirmation until resolved by an authorized path.
- **FR-089 — Mixed-operation granularity.** Mixed goods/services sales shall resolve tax treatment at line or explicitly defined sub-operation granularity. A mixed aggregate shall not be assigned one export/domestic classification merely from customer or header data.
- **FR-090 — Client fiscal hints are non-authoritative.** Client-supplied country, `export`, tax-treatment, rate or CFE-family hints shall be treated only as request facts where the API contract explicitly allows them; they shall never override server-side identity, transaction, rule-version or evidence validation.

## Additional business rules

- **BR-016 — RUC does not decide tax treatment.** Possession of a Uruguayan RUC can affect receiver identity/CFE-family eligibility, but it does not by itself classify an operation as domestic, export, exempt or taxable.
- **BR-017 — `DOMESTIC` is not a VAT percentage.** A `DOMESTIC` tax classification establishes the territorial/treatment branch only. The effective TaxProfile/rule layer determines the applicable rate, exemption or other domestic treatment. No decision-engine branch may equate `DOMESTIC` with a hard-coded 22%, 10% or zero rate.
- **BR-018 — Rule evidence must be effective.** A regulated tax decision may cite only rule evidence whose effective range covers the operation date. Historical operations must remain explainable using the rule/source/version effective at that time.
- **BR-019 — Export service qualification is delegated, explicit and sourced.** `EXPORT_SERVICES` may be emitted only from an Article-34 eligibility evaluation that returns `Qualified` plus one-or-more effective regulatory rule-evidence records.
- **BR-020 — Outside territorial scope is distinct from export of services.** A service established as performed entirely outside Uruguay may be classified outside Uruguayan VAT territorial scope without being modeled as an Article-34 export-of-services case.

## Additional acceptance criteria

| ID | Requirement(s) | Acceptance criterion |
|---|---|---|
| `AC-033` | FR-087, BR-016 | The same local goods transaction with a foreign-resident receiver returns `DOMESTIC` both when that receiver has and does not have a Uruguayan RUC; later CFE eligibility may differ but tax classification does not change from the RUC fact alone. |
| `AC-034` | FR-088/090, BR-014/016 | Foreign receiver plus foreign destination without an authoritative goods-export fact returns `REQUIRES_REVIEW`; no client country/flag can directly produce `EXPORT_GOODS`. |
| `AC-035` | FR-087, BR-020 | A service whose performance is authoritatively established as entirely outside Uruguay returns `OUTSIDE_VAT_SCOPE` and does not require an Article-34 export-service classification merely to reach that territorial result. |
| `AC-036` | FR-047/088, BR-019 | For a service performed in Uruguay: Article-34 `Qualified` returns `EXPORT_SERVICES`; `NotQualified` returns the non-export domestic branch; `InsufficientEvidence` or unsupported scenario returns `REQUIRES_REVIEW`. |
| `AC-037` | FR-089 | A mixed goods/services aggregate cannot receive one header-level tax classification; the engine returns review/missing line-level classification until each relevant sub-operation is resolved. |
| `AC-038` | BR-018, NFR-011/016 | Supplying regulatory evidence outside its effective range rejects the decision deterministically with a stable rule-not-effective error rather than silently using the current rule. |
| `AC-039` | BR-017 | The tax-treatment decision source contains no VAT percentage calculation and returns treatment classification/provenance only; rate resolution stays in the TaxProfile/rule layer. |
| `AC-040` | FR-090 | No public controller/API added by this slice allows a caller to force a tax classification, Article-34 result or CFE code. |

## Traceability

| Requirement / rule | Source/evidence |
|---|---|
| FR-087, BR-016/017/020 | Architectural decomposition derived from current DGI/IMPO territoriality and current CFE receiver-identity rules; implementation evidence `TaxTreatmentDecisionEngine`. |
| FR-088, BR-018/019 | Regulatory provenance/effective-version constraints already required by NFR-011/NFR-016 and strengthened for tax decision execution. |
| FR-089 | Product requirement that goods/services may coexist in one sale plus the fact that goods-export and service-export rules are legally distinct. |
| FR-090 | Existing server-authoritative fiscal-selection/idempotency/security principles; prevents frontend bypass of regulated decisions. |
| AC-033..040 | Executable regression/architecture tests introduced by this implementation slice. |

## Still OPEN

This amendment deliberately does not close:

- the complete Release-1 Article 34 numeral catalog;
- exact evidence requirements for each supported Article 34 case;
- production storage/administration of rule packs;
- the final CFE selector and its current export-of-services documentation strategy;
- e-Ticket receiver-identification thresholds and conditional field rules;
- exact domestic tax-rate/exemption catalog;
- export-goods customs/logistics evidence integration.

Those remain separate regulated slices. Sales/POS irreversible confirmation must not bypass them.
