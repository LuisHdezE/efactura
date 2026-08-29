# Tax Treatment Decision Engine — Implementation Evidence

Status: `READY_FOR_REVIEW` subject to exact-head CI and human approval of PR #23.

Baseline: 2026-08-29.

## Purpose

This slice establishes the server-side tax-treatment decision boundary needed before Sales/POS can calculate or fiscalize cross-border operations safely. It implements classification and explainability only. It deliberately does not implement tax rates, a complete Article 34 ruleset or CFE-family selection.

## Implemented source

### Domain

`src/Domain/Taxation/TaxTreatmentDecision.cs`

Contains framework-free concepts:

- `ReceiverFiscalIdentityFact`;
- `ReceiverTaxFacts`;
- `TaxTransactionFacts`;
- `RegulatoryRuleEvidence`;
- `TaxTreatmentRulePack`;
- `ExportServiceEligibilityEvaluation`;
- `TaxTreatmentDecision`;
- `TaxTreatmentDecisionEngine`.

Main classifications:

- `DOMESTIC`;
- `EXPORT_GOODS`;
- `EXPORT_SERVICES`;
- `OUTSIDE_VAT_SCOPE`;
- `REQUIRES_REVIEW`.

### Application

`src/Application/Taxation/TaxTreatmentDecisionApplication.cs`

Application owns the ports:

- `ITaxTreatmentRulePackProvider`;
- `IExportServiceEligibilityEvaluator`.

`ResolveTaxTreatmentUseCase` composes transaction facts, obtains the effective rule pack, obtains Article-34 eligibility when relevant and delegates deterministic classification to Domain.

No Infrastructure or WebApi dependency is introduced.

## Invariants proven by tests

The implementation regression suite verifies that:

1. A foreign-resident receiver making a local goods purchase remains `DOMESTIC`.
2. Adding a Uruguayan RUC to the same foreign-resident receiver does not change the tax classification by itself.
3. Foreign receiver plus foreign destination without authoritative export evidence does not become `EXPORT_GOODS`; it requires review.
4. A trusted goods-export fact resolves `EXPORT_GOODS` and retains source/rule evidence.
5. A service established as performed entirely outside Uruguay resolves `OUTSIDE_VAT_SCOPE` independently from Article 34.
6. A service performed in Uruguay with a qualified Article-34 evaluation resolves `EXPORT_SERVICES` and retains rule/version/source evidence.
7. A definitive Article-34 non-qualification follows the non-export domestic branch.
8. Insufficient Article-34 evidence fails closed to `REQUIRES_REVIEW` and reports missing evidence.
9. Mixed goods/services aggregates require line/sub-operation treatment resolution.
10. A regulatory rule outside its effective range is rejected rather than silently reused.

Architecture guards additionally enforce that:

- the decision engine contains no `IsForeign` switch;
- the decision engine does not reference `TaxProfile` or `RatePercent`;
- CFE-family concepts are not embedded in the treatment engine;
- Application owns rule-provider/evaluator ports without Infrastructure/provider leakage.

## Regulatory grounding

The design is grounded in the current official baseline documented in:

- `documentation/blueprint-target/14_TAX_TREATMENT_DECISION_MATRIX.md`;
- T.O. 2023, Título 10, Art. 5, territoriality;
- Decreto 220/998, Art. 34, export of services;
- current DGI VAT services guidance;
- CFE Format 25.2 receiver identity rules.

Important semantic conclusion: receiver nationality, residence, tax residence, fiscal identity, issuing country and possession of a Uruguayan RUC are distinct facts. None is an authoritative `isForeign => export` switch.

## Relationship with TaxProfile

The accepted TaxProfile foundation remains responsible for effective rate/treatment metadata. The new engine intentionally stops before rate resolution:

```text
receiver + transaction facts + regulatory evidence
        |
        v
TaxTreatmentDecisionEngine
        |
        v
classification + reasons + rule provenance
        |
        +--> later effective TaxProfile/rate resolver
        |
        +--> later CFE selector
```

A `DOMESTIC` classification therefore does not mean 22%, 10%, exempt or any other rate by itself.

## Deliberately not implemented

- production regulatory rule-pack persistence/administration;
- complete Article 34 numeral catalog;
- production Article-34 evaluators;
- documentary-evidence verification;
- VAT amount/rate calculation;
- e-Ticket/e-Factura/e-Factura Exportación selection;
- receiver-identification thresholds for every CFE family;
- customs/export logistics integration;
- Sales/POS confirmation integration;
- any new public API endpoint for forcing a tax decision.

These boundaries are intentional. An unsupported regulated scenario must remain `REQUIRES_REVIEW` until a sourced, versioned rule slice is approved.

## Next implementation dependency

Before Sales/POS irreversible confirmation can consume this decision engine in production, eFactura needs:

1. an approved production `ITaxTreatmentRulePackProvider`;
2. one or more explicitly scoped Article-34 evaluators;
3. approved evidence requirements for each supported evaluator;
4. historical/effective-date fixtures;
5. a separate tax-profile/rate resolution step;
6. a later CFE selector using both receiver identity eligibility and resolved tax treatment.

This implementation slice must be reviewed and merged independently before those follow-on layers are authorized.
