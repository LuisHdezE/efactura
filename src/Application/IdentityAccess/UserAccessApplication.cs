using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Application.Organizations;
using EFactura.Domain.Common;
using EFactura.Domain.IdentityAccess;

namespace EFactura.Application.IdentityAccess;

public sealed record UserView(
    string Id,
    string IdentityProvider,
    string ExternalSubject,
    string DisplayName,
    string? Email,
    bool Active,
    long Version,
    IReadOnlyList<string> LocationScopes,
    IReadOnlyList<string> TerminalScopes,
    IReadOnlyList<string> RoleIds)
{
    public static UserView FromDomain(SecurityUser user) =>
        new(
            user.Id,
            user.IdentityProvider,
            user.ExternalSubject,
            user.DisplayName,
            user.Email,
            user.Active,
            user.Version,
            user.LocationScopes.ToArray(),
            user.TerminalScopes.ToArray(),
            user.RoleIds.ToArray());
}

public interface ISecurityUserRepository
{
    Task<IReadOnlyCollection<SecurityUser>> ListAsync(string organizationId, CancellationToken cancellationToken = default);
    Task<SecurityUser?> GetAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
    Task<bool> IdentityLinkExistsAsync(
        string organizationId,
        string normalizedIdentityProvider,
        string externalSubject,
        CancellationToken cancellationToken = default);
    Task AddAsync(SecurityUser user, CancellationToken cancellationToken = default);
    Task SaveAsync(SecurityUser user, CancellationToken cancellationToken = default);
}

public sealed record CreateUserCommand(
    string OrganizationId,
    string IdentityProvider,
    string ExternalSubject,
    string DisplayName,
    string? Email,
    IReadOnlyCollection<string> LocationScopes,
    IReadOnlyCollection<string> TerminalScopes,
    string IdempotencyKey,
    string RequestHash);

public sealed record UpdateUserCommand(
    string OrganizationId,
    string UserId,
    string? DisplayName,
    string? Email,
    bool? Active,
    IReadOnlyCollection<string>? LocationScopes,
    IReadOnlyCollection<string>? TerminalScopes,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed record AssignUserRolesCommand(
    string OrganizationId,
    string UserId,
    IReadOnlyCollection<string> RoleIds,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed record UserMutationResult(UserView User, bool Replayed);

public sealed record SecurityUserChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string OrganizationId,
    string UserId,
    long Version,
    string ChangeType) : IIntegrationEvent;

public sealed record SecurityUserRolesChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string OrganizationId,
    string UserId,
    long Version,
    IReadOnlyList<string> RoleIds) : IIntegrationEvent;

internal static class UserAuthorization
{
    public static void EnsureRead(ActorContext actor, string organizationId)
    {
        EnsurePermission(actor, Permissions.SecurityUsersRead, "The actor is not allowed to read users.");
        EnsureOrganization(actor, organizationId);
    }

    public static void EnsureManage(ActorContext actor, string organizationId)
    {
        EnsurePermission(actor, Permissions.SecurityUsersManage, "The actor is not allowed to manage users.");
        EnsureOrganization(actor, organizationId);
    }

    public static void EnsureScopeManage(ActorContext actor, string organizationId)
    {
        EnsureManage(actor, organizationId);
        EnsurePermission(actor, Permissions.SecurityManageRoles, "The actor is not allowed to change user scopes.");
    }

    public static void EnsureRoleManage(ActorContext actor, string organizationId)
    {
        EnsurePermission(actor, Permissions.SecurityManageRoles, "The actor is not allowed to assign roles.");
        EnsureOrganization(actor, organizationId);
    }

    private static void EnsurePermission(ActorContext actor, string permission, string message)
    {
        if (!actor.IsAuthenticated || !actor.HasPermission(permission))
            throw new ApplicationProblemException(ApplicationProblemKind.Forbidden, "permission_denied", message);
    }

    private static void EnsureOrganization(ActorContext actor, string organizationId)
    {
        if (!actor.CompanyScopes.Contains(organizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "organization_scope_denied",
                "The actor is not allowed to access users in this organization.");
        }
    }
}

