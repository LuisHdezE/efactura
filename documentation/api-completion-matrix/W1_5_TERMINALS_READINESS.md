# W1.5 Terminals readiness

Status: `READINESS_AUDITED / CONTRACT_PREREQUISITE_REQUIRED / IMPLEMENTATION_NOT_AUTHORIZED`

Readiness baseline: `main@330cb411e1de90244c793adff906dea13325d65f` after formal W1.4 closure through PR #188.

Scope: `API-ORG-007..010` only.

No endpoint outside this set is introduced by W1.5 readiness work.

## Contract inventory

| API ID | operationId | Method / path | Permission | Idempotency | Current implementation |
|---|---|---|---|---|---|
| `API-ORG-007` | `listTerminals` | GET `/api/v1/terminals` | `organization.read` | NO | `MISSING_HTTP` |
| `API-ORG-008` | `registerTerminal` | POST `/api/v1/terminals` | `organization.manage` | REQUIRED | `MISSING_HTTP` |
| `API-ORG-009` | `getTerminal` | GET `/api/v1/terminals/{terminalId}` | `organization.read` | NO | `MISSING_HTTP` |
| `API-ORG-010` | `updateTerminal` | PATCH `/api/v1/terminals/{terminalId}` | `organization.manage` | REQUIRED | `MISSING_HTTP` |

The accepted endpoint inventory defines the public purpose as listing POS/server terminal registrations, registering an operational terminal, reading terminal metadata/status, and updating terminal status/location metadata.

## Readiness conclusion

W1.5 is **not yet implementation-ready**.

The repository has an accepted endpoint-level contract for the four terminal operations, but it does not currently contain an accepted field-level public contract for terminal registration or terminal projection. No governed `TerminalDto`, `TerminalCreateRequest`, `TerminalUpdateRequest`, equivalent OpenAPI schema, or other authoritative payload definition exists on the audited baseline.

Implementation must therefore not guess whether a terminal has fields such as name, code, description, hardware identity, fiscal identity, arbitrary metadata, or a particular status enum. It must also not guess which fields are mutable or what uniqueness rule identifies a terminal.

The next W1.5 step is a bounded terminal-contract decision, followed by a refreshed readiness audit. Only then may the four rows become implementation-authorized.

## Findings that are already locked

The following constraints are supported by existing governed artifacts and do not require invention:

1. A terminal is organization operational master data, not an authentication credential.
2. `organization.read` is the existing permission for company/location/terminal metadata reads.
3. `organization.manage` is the existing permission for company/location/terminal operational metadata mutations.
4. Every terminal request remains inside the resolved company scope. A body-level company override must not bypass `V1OrganizationContextResolver` behavior.
5. Location/terminal scope remains part of the authorization model where applicable.
6. `registerTerminal` and `updateTerminal` require `Idempotency-Key`.
7. Mutable terminal state must use optimistic concurrency where the accepted mutable-resource rules apply; stale writes use the existing `409 concurrency_conflict / stale_version` shape rather than silent last-write-wins.
8. Successful registration/update must emit durable `organization.terminal.registered|updated` evidence and must participate in the same transactionally coupled business/audit/outbox/idempotency boundary used by the current V1 architecture.
9. Terminal registration is distinct from offline/sync device registration. W1.5 must not absorb `registerDevice`, `revokeDevice`, device credentials, offline grants, or sync operation identity.
10. The implementation must remain provider-neutral for PostgreSQL and MySQL and preserve current Clean Architecture boundaries.

## Existing architecture to reuse

### Organization domain/application

The current organization slice already provides the implementation pattern for company-scoped mutable master data:

- `CompanyFiscalProfile`;
- `FiscalLocation`;
- `OrganizationAuthorization.EnsureRead/EnsureManage`;
- application-managed `version`;
- repository interfaces in Application;
- transaction manager + unit of work;
- request-hash idempotency store;
- durable audit writer;
- transactional outbox;
- RFC 9457 conflict/validation mapping.

W1.5 should extend this slice rather than introduce a parallel organization-management subsystem.

### Presentation

`LocationsController` is the closest current HTTP pattern:

- organization resolved from the request context;
- `organization.read/manage` permission enforcement;
- POST/PATCH idempotency key requirement;
- deterministic request hash;
- `Idempotent-Replayed: true` on completed replay;
- `CreatedAtAction` for create;
- canonical resource projection after mutation.

This is a structural precedent only. It does not authorize copying fiscal-location fields into the terminal contract.

### Existing terminal references

`TerminalId` and terminal scopes already appear in actor context, sales, CAE/fiscal and persistence records. These are references to an operational terminal identity, not evidence that an authoritative terminal master currently exists.

The W1.4 user model can persist terminal-scope identifiers. That capability must not be misread as a terminal registry.

## Terminal vs device boundary

The accepted API program distinguishes two resources:

- **Terminal**: organization/POS operational master data under `organization.read/manage`.
- **Device**: offline/sync registration under `sync.device.manage`, with device/client-operation identity and offline capability concerns.

W1.5 must not place device secrets, bearer tokens, offline grants or sync replay identity into the terminal table or public DTO merely because a physical machine may play both roles in some deployments.

Any future association between a registered terminal and a registered sync device must be an explicit contract, not an inferred one.

## Contract prerequisite that must close

Before implementation begins, the repository must accept a bounded field-level contract defining at least:

- the canonical terminal identifier strategy and whether it is server-generated;
- the minimum registration inputs;
- the terminal-to-location relationship and whether reassignment is permitted;
- the exact status model and allowed transitions;
- the safe read projection;
- fields mutable through `updateTerminal`;
- optimistic concurrency input (`expectedVersion`) for updates;
- the duplicate/uniqueness boundary and stable conflict code;
- behavior when the referenced location is missing, cross-organization or inactive;
- list filtering semantics, if any;
- whether deactivation is represented through status and whether physical deletion is forbidden;
- audit metadata that is material without leaking secrets or device credentials.

Until this decision exists, W1.5 remains `CONTRACT_PREREQUISITE_REQUIRED` and all four operations remain `MISSING_HTTP`.

## Persistence readiness

No authoritative terminal-registration table or aggregate was found on the audited baseline. Existing migrations contain terminal references for other aggregates and W1.4 terminal-scope assignments, but those references do not constitute master-data persistence.

Once the contract prerequisite closes, W1.5 is expected to require an additive V1 persistence slice for the terminal master. The exact columns and unique indexes must be derived from the accepted field-level contract, not decided by the migration itself.

No production migration is authorized by this readiness document.

## Test readiness

No dedicated `registerTerminal` or `listTerminals` tests exist on the audited baseline.

The implementation increment must add, at minimum, coverage for:

- exact four-operation OpenAPI surface and operation IDs;
- 401 without JWT;
- 403 without `organization.read/manage` as applicable;
- organization-scope isolation;
- deterministic list/detail projection;
- create/update idempotent replay;
- same-key changed-payload conflict;
- stale-version conflict for update;
- contract-defined uniqueness conflict;
- contract-defined location validation and cross-organization denial;
- audit/outbox/idempotency atomicity;
- PostgreSQL and MySQL persistence behavior;
- regressions for current company/location, W1.4 users/roles, reference data and parties.

## Implementation gate

Implementation is prohibited until the terminal field-level contract is accepted and this readiness document is updated from `CONTRACT_PREREQUISITE_REQUIRED` to an implementation-ready state.

When that prerequisite closes, implementation should remain a bounded W1.5 slice covering only `API-ORG-007..010`, with no speculative device/sync endpoints and no unrelated organization redesign.
