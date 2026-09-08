# Sale Confirm API

## Purpose

Expose the already accepted local sale-confirmation transaction through the contracted `API-SAL-007 confirmSale` HTTP route without moving any fiscal-workflow boundary into the controller.

The internal transaction was accepted first in the preceding foundation slice. This transport slice only maps an authenticated and authorized request into `ConfirmSaleUseCase` and returns the durable local-transaction receipt.

## Contract

- operation: `API-SAL-007 confirmSale`
- route: `POST /api/v1/sales/{saleId}/confirm`
- permission: `sales.confirm`
- idempotency: REQUIRED through the shared `Idempotency-Key` request contract
- organization: resolved from the authenticated V1 organization context, never accepted from the request body

## Request surface

The public body accepts only information the operator may legitimately provide at confirmation time:

- expected Sale version;
- zero or more immediate-payment intents containing payment-method id, amount, sale currency and optional external reference;
- optional credit due date;
- required operator reason;
- optional operator context.

The body deliberately does not accept server-owned authoritative evidence such as:

- Sale net, tax or total amount;
- CFE family or format decision;
- validation, confirmation or settlement fingerprints;
- payment-method version/evidence;
- inventory position/version evidence;
- receivable amount or balance;
- CAE, fiscal number or FiscalDocument identity;
- XML, signature or DGI transport data.

Immediate-payment amount is an operator settlement intent, not authority over the Sale total. `SaleSettlementPlanner` compares those intents against the authoritative total recomputed from the accepted confirmation plan and fails closed on uncovered balance, overpayment, disabled/missing payment methods or unsupported cross-currency settlement.

## Server-side execution

The controller:

1. resolves organization from the authenticated V1 context;
2. requires `sales.confirm`;
3. requires the shared idempotency key and computes the canonical request hash;
4. converts payment-method ids to typed identifiers and maps optional credit terms;
5. invokes `ConfirmSaleUseCase` exactly once;
6. returns the committed local transaction receipt and emits `Idempotent-Replayed: true` on completed replay.

`ConfirmSaleUseCase` remains the sole owner of the local transaction. It revalidates authoritative Sale, tax, CFE and inventory evidence server-side and atomically composes Sale confirmation, Payment and/or Receivable, tracked-stock consumption, durable `FiscalizationRequest`, audit, outbox and idempotency completion.

## Response

`SaleConfirmationDto` is a local transaction receipt containing:

- Sale id and committed version;
- confirmation fingerprint;
- settlement fingerprint;
- durable fiscalization-request id;
- payment count;
- optional receivable id;
- confirmation timestamp;
- replay indicator.

It is intentionally not a fiscal-document response.

## Explicit non-scope

This slice does not:

- reserve or allocate CAE;
- create FiscalDocument identity;
- generate CFE XML;
- validate or sign XML;
- access certificate/private-key material;
- send anything to DGI or another fiscal provider;
- expose sale cancellation;
- expose fiscalization-status routes;
- infer CashManagement behavior from payment-method names;
- invent credit limits, supervisor approval, FX, overpayment or customer-advance policy.

Those capabilities remain separate later workflow slices.

## Architecture guards

The transport guards prove that:

- only `API-SAL-001` through `API-SAL-007` are exposed by `SalesController` in this slice;
- `API-SAL-007` requires `Permissions.SalesConfirm` and the shared idempotency contract;
- confirmation request DTOs cannot carry authoritative fiscal, inventory or receivable evidence;
- the controller depends on `ConfirmSaleUseCase`, not persistence, CAE, FiscalDocument, XML, signing or transport components;
- the response stops at `FiscalizationRequestId` and does not masquerade as fiscal completion;
- earlier planning, finance and local-effect components remain within their original boundaries after the HTTP route opens.

## Historical provenance

Implementation documents 17 through 22 intentionally recorded the public confirmation gate as closed while each prerequisite was being proven. They remain historical provenance and are not rewritten. This document records the later transport-layer gate opening after the local transaction foundation was accepted.
