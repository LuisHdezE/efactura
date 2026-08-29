# Authentication and Permission Matrix

## Authentication contract

Default: every `/api/v1` business operation requires a valid JWT Bearer token. Only `getHealth` and `getVersion` are public and return minimal non-secret metadata.

Token issuer, audience, lifetime and signature are validated by ASP.NET Core authentication. Auth0 is **not** presumed active merely because packages/configuration exist in the Brownfield repository.

Token acquisition/refresh/logout remain identity-provider/deployment lifecycle concerns until a separate accepted identity-provider decision authorizes API-owned credential endpoints. The business API therefore does not invent `/login` or `/refresh`.

`getCurrentActor` returns safe application context only: actor identity/display metadata, effective permissions, allowed company/location/terminal scopes, and device/offline capability metadata where applicable. It never returns tokens, signing secrets, provider secrets or unnecessary raw claims.

## Authorization model

Authentication != authorization.

Every protected operation evaluates:

1. required stable application permission;
2. company scope;
3. location/terminal scope when applicable;
4. object/resource scope;
5. domain-specific authorization constraints.

A user knowing an object ID never bypasses these checks. UI hiding is convenience only; API policy is authoritative.

## Permission catalog

| Permission | Main capability |
|---|---|
| `security.users.read` | read users/scope assignments |
| `security.users.manage` | create/update user application access |
| `security.roles.read` | read roles/permission catalog |
| `security.manage_roles` | create/change roles and user role assignments |
| `organization.read` | read company/location/terminal metadata |
| `organization.manage` | change company/location/terminal operational metadata |
| `parties.read` | read/search parties/customers/suppliers |
| `parties.manage` | create/update parties and roles |
| `parties.fiscal.manage` | change typed fiscal identities |
| `catalog.read` | read products/services/categories/tax metadata |
| `catalog.manage` | maintain commercial items/categories |
| `payments.read` | read enabled payment media |
| `payments.manage` | maintain payment media configuration |
| `dashboard.read` | read scoped operational dashboard |
| `sales.read` | read/search sales/POS data |
| `sales.create` | create/update/validate sale drafts |
| `sales.confirm` | confirm a sale and start authoritative effects |
| `sales.cancel` | cancel only when domain state permits |
| `fiscal.read` | read fiscal documents, metadata, CAE/CFC state |
| `fiscal.correct` | request permitted credit/debit corrections |
| `fiscal.regularization.manage` | resolve fiscal rejection/regularization work |
| `fiscal.manage_cae` | import/activate/allocate/close CAE ranges |
| `fiscal.manage_contingency` | enter/exit/manage formal CFC contingency |
| `fiscal.report.read` | read statutory fiscal report state |
| `fiscal.report.manage` | generate/submit statutory fiscal reports |
| `fiscal.configuration.read` | read safe fiscal configuration |
| `fiscal.configuration.manage` | change approved non-secret fiscal configuration |
| `inventory.read` | read positions/movements/transfers/replenishment |
| `inventory.adjust` | post reasoned stock adjustments |
| `inventory.transfer` | create/approve/dispatch/receive/reconcile transfers |
| `procurement.read` | read purchase orders/receipts |
| `procurement.manage` | create/update/cancel purchase orders |
| `procurement.approve` | approve purchase orders |
| `procurement.receive` | create/post goods receipts |
| `receivables.read` | read customer obligations/aging/collections |
| `receivables.adjust` | append receivable adjustments |
| `receivables.collect` | create/reverse collections and allocations |
| `payables.read` | read supplier obligations/aging/payments |
| `payables.adjust` | append payable adjustments |
| `payables.pay` | create/reverse supplier payments/allocations |
| `cash.read` | read assigned cash shifts/movements |
| `cash.open` | open shift |
| `cash.move` | post manual authorized cash movement |
| `cash.close` | submit counts/close shift |
| `cash.reconcile` | resolve/approve variance |
| `sync.use` | use scoped offline/sync API for registered device |
| `sync.device.manage` | register/revoke devices |
| `received_fiscal.read` | read received CFE/evidence/findings |
| `received_fiscal.import` | import received fiscal artifacts |
| `received_fiscal.validate` | run bounded fiscal XML validation |
| `reports.read` | read operational/management reports/calendar |
| `audit.read` | query durable audit evidence |
| `audit.export` | request/access bounded audit exports |
| `alerts.read` | read operational alerts |
| `alerts.manage` | acknowledge/manage allowed alert workflow |
| `operations.read` | read safe integration/outbox/certificate/CAE operational health |
| `integrations.read` | read safe adapter configuration/status |
| `integrations.manage` | change non-secret integration settings/secret references |
| `accounting.export` | list/create/read accounting export jobs |

`AUTHENTICATED` in endpoint inventory means no specialized business permission beyond authenticated/scope-safe reference access. `PUBLIC` means no bearer token.

## Scope rules

### Company

Every business resource belongs to one company/issuer context. Actor must have that company scope. There is no client-controlled `companyId` override that bypasses token/application scope.

### Location

Inventory, POS, cash, CAE allocation and operational reports enforce allowed location scope. Cross-location transfer requires access to the transition permitted by policy, not merely read access to both IDs.

### Terminal/device

Cash shift and POS operations enforce terminal context where required. Offline sync additionally binds actor + registered device + allowed company/location + current offline capability.

### Fiscal privilege separation

System administrator is not automatically fiscal administrator. `organization.manage` does not imply `fiscal.manage_cae`, `fiscal.manage_contingency`, `fiscal.correct` or `fiscal.configuration.manage`.

### Financial privilege separation

Read permissions do not permit allocations/reversals. Collection, supplier-payment, cash close/reconciliation and adjustments have distinct mutation permissions.

## Role examples, not hard-coded authorization

Roles are editable compositions of permissions. Typical profiles may include Cashier, Seller, Inventory Operator, Purchaser, Treasury, Accountant, Fiscal Administrator, System Administrator and Auditor. These names are not authorization shortcuts in controllers.

## Object authorization response

Policy may use 403 for known-but-forbidden resources or a consistent 404-hiding strategy where disclosure itself is sensitive. The chosen behavior must be consistent per resource family and covered by integration tests.

## Security testing obligations

Later implementation/QA must prove at least:

- missing/invalid token -> 401;
- valid token lacking permission -> 403/approved hidden-404 policy;
- same permission but wrong company/location/object -> denied;
- user cannot escalate own roles/scopes without `security.manage_roles` and allowed target scope;
- client cannot force arbitrary CFE type/tax treatment;
- revoked device cannot submit offline operations;
- legacy coexistence does not become a permanent authorization bypass.