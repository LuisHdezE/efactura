using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Results;
using EFactura.Application.Common.Security;
using EFactura.Domain.Common;
using EFactura.Domain.Parties;

namespace EFactura.Application.Parties;

public sealed record PartyFiscalIdentityView(
    Guid Id,
    string TypeCode,
    string Number,
    string IssuingCountry,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool Active);

public sealed record PartyView(
    Guid Id,
    long Version,
    bool Active,
    PartyKind Kind,
    string Name,
    string ResidenceCountry,
    string TaxResidenceCountry,
    IReadOnlyCollection<PartyRole> Roles,
    IReadOnlyCollection<PartyFiscalIdentityView> FiscalIdentities)
{
    public static PartyView FromDomain(Party party) =>
        new(
            party.Id,
            party.Version,
            party.Active,
            party.Kind,
            party.Name,
            party.ResidenceCountry,
            party.TaxResidenceCountry,
            party.Roles.OrderBy(x => x).ToArray(),
            party.FiscalIdentities
                .OrderBy(x => x.TypeCode, StringComparer.Ordinal)
                .ThenBy(x => x.Number, StringComparer.Ordinal)
                .Select(x => new PartyFiscalIdentityView(
                    x.Id,
                    x.TypeCode,
                    x.Number,
                    x.IssuingCountry,
                    x.ValidFrom,
                    x.ValidTo,
                    x.Active))
                .ToArray());
}

public sealed record PartySearchRequest(
    string OrganizationId,
    string? Search,
    PartyRole? Role,
    bool? Active,
    int Page = 1,
    int PageSize = 50);

public interface IPartyMaintenanceRepository
{
    Task<PageResult<Party>> SearchAsync(
        PartySearchRequest request,
        CancellationToken cancellationToken = default);

    Task SaveAsync(Party party, CancellationToken cancellationToken = default);
}

public sealed class ListPartiesUseCase
{
    private readonly IPartyMaintenanceRepository _parties;
    private readonly IActorContextAccessor _actorContext;

    public ListPartiesUseCase(IPartyMaintenanceRepository parties, IActorContextAccessor actorContext)
    {
        _parties = parties;
        _actorContext = actorContext;
    }

    public async Task<PageResult<PartyView>> ExecuteAsync(
        PartySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureReadAuthorized(_actorContext.Current, request.OrganizationId);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var result = await _parties.SearchAsync(request with { Page = page, PageSize = pageSize }, cancellationToken);
        return new PageResult<PartyView>(
            result.Items.Select(PartyView.FromDomain).ToArray(),
            page,
            pageSize,
            result.Total);
    }

    internal static void EnsureReadAuthorized(ActorContext actor, string organizationId)
    {
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.PartiesRead))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "permission_denied",
                "The actor is not allowed to read parties.");
        }

        if (!actor.CompanyScopes.Contains(organizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "organization_scope_denied",
                "The actor is not allowed to read parties in this organization.");
        }
    }
}

public sealed class GetPartyUseCase
{
    private readonly IPartyRepository _parties;
    private readonly IActorContextAccessor _actorContext;

    public GetPartyUseCase(IPartyRepository parties, IActorContextAccessor actorContext)
    {
        _parties = parties;
        _actorContext = actorContext;
    }

    public async Task<PartyView> ExecuteAsync(
        string organizationId,
        Guid partyId,
        CancellationToken cancellationToken = default)
    {
        ListPartiesUseCase.EnsureReadAuthorized(_actorContext.Current, organizationId);
        var party = await _parties.GetAsync(organizationId, partyId, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "party.not_found",
                "The requested party was not found.");

        return PartyView.FromDomain(party);
    }
}

public sealed record PartyMutationResult(Guid PartyId, long Version, bool Replayed);

public sealed record PartyChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid PartyId,
    string OrganizationId,
    string ChangeType) : IIntegrationEvent;

public sealed class PartyMutationWorkflow
{
    private readonly IPartyRepository _parties;
    private readonly IPartyMaintenanceRepository _maintenance;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actorContext;
    private readonly ICorrelationContextAccessor _correlationContext;

    public PartyMutationWorkflow(
        IPartyRepository parties,
        IPartyMaintenanceRepository maintenance,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actorContext,
        ICorrelationContextAccessor correlationContext)
    {
        _parties = parties;
        _maintenance = maintenance;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actorContext = actorContext;
        _correlationContext = correlationContext;
    }

