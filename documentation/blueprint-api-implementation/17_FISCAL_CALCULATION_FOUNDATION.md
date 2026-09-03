# API Implementation 17 — Fiscal Calculation Foundation

Status: IMPLEMENTED_ON_BRANCH / PENDING_HUMAN_ACCEPTANCE

Branch: `blueprint/fiscal-calculation-v1`

## Purpose

Establish the first authoritative, versioned CFE arithmetic boundary required before `API-SAL-007 confirmSale` can be implemented safely.

This slice does **not** confirm a sale or issue a fiscal document. It creates a pure Domain calculator that accepts already-resolved fiscal line bases and tax-rate decisions, applies the supported CFE 25.2 arithmetic rules, and returns an immutable calculation result with rule provenance.

## Regulatory baseline reviewed 2026-09-03

Current official DGI evidence used by this slice:

1. `Formato CFE v25.2` is the Release-1 production format supported by this implementation from 2026-06-30.
   - https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=
2. CFE 25.2 field `B-C24 Monto Ítem` defines the line amount as quantity × unit price − line discount + line surcharge and stores the result with two decimal places.
3. Header net amounts are accumulated by fiscal indicator; VAT totals such as `A-C121/A-C122` are calculated from the corresponding accumulated taxable amount and rate rather than by summing independently rounded line VAT values.
4. `A-C124` composes the CFE total from the applicable header net/tax buckets.
5. DGI homologation instructions state that calculation results use mathematical rounding with two decimals.
   - https://www.efactura.dgi.gub.uy/principal/factura-electronica-informacion-general-instructivos

The implementation uses `decimal` and `MidpointRounding.AwayFromZero` for the supported non-negative Release-1 monetary values. For positive midpoint values this implements the mathematical half-up behavior required by the reviewed DGI test instruction. This implementation choice is recorded explicitly rather than relying on the .NET default banker rounding.

## Domain boundary

Added `src/Domain/Fiscal/CfeArithmetic.cs` with:

- `CfeArithmeticRulePack`;
- `CfeArithmeticLineInput`;
- `CfeArithmeticRequest`;
- `CfeArithmeticLineResult`;
- `CfeArithmeticTotals`;
- `CfeArithmeticResult`;
- `CfeArithmeticCalculator`.

The calculator remains framework/provider independent. It does not depend on Sales, EF Core, ASP.NET, Infrastructure, PostgreSQL/MySQL or a fiscal transport provider.

Application adds only the source-controlled `UruguayCfe25_2ArithmeticCatalog` containing the current Release-1 rule-pack identity and DGI provenance.

## Supported Release-1 arithmetic

For each line:

`itemAmount = round(quantity × unitPrice − discountAmount + surchargeAmount, 2)`

The calculator then creates independent header buckets for:

- minimum-rate taxable amount;
- basic-rate taxable amount;
- export amount with `NO_VAT_DUE` semantics.

VAT is calculated at header-bucket level:

- minimum VAT = round(minimum taxable amount × resolved minimum rate / 100, 2);
- basic VAT = round(basic taxable amount × resolved basic rate / 100, 2).

`totalAmount` is calculated from rounded net/header amounts plus the calculated VAT totals.

This deliberately prevents a common rounding error where VAT is rounded independently per line and those rounded values are then summed.

## Required upstream evidence

The calculator does not invent tax treatment or rates. Every line must carry a `TaxRateResolution` that is already:

- `Resolved`;
- supported by the Release-1 arithmetic boundary;
- associated with a non-empty tax rule-pack version;
- associated with effective regulatory evidence for the requested fiscal date.

Current supported tax outcomes are:

- `VatDue + Minimum`;
- `VatDue + Basic`;
- `NoVatDue + Export`.

The existing `VAT_EXEMPT` path remains blocked because Release 1 still requires a specific accepted exemption-rule slice. `OutsideVatTerritorialScope` also remains outside this CFE arithmetic slice because current CFE-family applicability is unresolved there.

## Deliberate model boundary

The current Sales public/domain model does not yet expose line/global discount and surcharge semantics or an explicit gross-price/IVA-included indicator. Therefore this slice does not silently reinterpret `SaleLine.UnitPrice`.

`CfeArithmeticLineInput` carries explicit `DiscountAmount` and `SurchargeAmount` values. The future Sales confirmation orchestration must map commercial facts into these fiscal inputs only after the corresponding commercial pricing semantics are accepted.

Global discounts/recargos and gross-price extraction are not implemented here.

## Verification added

`CrossCuttingTests/CfeArithmeticTests.cs` proves:

- line formula and two-decimal midpoint rounding;
- header-bucket VAT instead of sum-of-line-roundings;
- minimum-rate midpoint rounding;
- separate 10% and 22% buckets;
- export `NO_VAT_DUE` arithmetic;
- unresolved tax rates fail closed;
- exemption is not silently enabled;
- dates before the 25.2 production support boundary fail closed.

`ArchitectureTests/FiscalCalculationArchitectureTests.cs` guards:

- Domain ownership and framework/provider independence;
- source-controlled DGI provenance;
- Sales remains preview-only and `API-SAL-007` remains absent in this slice.

## Explicit non-goals

Not implemented by this slice:

- `confirmSale`;
- Sale state transition to CONFIRMED;
- stock consumption;
- payment/receivable creation;
- CAE number reservation during sale confirmation;
- FiscalDocument persistence;
- global discounts/recargos;
- gross-price/IVA-included extraction;
- exemption catalogs;
- outside-territorial-scope CFE applicability;
- XML generation/XSD validation;
- signing/certificate custody;
- DGI/provider transport;
- fiscal acceptance/rejection/regularization;
- corrections, contingency or daily report generation.

Human acceptance of this foundation is required before it can become an input to the sale-confirmation/fiscal-document snapshot orchestration.
