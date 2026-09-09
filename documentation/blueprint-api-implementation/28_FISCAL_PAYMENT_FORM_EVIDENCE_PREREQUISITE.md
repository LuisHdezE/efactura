# 28 - Fiscal Payment Form Evidence Prerequisite

Status: **CANDIDATE / NOT ACCEPTED UNTIL MERGE**

Date: 2026-09-08

Accepted predecessor baseline: `main@4f1a89ea1fca97c3f5f0299fbb02ac510549368e`
(merge of PR #49, Immutable Fiscal CFE Content Snapshot Foundation).

Blueprint evaluator for this consumer remains:
`0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

## Why this prerequisite was discovered

The accepted sequence after PR #49 was to start **CFE XML BUILD** from the immutable
`FiscalContentSnapshot`.

Before implementing XML tags, the active official DGI CFE format was rechecked. `Formato CFE v25.2`
defines **Forma de Pago** as CFE header content and identifies the supported values:

- `1` - Contado;
- `2` - Credito.

The same header area also defines an optional/conditional **Fecha de vencimiento**. For export CFE,
the format additionally exposes content such as the sale clause/Incoterm, which is not introduced by
this corrective slice.

Official source rechecked on 2026-09-08:

- DGI Formato CFE v25.2:
  `https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=`
- DGI e-Factura home/technical publication confirms v25.2 is enabled in Production from
  2026-06-30:
  `https://www.efactura.dgi.gub.uy/frontend/page`

The pre-PR50 immutable fiscal evidence preserved only `SettlementFingerprint`. The authoritative
settlement decision did already exist at Sale confirmation in `SaleSettlementPlan`, but its actual
kind and credit due date were not frozen into `FiscalConfirmationEvidence`.

Building XML from the fingerprint alone would require reconstructing or guessing a fiscal fact. That
would violate the accepted immutable-snapshot architecture. XML BUILD is therefore paused until this
bounded evidence gap is closed.

## Existing authoritative source

`SaleSettlementPlanner` already resolves the local financial coverage deterministically as one of:

- `NoCharge`;
- `ImmediatePayment`;
- `CreditReceivable`;
- `Mixed`.

For credit and mixed settlement, the accepted plan also owns the receivable due date. This decision is
produced before `FiscalizationRequest` is created and inside the same server-authoritative
`ConfirmSaleUseCase` transaction.

This slice does not recalculate settlement later and does not infer payment form from persisted
payments, receivables or a hash.

## Added immutable evidence

`FiscalConfirmationEvidence` now optionally carries `FiscalSettlementEvidence`:

- `Kind` - the authoritative local settlement kind;
- `PaymentForm` - the DGI header value only when the mapping is unambiguous in the accepted model;
- `DueDate` - the authoritative receivable due date when applicable.

The bounded mappings are:

| Sale settlement | Frozen fiscal payment form | Due date |
| --- | --- | --- |
| `ImmediatePayment` | `Cash = 1` | none |
| `CreditReceivable` | `Credit = 2` | required |
| `Mixed` | none | required |
| `NoCharge` | none | none |

`Mixed` and `NoCharge` intentionally preserve the real settlement fact while leaving
`PaymentForm = null`. The future XML builder must fail closed for such a case until a separately
accepted DGI/business rule establishes a valid mapping. This slice does not invent one.

## Historical compatibility

The previously persisted JSON shape did not contain a settlement property. Compatibility is
preserved deliberately:

- `FiscalConfirmationEvidence.Settlement` is nullable;
- when it is absent, `ComputeFingerprint()` uses exactly the pre-slice fingerprint material;
- historical JSON with no `settlement` property deserializes with `Settlement = null`;
- the original evidence fingerprint remains valid;
- newly confirmed sales include the settlement block in the evidence fingerprint.

No database migration is required because `ConfirmationEvidenceJson` is already the versionable
provider-neutral persistence envelope introduced by PR #49.

Historical evidence remains readable but does not magically acquire payment-form facts that were not
captured at confirmation. A later XML builder must reject a historical snapshot that lacks a required
payment-form prerequisite rather than infer it.

## Transaction boundary

`ConfirmSaleUseCase` now freezes settlement evidence immediately after the authoritative
`SaleSettlementPlan` has been produced and before the `FiscalizationRequest` is persisted.

The existing atomic boundary remains unchanged:

`Sale + Payment/Receivable + Inventory effects + FiscalizationRequest + Audit + Outbox + Idempotency`

The new evidence is simply part of the already-atomic fiscalization payload. No second transaction,
post-confirmation lookup or mutable financial reread is introduced.

## Automated proof

The candidate adds automated checks for:

- immediate payment maps to `Cash = 1`;
- credit settlement maps to `Credit = 2` and requires the authoritative due date;
- mixed settlement preserves its due date without inventing a DGI payment-form value;
- new settlement evidence changes and protects the SHA-256 fiscal evidence fingerprint;
- historical JSON without the new property retains the legacy fingerprint;
- PostgreSQL and MySQL round-trip the new settlement evidence through
  `EfFiscalizationRequestRepository`;
- PostgreSQL and MySQL remain able to read persisted historical JSON with no `settlement` property;
- Domain remains free of EF/AspNet/XML/signing/transport dependencies;
- `ConfirmSaleUseCase` is the only accepted capture point and CFE XML BUILD remains outside this
  prerequisite slice.

Exact-head CI evidence is intentionally not claimed in this document until the PR candidate has run.

## Explicit non-scope

This corrective prerequisite does **not** implement:

- XML element/tag construction;
- XML namespaces;
- XSD loading or validation;
- export sale clause/Incoterm capture;
- mixed/no-charge DGI payment-form policy;
- XML signature;
- certificates/private keys;
- artifact archival;
- DGI/provider transport;
- response interpretation.

## Decision

Do not implement a CFE XML builder that guesses `Forma de Pago` from `SettlementFingerprint` or from
mutable finance state.

After this candidate is accepted and post-merge validated, return to **CFE XML BUILD**. The builder
must consume the frozen `FiscalSettlementEvidence` and fail closed whenever the selected CFE requires
a header fact that is absent or does not have an accepted mapping.
