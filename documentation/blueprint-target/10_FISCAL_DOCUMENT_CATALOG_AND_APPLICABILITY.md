# Fiscal Document Catalog and Applicability Baseline

## Status

Target-design evidence, validated against current DGI official material available on **2026-08-28**. This is not yet a production rule engine. Exact field-level conditions are loaded later from the approved technical specification/version.

Primary official baseline:

- DGI e-Factura portal: https://www.efactura.dgi.gub.uy/
- `Formato_CFE_v25-2` (Production enabled 2026-06-30)
- DGI FAQ / functional definitions
- Resolution DGI 798/2012, updated through August 2025
- Resolution DGI 821/2024 for e-Resguardo/credit communication

## Architectural decision

Do **not** model the current CFE set as a timeless C# enum that embeds all applicability rules.

Use a versioned catalog/rule model containing at least:

- canonical code;
- display/legal name;
- family;
- fiscal specification version;
- effective dates;
- enabled-by-issuer profile;
- corresponding contingency code/type;
- correction/reference rules;
- receiver-identification rules;
- transport timing rules;
- schema/validation profile;
- reporting profile;
- official source/provenance.

A small stable enum may classify broad families internally, but DGI code/applicability belongs to configuration/versioned fiscal metadata.

## Current DGI document families observed

| CFE code | CFE | CFC code | CFC | Baseline applicability |
|---:|---|---:|---|---|
| 101 | e-Ticket | 201 | e-Ticket Contingencia | Consumption-final operations under applicable rules. |
| 102 | Nota de Crédito de e-Ticket | 202 | NC e-Ticket Contingencia | Downward adjustment/cancellation relative to eligible e-Ticket. |
| 103 | Nota de Débito de e-Ticket | 203 | ND e-Ticket Contingencia | Upward adjustment relative to eligible e-Ticket. |
| 111 | e-Factura | 211 | e-Factura Contingencia | Operations between taxpayers/contributors under applicable rules. |
| 112 | Nota de Crédito de e-Factura | 212 | NC e-Factura Contingencia | Downward adjustment/cancellation relative to eligible e-Factura. |
| 113 | Nota de Débito de e-Factura | 213 | ND e-Factura Contingencia | Upward adjustment relative to eligible e-Factura. |
| 121 | e-Factura de Exportación | 221 | e-Factura Exportación Contingencia | Exports of goods; official resolution also establishes optional use for export of services. |
| 122 | NC e-Factura de Exportación | 222 | NC Exportación Contingencia | Downward adjustment/cancellation of export e-Factura. |
| 123 | ND e-Factura de Exportación | 223 | ND Exportación Contingencia | Upward adjustment of export e-Factura. |
| 124 | e-Remito de Exportación | 224 | e-Remito Exportación Contingencia | Valued document for physical movement of goods in export. |
| 131 | e-Ticket Venta por Cuenta Ajena | 231 | corresponding CFC | Account-on-behalf consumption-final family, for operations covered by the applicable regime. |
| 132 | NC e-Ticket Venta por Cuenta Ajena | 232 | corresponding CFC | Downward correction. |
| 133 | ND e-Ticket Venta por Cuenta Ajena | 233 | corresponding CFC | Upward correction. |
| 141 | e-Factura Venta por Cuenta Ajena | 241 | corresponding CFC | Account-on-behalf taxpayer family. |
| 142 | NC e-Factura Venta por Cuenta Ajena | 242 | corresponding CFC | Downward correction. |
| 143 | ND e-Factura Venta por Cuenta Ajena | 243 | corresponding CFC | Upward correction. |
| 151 | e-Boleta de Entrada | 251 | corresponding CFC | Purchaser-issued document for specific purchases where seller is not required to document, plus defined FX-resale cases. |
| 152 | NC e-Boleta de Entrada | 252 | corresponding CFC | Downward adjustment/cancellation. |
| 153 | ND e-Boleta de Entrada | 253 | corresponding CFC | Upward adjustment. |
| 181 | e-Remito | 281 | e-Remito Contingencia | Physical movement of goods. |
| 182 | e-Resguardo | 282 | e-Resguardo Contingencia | Retentions/perceptions and, since 2024, cases where regulation requires communicating tax credits to other taxpayers. |