    public Task<PartyMutationResult> ExecuteAsync(
        string organizationId,
        Guid partyId,
        string permission,
        string operationScope,
        string idempotencyKey,
        string requestHash,
        string auditEventName,
        string changeType,
        Func<Party, CancellationToken, Task> mutate,
        Func<Party, IReadOnlyDictionary<string, string?>> metadata,
        CancellationToken cancellationToken = default)
    {
        EnsureMutationAuthorized(permission, organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var actor = _actorContext.Current;
            var correlation = _correlationContext.Current;
            var scope = $"{operationScope}:{organizationId}:{partyId:N}";

            var reservation = await _idempotency.TryReserveAsync(
                new IdempotencyReservation(
                    scope,
                    idempotencyKey,
                    requestHash,
                    actor.ActorId,
                    correlation.CorrelationId,
                    now.AddMinutes(10)),
                ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = await _parties.GetAsync(organizationId, partyId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "idempotency.missing_completed_resource",
                        "The prior completed party no longer exists in the authoritative store.");

                return new PartyMutationResult(replayed.Id, replayed.Version, true);
            }

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

            await _unitOfWork.SaveChangesAsync(ct);

            var party = await _parties.GetAsync(organizationId, partyId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound,
                    "party.not_found",
                    "The requested party was not found.");

            try
            {
                await mutate(party, ct);
            }
            catch (DomainRuleException ex)
            {
                throw MapDomainRule(ex, party.Version);
            }

            await _maintenance.SaveAsync(party, ct);

            await _audit.AppendAsync(
                new AuditEvent(
                    Guid.NewGuid(),
                    now,
                    auditEventName,
                    actor.ActorId,
                    organizationId,
                    null,
                    null,
                    "Party",
                    party.Id.ToString(),
                    AuditOutcome.Succeeded,
                    correlation.CorrelationId,
                    correlation.CausationId,
                    metadata(party)),
                ct);

            await _outbox.EnqueueAsync(
                new PartyChangedIntegrationEvent(
                    Guid.NewGuid(),
                    now,
                    party.Id,
                    organizationId,
                    changeType),
                new OutboxContext(
                    correlation.CorrelationId,
                    correlation.CausationId,
                    organizationId,
                    actor.ActorId),
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    idempotencyKey,
                    requestHash,
                    auditEventName,
                    "Party",
                    party.Id.ToString(),
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new PartyMutationResult(party.Id, party.Version, false);
        }, cancellationToken);
    }

    private void EnsureMutationAuthorized(string permission, string organizationId)
    {
        var actor = _actorContext.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(permission))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "permission_denied",
                "The actor is not allowed to perform this party operation.");
        }

        if (!actor.CompanyScopes.Contains(organizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "organization_scope_denied",
                "The actor is not allowed to manage parties in this organization.");
        }
    }

    private static ApplicationProblemException MapDomainRule(DomainRuleException ex, long currentVersion)
    {
        if (string.Equals(ex.Code, "concurrency.stale_version", StringComparison.Ordinal))
        {
            return new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The party changed before this operation could be applied.",
                conflictType: "stale_version",
                currentVersion: currentVersion.ToString());
        }

        return new ApplicationProblemException(ApplicationProblemKind.Validation, ex.Code, ex.Message);
    }
}

