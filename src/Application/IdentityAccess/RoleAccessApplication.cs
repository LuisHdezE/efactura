using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Domain.Common;
using EFactura.Domain.IdentityAccess;

namespace EFactura.Application.IdentityAccess;

public sealed record RoleView(
    string Id,
    string Name,
    string? Description,
    bool Active,
    long Version,
    IReadOnlyList<string> Permissions)
{
    public static RoleView FromDomain(SecurityRole role) =>
        new(role.Id, role.Name, role.Description, role.Active, role.Version, role.Permissions.ToArray());
}

public interface ISecurityRoleRepository
{
    Task<IReadOnlyCollection<SecurityRole>> ListAsync(string organizationId, CancellationToken cancellationToken = default);
    Task<SecurityRole?> GetAsync(string organizationId, string roleId, CancellationToken cancellationToken = default);
    Task<bool> NormalizedNameExistsAsync(string organizationId, string normalizedName, string? excludingRoleId = null, CancellationToken cancellationToken = default);
    Task AddAsync(SecurityRole role, CancellationToken cancellationToken = default);
    Task SaveAsync(SecurityRole role, CancellationToken cancellationToken = default);
}

public sealed record CreateRoleCommand(
    string OrganizationId,
    string Name,
    string? Description,
    IReadOnlyCollection<string> Permissions,
    string IdempotencyKey,
    string RequestHash);

public sealed record UpdateRoleCommand(
    string OrganizationId,
    string RoleId,
    string Name,
    string? Description,
    bool Active,
    IReadOnlyCollection<string> Permissions,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed record RoleMutationResult(RoleView Role, bool Replayed);

public sealed record RoleChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string OrganizationId,
    string RoleId,
    long Version,
    string ChangeType) : IIntegrationEvent;

internal static class RoleAuthorization
{
    public static void EnsureRead(ActorContext actor, string organizationId)
    {
        EnsurePermission(actor, Permissions.SecurityRolesRead, "The actor is not allowed to read roles.");
        EnsureScope(actor, organizationId);
    }

    public static void EnsureManage(ActorContext actor, string organizationId)
    {
        EnsurePermission(actor, Permissions.SecurityManageRoles, "The actor is not allowed to manage roles.");
        EnsureScope(actor, organizationId);
    }

    private static void EnsurePermission(ActorContext actor, string permission, string message)
    {
        if (!actor.IsAuthenticated || !actor.HasPermission(permission))
            throw new ApplicationProblemException(ApplicationProblemKind.Forbidden, "permission_denied", message);
    }

    private static void EnsureScope(ActorContext actor, string organizationId)
    {
        if (!actor.CompanyScopes.Contains(organizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "organization_scope_denied",
                "The actor is not allowed to access roles in this organization.");
        }
    }
}

internal static class RoleContract
{
    private static readonly HashSet<string> CanonicalPermissions =
        new(Permissions.All, StringComparer.Ordinal);

    public static IReadOnlyList<string> NormalizePermissions(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        var normalized = permissions
            .Select(permission => permission?.Trim())
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(permission => permission, StringComparer.Ordinal)
            .ToArray();

        var unknown = normalized
            .Where(permission => !CanonicalPermissions.Contains(permission))
            .ToArray();

        if (unknown.Length > 0)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "identity.role.permission_unknown",
                $"Unknown application permission code(s): {string.Join(", ", unknown)}.");
        }

        return normalized;
    }

    public static string NormalizeRoleName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "identity.role.name_required",
                "Role name is required.");
        }

        return name.Trim().ToUpperInvariant();
    }
}

public sealed class ListRolesUseCase
{
    private readonly ISecurityRoleRepository _roles;
    private readonly IActorContextAccessor _actors;

    public ListRolesUseCase(ISecurityRoleRepository roles, IActorContextAccessor actors)
    {
        _roles = roles;
        _actors = actors;
    }

    public async Task<IReadOnlyList<RoleView>> ExecuteAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        RoleAuthorization.EnsureRead(_actors.Current, organizationId);
        return (await _roles.ListAsync(organizationId, cancellationToken))
            .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(role => role.Id, StringComparer.Ordinal)
            .Select(RoleView.FromDomain)
            .ToArray();
    }
}