internal static class UserContract
{
    public static string NormalizeIdentityProvider(string identityProvider)
    {
        if (string.IsNullOrWhiteSpace(identityProvider))
            throw Validation("identity.user.identity_provider_required", "Identity provider is required.");

        return identityProvider.Trim().ToUpperInvariant();
    }

    public static string NormalizeExternalSubject(string externalSubject)
    {
        if (string.IsNullOrWhiteSpace(externalSubject))
            throw Validation("identity.user.external_subject_required", "External subject is required.");

        return externalSubject.Trim();
    }

    public static IReadOnlyList<string> NormalizeIds(IEnumerable<string> values, string code)
    {
        ArgumentNullException.ThrowIfNull(values);
        var normalized = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        if (normalized.Any(value => value.Length > 200))
            throw Validation(code, "A user scope or role identifier exceeds the supported length.");

        return normalized;
    }

    public static async Task ValidateScopesAsync(
        IFiscalLocationRepository locations,
        ActorContext actor,
        string organizationId,
        IReadOnlyCollection<string> locationScopes,
        IReadOnlyCollection<string> terminalScopes,
        CancellationToken cancellationToken)
    {
        foreach (var locationId in locationScopes)
        {
            if (await locations.GetAsync(organizationId, locationId, cancellationToken) is null)
            {
                throw Validation(
                    "identity.user.scope_invalid",
                    "One or more requested location scopes are not valid for this organization.");
            }
        }

        if (actor.TerminalScopes.Count > 0
            && terminalScopes.Any(terminalId => !actor.TerminalScopes.Contains(terminalId)))
        {
            throw Validation(
                "identity.user.scope_invalid",
                "One or more requested terminal scopes are outside the actor's allowed terminal context.");
        }
    }

    public static bool Expands(IReadOnlyCollection<string> current, IReadOnlyCollection<string> desired)
    {
        var currentSet = current.ToHashSet(StringComparer.Ordinal);
        return desired.Any(value => !currentSet.Contains(value));
    }

    public static bool IsSelfTarget(ActorContext actor, SecurityUser user) =>
        !string.IsNullOrWhiteSpace(actor.ActorId)
        && string.Equals(actor.ActorId, user.ExternalSubject, StringComparison.Ordinal);

    public static bool IsSelfTarget(ActorContext actor, string externalSubject) =>
        !string.IsNullOrWhiteSpace(actor.ActorId)
        && string.Equals(actor.ActorId, externalSubject, StringComparison.Ordinal);

    public static ApplicationProblemException SelfEscalation() =>
        new(
            ApplicationProblemKind.Forbidden,
            "identity.user.self_escalation_forbidden",
            "A caller cannot expand its own application access scopes or role assignments.");

    public static ApplicationProblemException Validation(string code, string detail) =>
        new(ApplicationProblemKind.Validation, code, detail);
}

internal static class UserMutationWorkflow
{
    public static Task<IdempotencyReservationResult> ReserveAsync(
        IIdempotencyStore store,
        string scope,
        string key,
        string requestHash,
        string? actorId,
        string correlationId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        store.TryReserveAsync(
            new IdempotencyReservation(scope, key, requestHash, actorId, correlationId, now.AddMinutes(10)),
            cancellationToken);

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
            "The prior completed user no longer exists in the authoritative store.");

    public static ApplicationProblemException DuplicateIdentity() =>
        new(
            ApplicationProblemKind.Conflict,
            "identity.user.identity_duplicate",
            "This external identity is already linked inside the organization.",
            conflictType: "duplicate_identity_link");

    public static ApplicationProblemException NotFound() =>
        new(ApplicationProblemKind.NotFound, "identity.user.not_found", "The requested user was not found.");

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

