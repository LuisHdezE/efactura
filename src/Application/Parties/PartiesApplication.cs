using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Domain.Common;
using EFactura.Domain.Parties;

namespace EFactura.Application.Parties;

public sealed record PartyFiscalIdentityInput(
    string TypeCode,
    string Number,
    string IssuingCountry,
    DateOnly? ValidFrom = null,
    DateOnly? ValidTo = null);

public sealed record CreatePartyCommand(
    string OrganizationId,
    PartyKind Kind,
    string Name,
    string ResidenceCountry,
    string TaxResidenceCountry,
    IReadOnlyCollection<PartyRole> Roles,
    IReadOnlyCollection<PartyFiscalIdentityInput> FiscalIdentities,
    string IdempotencyKey,
    string RequestHash);

public sealed record PartyCreatedResult(Guid PartyId, long Version, bool Replayed);

public interface IPartyRepository
{
    Task AddAsync(Party party, CancellationToken cancellationToken = default);
    Task<Party?> GetAsync(string organizationId, Guid partyId, CancellationToken cancellationToken = default);
    Task<bool> FiscalIdentityExistsAsync(
        string organizationId,
        string typeCode,
        string number,
        string issuingCountry,
        Guid? excludingPartyId = null,
        CancellationToken cancellationToken = default);
}

public sealed record PartyCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid PartyId,
    string OrganizationId) : IIntegrationEvent;

public sealed class CreatePartyUseCase
{
    private readonly IPartyRepository _parties;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actorContext;
    private readonly ICorrelationContextAccessor _correlationContext;

    public CreatePartyUseCase(
        IPartyRepository parties,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actorContext,
        ICorrelationContextAccessor correlationContext)
    {
        _parties = parties;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actorContext = actorContext;
        _correlationContext = correlationContext;
    }

    public Task<PartyCreatedResult> ExecuteAsync(
        CreatePartyCommand command,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthorized(command.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var actor = _actorContext.Current;
            var correlation = _correlationContext.Current;
            var scope = $"party.create:{command.OrganizationId}";

            var reservation = await _idempotency.TryReserveAsync(
                new IdempotencyReservation(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    actor.ActorId,
                    correlation.CorrelationId,
                    now.AddMinutes(10)),
                ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                if (!Guid.TryParse(reservation.ResourceId, out var replayId))
                {
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "idempotency.invalid_completed_resource",
                        "The prior completed operation cannot be reconstructed safely.");
                }

                var replayed = await _parties.GetAsync(command.OrganizationId, replayId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "idempotency.missing_completed_resource",
                        "The prior completed party no longer exists in the authoritative store.");

                return new PartyCreatedResult(replayed.Id, replayed.Version, true);
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

            // Persist the reservation while keeping it inside the same local transaction.
            // A later failure must roll it back with the business state.
            await _unitOfWork.SaveChangesAsync(ct);

            foreach (var input in command.FiscalIdentities)
            {
                if (await _parties.FiscalIdentityExistsAsync(
                        command.OrganizationId,
                        input.TypeCode,
                        input.Number,
                        input.IssuingCountry,
                        null,
                        ct))
                {
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "party.fiscal_identity.already_registered",
                        "The fiscal identity is already assigned to another party.",
                        conflictType: "duplicate_fiscal_identity");
                }
            }

            Party party;
            try
            {
                var identities = command.FiscalIdentities
                    .Select(input => PartyFiscalIdentity.Create(
                        Guid.NewGuid(),
                        input.TypeCode,
                        input.Number,
                        input.IssuingCountry,
                        input.ValidFrom,
                        input.ValidTo))
                    .ToArray();

                party = Party.Create(
                    Guid.NewGuid(),
                    command.OrganizationId,
                    command.Kind,
                    command.Name,
                    command.ResidenceCountry,
                    command.TaxResidenceCountry,
                    command.Roles,
                    identities);
            }
            catch (DomainRuleException ex)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Validation,
                    ex.Code,
                    ex.Message);
            }

            await _parties.AddAsync(party, ct);

            await _audit.AppendAsync(
                new AuditEvent(
                    Guid.NewGuid(),
                    now,
                    "party.created",
                    actor.ActorId,
                    command.OrganizationId,
                    null,
                    null,
                    "Party",
                    party.Id.ToString(),
                    AuditOutcome.Succeeded,
                    correlation.CorrelationId,
                    correlation.CausationId,
                    new Dictionary<string, string?>
                    {
                        ["kind"] = party.Kind.ToString(),
                        ["roles"] = string.Join(',', party.Roles.OrderBy(x => x)),
                        ["fiscalIdentityCount"] = party.FiscalIdentities.Count.ToString()
                    }),
                ct);

            await _outbox.EnqueueAsync(
                new PartyCreatedIntegrationEvent(Guid.NewGuid(), now, party.Id, command.OrganizationId),
                new OutboxContext(
                    correlation.CorrelationId,
                    correlation.CausationId,
                    command.OrganizationId,
                    actor.ActorId),
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "created",
                    "Party",
                    party.Id.ToString(),
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new PartyCreatedResult(party.Id, party.Version, false);
        }, cancellationToken);
    }

    private void EnsureAuthorized(string organizationId)
    {
        var actor = _actorContext.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.PartiesManage))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "permission_denied",
                "The actor is not allowed to manage parties.");
        }

        if (!actor.CompanyScopes.Contains(organizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "organization_scope_denied",
                "The actor is not allowed to manage parties in this organization.");
        }
    }
}