public sealed class GetRoleUseCase
{
    private readonly ISecurityRoleRepository _roles;
    private readonly IActorContextAccessor _actors;

    public GetRoleUseCase(ISecurityRoleRepository roles, IActorContextAccessor actors)
    {
        _roles = roles;
        _actors = actors;
    }

    public async Task<RoleView> ExecuteAsync(string organizationId, string roleId, CancellationToken cancellationToken = default)
    {
        RoleAuthorization.EnsureRead(_actors.Current, organizationId);
        var role = await _roles.GetAsync(organizationId, roleId, cancellationToken)
            ?? throw NotFound();
        return RoleView.FromDomain(role);
    }

    internal static ApplicationProblemException NotFound() =>
        new(ApplicationProblemKind.NotFound, "identity.role.not_found", "The requested role was not found.");
}

internal static class RoleMutationWorkflow
{
    public static async Task<IdempotencyReservationResult> ReserveAsync(
        IIdempotencyStore store,
        string scope,
        string key,
        string requestHash,
        string? actorId,
        string correlationId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return await store.TryReserveAsync(
            new IdempotencyReservation(scope, key, requestHash, actorId, correlationId, now.AddMinutes(10)),
            cancellationToken);
    }

    public static void EnsureNewReservation(IdempotencyReservationResult reservation)
    {
        if (reservation.Status == IdempotencyReservationStatus.PayloadMismatch)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "idempotency_key_reused",
                "The idempotency key was already used with a different request.",
                conflictType: "payload_mismatch");
        }

        if (reservation.Status == IdempotencyReservationStatus.ExistingInProgress)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "idempotency_in_progress",
                "An operation with this idempotency key is still in progress.",
                conflictType: "in_progress",
                retryAfterSeconds: 2);
        }
    }

    public static ApplicationProblemException MissingReplay() =>
        new(
            ApplicationProblemKind.Conflict,
            "idempotency.missing_completed_resource",
            "The prior completed role no longer exists in the authoritative store.");

    public static ApplicationProblemException DuplicateName() =>
        new(
            ApplicationProblemKind.Conflict,
            "identity.role.name_duplicate",
            "A role with the same name already exists in this organization.",
            conflictType: "duplicate_role_name");

    public static ApplicationProblemException MapDomain(DomainRuleException exception, long? currentVersion = null)
    {
        if (exception.Code == "concurrency.stale_version")
        {
            return new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                exception.Message,
                conflictType: "stale_version",
                currentVersion: currentVersion?.ToString());
        }

        return new ApplicationProblemException(ApplicationProblemKind.Validation, exception.Code, exception.Message);
    }

    public static async Task AppendEvidenceAsync(
        SecurityRole role,
        string eventName,
        string changeType,
        DateTimeOffset now,
        ActorContext actor,
        CorrelationContext correlation,
        IAuditWriter audit,
        IOutboxWriter outbox,
        CancellationToken cancellationToken)
    {
        await audit.AppendAsync(
            new AuditEvent(
                Guid.NewGuid(),
                now,
                eventName,
                actor.ActorId,
                role.OrganizationId,
                null,
                null,
                "SecurityRole",
                role.Id,
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                correlation.CausationId,
                new Dictionary<string, string?>
                {
                    ["name"] = role.Name,
                    ["active"] = role.Active.ToString(),
                    ["version"] = role.Version.ToString(),
                    ["permissions"] = string.Join(",", role.Permissions)
                }),
            cancellationToken);

        await outbox.EnqueueAsync(
            new RoleChangedIntegrationEvent(
                Guid.NewGuid(),
                now,
                role.OrganizationId,
                role.Id,
                role.Version,
                changeType),
            new OutboxContext(correlation.CorrelationId, correlation.CausationId, role.OrganizationId, actor.ActorId),
            cancellationToken);
    }
}