    public static async Task AppendUserEvidenceAsync(
        SecurityUser user,
        string eventName,
        string changeType,
        DateTimeOffset now,
        ActorContext actor,
        CorrelationContext correlation,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IReadOnlyCollection<string> beforeLocationScopes,
        IReadOnlyCollection<string> beforeTerminalScopes,
        CancellationToken cancellationToken)
    {
        await audit.AppendAsync(
            new AuditEvent(
                Guid.NewGuid(),
                now,
                eventName,
                actor.ActorId,
                user.OrganizationId,
                null,
                null,
                "SecurityUser",
                user.Id,
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                correlation.CausationId,
                new Dictionary<string, string?>
                {
                    ["identityProvider"] = user.IdentityProvider,
                    ["active"] = user.Active.ToString(),
                    ["version"] = user.Version.ToString(),
                    ["beforeLocationScopes"] = string.Join(",", beforeLocationScopes.OrderBy(x => x, StringComparer.Ordinal)),
                    ["afterLocationScopes"] = string.Join(",", user.LocationScopes),
                    ["beforeTerminalScopes"] = string.Join(",", beforeTerminalScopes.OrderBy(x => x, StringComparer.Ordinal)),
                    ["afterTerminalScopes"] = string.Join(",", user.TerminalScopes)
                }),
            cancellationToken);

        await outbox.EnqueueAsync(
            new SecurityUserChangedIntegrationEvent(
                Guid.NewGuid(),
                now,
                user.OrganizationId,
                user.Id,
                user.Version,
                changeType),
            new OutboxContext(correlation.CorrelationId, correlation.CausationId, user.OrganizationId, actor.ActorId),
            cancellationToken);
    }

    public static async Task AppendRoleEvidenceAsync(
        SecurityUser user,
        DateTimeOffset now,
        ActorContext actor,
        CorrelationContext correlation,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IReadOnlyCollection<string> beforeRoleIds,
        CancellationToken cancellationToken)
    {
        await audit.AppendAsync(
            new AuditEvent(
                Guid.NewGuid(),
                now,
                "SECURITY_USER_ROLES_CHANGED",
                actor.ActorId,
                user.OrganizationId,
                null,
                null,
                "SecurityUser",
                user.Id,
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                correlation.CausationId,
                new Dictionary<string, string?>
                {
                    ["version"] = user.Version.ToString(),
                    ["beforeRoleIds"] = string.Join(",", beforeRoleIds.OrderBy(x => x, StringComparer.Ordinal)),
                    ["afterRoleIds"] = string.Join(",", user.RoleIds)
                }),
            cancellationToken);

        await outbox.EnqueueAsync(
            new SecurityUserRolesChangedIntegrationEvent(
                Guid.NewGuid(),
                now,
                user.OrganizationId,
                user.Id,
                user.Version,
                user.RoleIds.ToArray()),
            new OutboxContext(correlation.CorrelationId, correlation.CausationId, user.OrganizationId, actor.ActorId),
            cancellationToken);
    }
}

public sealed class ListUsersUseCase
{
    private readonly ISecurityUserRepository _users;
    private readonly IActorContextAccessor _actors;

    public ListUsersUseCase(ISecurityUserRepository users, IActorContextAccessor actors)
    {
        _users = users;
        _actors = actors;
    }

    public async Task<IReadOnlyList<UserView>> ExecuteAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        UserAuthorization.EnsureRead(_actors.Current, organizationId);
        return (await _users.ListAsync(organizationId, cancellationToken))
            .OrderBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(user => user.Id, StringComparer.Ordinal)
            .Select(UserView.FromDomain)
            .ToArray();
    }
}

public sealed class GetUserUseCase
{
    private readonly ISecurityUserRepository _users;
    private readonly IActorContextAccessor _actors;

    public GetUserUseCase(ISecurityUserRepository users, IActorContextAccessor actors)
    {
        _users = users;
        _actors = actors;
    }

    public async Task<UserView> ExecuteAsync(string organizationId, string userId, CancellationToken cancellationToken = default)
    {
        UserAuthorization.EnsureRead(_actors.Current, organizationId);
        var user = await _users.GetAsync(organizationId, userId, cancellationToken)
            ?? throw UserMutationWorkflow.NotFound();
        return UserView.FromDomain(user);
    }
}