The catalog above is **not permission to enable every family for every installation**. Issuer activity/regime and product scope determine the enabled subset.

## Receiver/document-selection baseline

### Consumption final

Current DGI CFE format distinguishes consumption-final families such as e-Ticket/e-Ticket Venta por Cuenta Ajena from between-taxpayer e-Factura families. Receiver identification can become mandatory based on the active specification/rule, amount threshold, or retention/perception context.

**Design:** `FiscalDocumentSelector` receives a normalized transaction context and returns an explainable rule result. Controllers/clients cannot choose a CFE code that bypasses applicability rules.

Inputs include:

- issuer profile/regime;
- transaction date/time;
- domestic/export;
- own-account/account-on-behalf;
- customer tax status/identity;
- goods/services/mixed;
- sale/purchase/movement/retention context;
- amount/currency/UI-equivalent values where required;
- correction/reference context;
- contingency state;
- active fiscal specification version.

Output includes:

- eligible/required CFE family;
- required receiver fields;
- rule IDs/source version;
- validation errors/warnings;
- allowed correction families;
- transport/reporting requirements.

## e-Boleta de Entrada baseline

Official DGI material defines e-Boleta de Entrada for purchaser-documented cases including:

1. purchases from a non-electronic seller where the sale is not mandatorily documented by the seller, with possible retentions/perceptions;
2. specific purchase-of-foreign-currency-for-resale cases defined by regulation.

If the seller documents the operation, e-Boleta de Entrada is not used for that same purchase under the general rule.

**Target consequence:** this is a procurement/fiscal intake workflow, not a generic “supplier invoice created by buyer” option.

## e-Resguardo baseline

Current Resolution 798/2012 as modified by Resolution 821/2024 defines e-Resguardo to support:

- tax retentions;
- tax perceptions;
- regulatory communication of tax credits to other taxpayers in applicable cases.

If the applicable regulation requires the retention to appear in the CFE that supports the operation, an additional e-Resguardo is not required for that retention. Likewise where perceptions already appear in the supporting CFE.

**Target consequence:** `ResguardoPolicy` decides whether a standalone e-Resguardo is required. Never generate one mechanically for every supplier/customer withholding.

## e-Remito baseline

DGI defines e-Remito for physical movement of goods. It therefore belongs to an explicit logistics/stock-movement workflow and is not a sales invoice surrogate.

**Target consequence:**

- movement reason/origin/destination/items are first-class;
- e-Remito can be linked to transfer, delivery or other qualifying movement;
- correction/reversal semantics must be taken from current DGI rules before implementing a “cancel remito” command;
- service-only businesses can disable this family.

## Export baseline

DGI defines:

- e-Factura de Exportación for export of goods;
- optional use in export of services under the cited regulation;
- NC/ND export families for corrections;
- e-Remito de Exportación as a valued document for physical export movement of goods.

Export flows require a dedicated applicability/field-validation profile and cannot be implemented by merely setting `country != UY` on a domestic sale.

## Account-on-behalf baseline

DGI functional definitions explicitly identify e-Ticket/e-Factura Venta por Cuenta Ajena and correction families for taxpayers performing operations in the applicable account-on-behalf regime.

**Target consequence:** model represented/principal party and operation ownership explicitly. Do not fake this through a free-text note or alternate customer field.

## Status of initial release enablement

`OPEN`: the owner must later approve which special families are Release-1 enabled after the official applicability matrix is completed.

Core architecture must support them without requiring a rewrite, but specialized endpoints/use cases stay disabled until their regulatory acceptance criteria are complete.