public sealed record UpdatePartyCommand(
    string OrganizationId,
    Guid PartyId,
    PartyKind? Kind,
    string? Name,
    string? ResidenceCountry,
    string? TaxResidenceCountry,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed class UpdatePartyUseCase
{
    private readonly PartyMutationWorkflow _workflow;

    public UpdatePartyUseCase(PartyMutationWorkflow workflow)
    {
        _workflow = workflow;
    }

    public Task<PartyMutationResult> ExecuteAsync(UpdatePartyCommand command, CancellationToken cancellationToken = default) =>
        _workflow.ExecuteAsync(
            command.OrganizationId,
            command.PartyId,
            Permissions.PartiesManage,
            "party.update",
            command.IdempotencyKey,
            command.RequestHash,
            "party.updated",
            "master-data-updated",
            (party, _) =>
            {
                party.UpdateMasterData(
                    command.Kind ?? party.Kind,
                    string.IsNullOrWhiteSpace(command.Name) ? party.Name : command.Name,
                    string.IsNullOrWhiteSpace(command.ResidenceCountry) ? party.ResidenceCountry : command.ResidenceCountry,
                    string.IsNullOrWhiteSpace(command.TaxResidenceCountry) ? party.TaxResidenceCountry : command.TaxResidenceCountry,
                    command.ExpectedVersion);
                return Task.CompletedTask;
            },
            party => new Dictionary<string, string?>
            {
                ["version"] = party.Version.ToString(),
                ["kind"] = party.Kind.ToString(),
                ["residenceCountry"] = party.ResidenceCountry,
                ["taxResidenceCountry"] = party.TaxResidenceCountry
            },
            cancellationToken);
}

public sealed record AddPartyFiscalIdentityCommand(
    string OrganizationId,
    Guid PartyId,
    PartyFiscalIdentityInput Identity,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed class AddPartyFiscalIdentityUseCase
{
    private readonly PartyMutationWorkflow _workflow;
    private readonly IPartyRepository _parties;

    public AddPartyFiscalIdentityUseCase(PartyMutationWorkflow workflow, IPartyRepository parties)
    {
        _workflow = workflow;
        _parties = parties;
    }

    public Task<PartyMutationResult> ExecuteAsync(
        AddPartyFiscalIdentityCommand command,
        CancellationToken cancellationToken = default) =>
        _workflow.ExecuteAsync(
            command.OrganizationId,
            command.PartyId,
            Permissions.PartiesFiscalManage,
            "party.fiscal-identity.add",
            command.IdempotencyKey,
            command.RequestHash,
            "party.fiscal_identity.added",
            "fiscal-identity-added",
            async (party, ct) =>
            {
                if (await _parties.FiscalIdentityExistsAsync(
                        command.OrganizationId,
                        command.Identity.TypeCode,
                        command.Identity.Number,
                        command.Identity.IssuingCountry,
                        command.PartyId,
                        ct))
                {
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "party.fiscal_identity.already_registered",
                        "The fiscal identity is already assigned to another party.",
                        conflictType: "duplicate_fiscal_identity");
                }

                party.AddFiscalIdentity(
                    Guid.NewGuid(),
                    command.Identity.TypeCode,
                    command.Identity.Number,
                    command.Identity.IssuingCountry,
                    command.Identity.ValidFrom,
                    command.Identity.ValidTo,
                    command.ExpectedVersion);
            },
            party => new Dictionary<string, string?>
            {
                ["version"] = party.Version.ToString(),
                ["fiscalIdentityCount"] = party.FiscalIdentities.Count.ToString()
            },
            cancellationToken);
}

public sealed record UpdatePartyFiscalIdentityCommand(
    string OrganizationId,
    Guid PartyId,
    Guid IdentityId,
    PartyFiscalIdentityInput Identity,
    bool Active,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed class UpdatePartyFiscalIdentityUseCase
{
    private readonly PartyMutationWorkflow _workflow;
    private readonly IPartyRepository _parties;

    public UpdatePartyFiscalIdentityUseCase(PartyMutationWorkflow workflow, IPartyRepository parties)
    {
        _workflow = workflow;
        _parties = parties;
    }

    public Task<PartyMutationResult> ExecuteAsync(
        UpdatePartyFiscalIdentityCommand command,
        CancellationToken cancellationToken = default) =>
        _workflow.ExecuteAsync(
            command.OrganizationId,
            command.PartyId,
            Permissions.PartiesFiscalManage,
            "party.fiscal-identity.update",
            command.IdempotencyKey,
            command.RequestHash,
            "party.fiscal_identity.updated",
            "fiscal-identity-updated",
            async (party, ct) =>
            {
                if (await _parties.FiscalIdentityExistsAsync(
                        command.OrganizationId,
                        command.Identity.TypeCode,
                        command.Identity.Number,
                        command.Identity.IssuingCountry,
                        command.PartyId,
                        ct))
                {
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "party.fiscal_identity.already_registered",
                        "The fiscal identity is already assigned to another party.",
                        conflictType: "duplicate_fiscal_identity");
                }

                party.UpdateFiscalIdentity(
                    command.IdentityId,
                    command.Identity.TypeCode,
                    command.Identity.Number,
                    command.Identity.IssuingCountry,
                    command.Identity.ValidFrom,
                    command.Identity.ValidTo,
                    command.Active,
                    command.ExpectedVersion);
            },
            party => new Dictionary<string, string?>
            {
                ["version"] = party.Version.ToString(),
                ["identityId"] = command.IdentityId.ToString(),
                ["active"] = command.Active.ToString()
            },
            cancellationToken);
}

public sealed record SetPartyRolesCommand(
    string OrganizationId,
    Guid PartyId,
    IReadOnlyCollection<PartyRole> Roles,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed class SetPartyRolesUseCase
{
    private readonly PartyMutationWorkflow _workflow;

    public SetPartyRolesUseCase(PartyMutationWorkflow workflow)
    {
        _workflow = workflow;
    }

    public Task<PartyMutationResult> ExecuteAsync(
        SetPartyRolesCommand command,
        CancellationToken cancellationToken = default) =>
        _workflow.ExecuteAsync(
            command.OrganizationId,
            command.PartyId,
            Permissions.PartiesManage,
            "party.roles.set",
            command.IdempotencyKey,
            command.RequestHash,
            "party.roles.changed",
            "roles-changed",
            (party, _) =>
            {
                party.SetRoles(command.Roles, command.ExpectedVersion);
                return Task.CompletedTask;
            },
            party => new Dictionary<string, string?>
            {
                ["version"] = party.Version.ToString(),
                ["roles"] = string.Join(',', party.Roles.OrderBy(x => x))
            },
            cancellationToken);
}
