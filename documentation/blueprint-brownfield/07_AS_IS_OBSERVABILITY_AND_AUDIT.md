# AS-IS Observability and Audit

## Technical logging

`OBSERVED` in `Program.cs`:

- Serilog configured;
- asynchronous rolling file sink under `Logs/webapi-.log`;
- Application Insights trace sink;
- log template references `CorrelationId` and `Username` properties;
- ASP.NET Core minimum-level override.

`UNKNOWN`: no inspected middleware was shown to populate `CorrelationId` and `Username` consistently for every request, so template placeholders are not evidence of end-to-end correlation/actor enrichment.

## Application Insights

Application Insights is registered and an instrumentation key-based configuration is present. Build warnings previously reported obsolete telemetry configuration, so modernization is a delivery concern, not an AS-IS capability gap by itself.

## Durable audit

`NO DURABLE BUSINESS/SECURITY AUDIT MECHANISM OBSERVED`.

Soft-delete fields (`CreatedBy`, `UpdatedBy`, `DeletedBy`, timestamps) are useful metadata but do not provide immutable/event-level traceability.

| Critical event category | Technical logging | Durable audit observed | Assessment |
|---|---|---|---|
| Authentication attempts | possible through framework logs; not specifically proven | no | GAP |
| Role/permission changes | feature not implemented | no | GAP/future |
| Customer/supplier sensitive edits | generic technical logs possible | no before/after audit | GAP |
| Product/stock adjustments | stock workflow not implemented | no | GAP/future |
| Invoice/CFE issuance | fiscal workflow not implemented | no | GAP/future |
| CFE correction/rejection | not implemented | no | GAP/future |
| Payment/allocation | persistence model exists, workflow absent | no | GAP/future |
| Cash operations | persistence model exists, workflow absent | no | GAP/future |
| CAE/certificate changes | not implemented | no | GAP/future |
| Integration/config changes | no durable audit observed | no | GAP |
| Critical exceptions | yes, Serilog fatal path | not business audit | PARTIAL technical only |

## Error contract observation

`ApiGlobalExceptionHandlerAttribute` is the global exception filter. The NotFound handler advertises logical 404 but emits HTTP 400. Generic errors may disclose internal exception data. Model-state handling should also be re-reviewed because control flow returns from the generic branch before the later validation block.

## Target distinction to preserve

Technical observability answers “what happened operationally?” Durable audit answers “who performed what sensitive business/security action, against which entity/value, when and why?” eFactura needs both; current repository clearly demonstrates only the first category.