public sealed class CreateUserUseCase
{
    private readonly ISecurityUserRepository _users;
    private readonly IFiscalLocationRepository _locations;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public CreateUserUseCase(
        ISecurityUserRepository users,
        IFiscalLocationRepository locations,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _users = users;
        _locations = locations;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<UserMutationResult> ExecuteAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = _actors.Current;
        UserAuthorization.EnsureManage(actor, command.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        var locationScopes = UserContract.NormalizeIds(command.LocationScopes, "identity.user.scope_invalid");
        var terminalScopes = UserContract.NormalizeIds(command.TerminalScopes, "identity.user.scope_invalid");
        if (locationScopes.Count > 0 || terminalScopes.Count > 0)
            UserAuthorization.EnsureScopeManage(actor, command.OrganizationId);
        if (UserContract.IsSelfTarget(actor, command.ExternalSubject)
            && (locationScopes.Count > 0 || terminalScopes.Count > 0))
            throw UserContract.SelfEscalation();

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var correlation = _correlations.Current;
            var scope = $"identity.user.create:{command.OrganizationId}";
            var reservation = await UserMutationWorkflow.ReserveAsync(
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
                    : await _users.GetAsync(command.OrganizationId, reservation.ResourceId, ct);
                if (replayed is null)
                    throw UserMutationWorkflow.MissingReplay();
                return new UserMutationResult(UserView.FromDomain(replayed), true);
            }

            UserMutationWorkflow.EnsureNewReservation(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            var provider = UserContract.NormalizeIdentityProvider(command.IdentityProvider);
            var subject = UserContract.NormalizeExternalSubject(command.ExternalSubject);
            if (await _users.IdentityLinkExistsAsync(command.OrganizationId, provider, subject, ct))
                throw UserMutationWorkflow.DuplicateIdentity();

            await UserContract.ValidateScopesAsync(_locations, actor, command.OrganizationId, locationScopes, terminalScopes, ct);

            SecurityUser user;
            try
            {
                user = SecurityUser.Create(
                    Guid.NewGuid().ToString("N"),
                    command.OrganizationId,
                    command.IdentityProvider,
                    subject,
                    command.DisplayName,
                    command.Email,
                    locationScopes,
                    terminalScopes);
            }
            catch (DomainRuleException exception)
            {
                throw UserMutationWorkflow.MapDomain(exception);
            }

            await _users.AddAsync(user, ct);
            await UserMutationWorkflow.AppendUserEvidenceAsync(
                user,
                "SECURITY_USER_CREATED",
                "created",
                now,
                actor,
                correlation,
                _audit,
                _outbox,
                Array.Empty<string>(),
                Array.Empty<string>(),
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "created",
                    "SecurityUser",
                    user.Id,
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new UserMutationResult(UserView.FromDomain(user), false);
        }, cancellationToken);
    }
}

public sealed class UpdateUserUseCase
{
    private readonly ISecurityUserRepository _users;
    private readonly IFiscalLocationRepository _locations;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public UpdateUserUseCase(
        ISecurityUserRepository users,
        IFiscalLocationRepository locations,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _users = users;
        _locations = locations;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<UserMutationResult> ExecuteAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = _actors.Current;
        UserAuthorization.EnsureManage(actor, command.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var correlation = _correlations.Current;
            var scope = $"identity.user.update:{command.OrganizationId}:{command.UserId}";
            var reservation = await UserMutationWorkflow.ReserveAsync(
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
                var replayed = await _users.GetAsync(command.OrganizationId, command.UserId, ct)
                    ?? throw UserMutationWorkflow.MissingReplay();
                return new UserMutationResult(UserView.FromDomain(replayed), true);
            }

            UserMutationWorkflow.EnsureNewReservation(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            var user = await _users.GetAsync(command.OrganizationId, command.UserId, ct)
                ?? throw UserMutationWorkflow.NotFound();

            var desiredLocations = command.LocationScopes is null
                ? user.LocationScopes
                : UserContract.NormalizeIds(command.LocationScopes, "identity.user.scope_invalid");
            var desiredTerminals = command.TerminalScopes is null
                ? user.TerminalScopes
                : UserContract.NormalizeIds(command.TerminalScopes, "identity.user.scope_invalid");
            var scopeChanged = !user.LocationScopes.SequenceEqual(desiredLocations, StringComparer.Ordinal)
                || !user.TerminalScopes.SequenceEqual(desiredTerminals, StringComparer.Ordinal);

            if (scopeChanged)
                UserAuthorization.EnsureScopeManage(actor, command.OrganizationId);

            if (scopeChanged
                && UserContract.IsSelfTarget(actor, user)
                && (UserContract.Expands(user.LocationScopes, desiredLocations)
                    || UserContract.Expands(user.TerminalScopes, desiredTerminals)))
            {
                throw UserContract.SelfEscalation();
            }

            if (scopeChanged)
                await UserContract.ValidateScopesAsync(_locations, actor, command.OrganizationId, desiredLocations, desiredTerminals, ct);

            var beforeLocations = user.LocationScopes.ToArray();
            var beforeTerminals = user.TerminalScopes.ToArray();
            try
            {
                user.Patch(
                    command.DisplayName,
                    command.Email,
                    command.Active,
                    command.LocationScopes is null ? null : desiredLocations,
                    command.TerminalScopes is null ? null : desiredTerminals,
                    command.ExpectedVersion);
            }
            catch (DomainRuleException exception)
            {
                throw UserMutationWorkflow.MapDomain(exception, user.Version);
            }

            await _users.SaveAsync(user, ct);
            await UserMutationWorkflow.AppendUserEvidenceAsync(
                user,
                "SECURITY_USER_UPDATED",
                "updated",
                now,
                actor,
                correlation,
                _audit,
                _outbox,
                beforeLocations,
                beforeTerminals,
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "updated",
                    "SecurityUser",
                    user.Id,
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new UserMutationResult(UserView.FromDomain(user), false);
        }, cancellationToken);
    }
}

public sealed class AssignUserRolesUseCase
{
    private readonly ISecurityUserRepository _users;
    private readonly ISecurityRoleRepository _roles;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public AssignUserRolesUseCase(
        ISecurityUserRepository users,
        ISecurityRoleRepository roles,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _users = users;
        _roles = roles;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<UserMutationResult> ExecuteAsync(AssignUserRolesCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = _actors.Current;
        UserAuthorization.EnsureRoleManage(actor, command.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var correlation = _correlations.Current;
            var scope = $"identity.user.roles:{command.OrganizationId}:{command.UserId}";
            var reservation = await UserMutationWorkflow.ReserveAsync(
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
                var replayed = await _users.GetAsync(command.OrganizationId, command.UserId, ct)
                    ?? throw UserMutationWorkflow.MissingReplay();
                return new UserMutationResult(UserView.FromDomain(replayed), true);
            }

            UserMutationWorkflow.EnsureNewReservation(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            var user = await _users.GetAsync(command.OrganizationId, command.UserId, ct)
                ?? throw UserMutationWorkflow.NotFound();
            if (UserContract.IsSelfTarget(actor, user))
                throw UserContract.SelfEscalation();

            var roleIds = UserContract.NormalizeIds(command.RoleIds, "identity.user.role_invalid");
            foreach (var roleId in roleIds)
            {
                var role = await _roles.GetAsync(command.OrganizationId, roleId, ct);
                if (role is null || !role.Active)
                {
                    throw UserContract.Validation(
                        "identity.user.role_invalid",
                        "One or more requested roles are missing, inactive, or outside the organization.");
                }
            }

            var beforeRoleIds = user.RoleIds.ToArray();
            try
            {
                user.ReplaceRoles(roleIds, command.ExpectedVersion);
            }
            catch (DomainRuleException exception)
            {
                throw UserMutationWorkflow.MapDomain(exception, user.Version);
            }

            await _users.SaveAsync(user, ct);
            await UserMutationWorkflow.AppendRoleEvidenceAsync(
                user,
                now,
                actor,
                correlation,
                _audit,
                _outbox,
                beforeRoleIds,
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "updated",
                    "SecurityUser",
                    user.Id,
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new UserMutationResult(UserView.FromDomain(user), false);
        }, cancellationToken);
    }
}