public sealed class CreateRoleUseCase
{
    private readonly ISecurityRoleRepository _roles;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public CreateRoleUseCase(
        ISecurityRoleRepository roles,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _roles = roles;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<RoleMutationResult> ExecuteAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        RoleAuthorization.EnsureManage(_actors.Current, command.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var actor = _actors.Current;
            var correlation = _correlations.Current;
            var scope = $"identity.role.create:{command.OrganizationId}";

            var reservation = await RoleMutationWorkflow.ReserveAsync(
                _idempotency,
                scope,
                command.IdempotencyKey,
                command.RequestHash,
                actor.ActorId,
                correlation.CorrelationId,
                now,
                ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = string.IsNullOrWhiteSpace(reservation.ResourceId)
                    ? null
                    : await _roles.GetAsync(command.OrganizationId, reservation.ResourceId, ct);

                if (replayed is null)
                    throw RoleMutationWorkflow.MissingReplay();

                return new RoleMutationResult(RoleView.FromDomain(replayed), true);
            }

            RoleMutationWorkflow.EnsureNewReservation(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            var normalizedName = RoleContract.NormalizeRoleName(command.Name);
            if (await _roles.NormalizedNameExistsAsync(command.OrganizationId, normalizedName, null, ct))
                throw RoleMutationWorkflow.DuplicateName();

            var permissions = RoleContract.NormalizePermissions(command.Permissions);
            SecurityRole role;
            try
            {
                role = SecurityRole.Create(
                    Guid.NewGuid().ToString("N"),
                    command.OrganizationId,
                    command.Name,
                    command.Description,
                    permissions);
            }
            catch (DomainRuleException exception)
            {
                throw RoleMutationWorkflow.MapDomain(exception);
            }

            await _roles.AddAsync(role, ct);
            await RoleMutationWorkflow.AppendEvidenceAsync(
                role,
                "SECURITY_ROLE_CREATED",
                "created",
                now,
                actor,
                correlation,
                _audit,
                _outbox,
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "created",
                    "SecurityRole",
                    role.Id,
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new RoleMutationResult(RoleView.FromDomain(role), false);
        }, cancellationToken);
    }
}

public sealed class UpdateRoleUseCase
{
    private readonly ISecurityRoleRepository _roles;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public UpdateRoleUseCase(
        ISecurityRoleRepository roles,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _roles = roles;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<RoleMutationResult> ExecuteAsync(UpdateRoleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        RoleAuthorization.EnsureManage(_actors.Current, command.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var actor = _actors.Current;
            var correlation = _correlations.Current;
            var scope = $"identity.role.update:{command.OrganizationId}:{command.RoleId}";

            var reservation = await RoleMutationWorkflow.ReserveAsync(
                _idempotency,
                scope,
                command.IdempotencyKey,
                command.RequestHash,
                actor.ActorId,
                correlation.CorrelationId,
                now,
                ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = await _roles.GetAsync(command.OrganizationId, command.RoleId, ct)
                    ?? throw RoleMutationWorkflow.MissingReplay();
                return new RoleMutationResult(RoleView.FromDomain(replayed), true);
            }

            RoleMutationWorkflow.EnsureNewReservation(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            var role = await _roles.GetAsync(command.OrganizationId, command.RoleId, ct)
                ?? throw GetRoleUseCase.NotFound();

            var normalizedName = RoleContract.NormalizeRoleName(command.Name);
            if (await _roles.NormalizedNameExistsAsync(command.OrganizationId, normalizedName, role.Id, ct))
                throw RoleMutationWorkflow.DuplicateName();

            var permissions = RoleContract.NormalizePermissions(command.Permissions);
            try
            {
                role.Replace(
                    command.Name,
                    command.Description,
                    command.Active,
                    permissions,
                    command.ExpectedVersion);
            }
            catch (DomainRuleException exception)
            {
                throw RoleMutationWorkflow.MapDomain(exception, role.Version);
            }

            await _roles.SaveAsync(role, ct);
            await RoleMutationWorkflow.AppendEvidenceAsync(
                role,
                "SECURITY_ROLE_UPDATED",
                "updated",
                now,
                actor,
                correlation,
                _audit,
                _outbox,
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "updated",
                    "SecurityRole",
                    role.Id,
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new RoleMutationResult(RoleView.FromDomain(role), false);
        }, cancellationToken);
    }
}
