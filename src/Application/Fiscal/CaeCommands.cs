using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public sealed record CaeAuthorizationImportedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid CaeAuthorizationId,
    string OrganizationId,
    int CfeType,
    string Series) : IIntegrationEvent;

public sealed record CaeAuthorizationActivatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid CaeAuthorizationId,
    string OrganizationId) : IIntegrationEvent;

public sealed record CaeAllocationChangedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid CaeAuthorizationId,
    Guid AllocationId,
    string OrganizationId,
    string LocationId,
    string Change) : IIntegrationEvent;

internal static class CaeCommandSupport
{
    public static ApplicationProblemException Map(DomainRuleException exception) =>
        new(
            exception.Code == "concurrency.stale_version"
                ? ApplicationProblemKind.Conflict
                : exception.Code is "cae.allocation_overlap" or "cae.allocation_consumed_range"
                    ? ApplicationProblemKind.Conflict
                    : ApplicationProblemKind.Validation,
            exception.Code == "concurrency.stale_version" ? "concurrency_conflict" : exception.Code,
            exception.Message,
            conflictType: exception.Code == "concurrency.stale_version" ? "stale_version" : null);

    public static void ThrowForIdempotency(IdempotencyReservationResult result, string resourceName)
    {
        if (result.Status == IdempotencyReservationStatus.PayloadMismatch)
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict, "idempotency_key_reused",
                $"The idempotency key was already used with a different {resourceName} command.",
                conflictType: "payload_mismatch");
        if (result.Status == IdempotencyReservationStatus.ExistingInProgress)
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict, "idempotency_in_progress",
                $"A {resourceName} command with this idempotency key is still in progress.",
                conflictType: "in_progress", retryAfterSeconds: 2);
    }

    public static IReadOnlyDictionary<string, string?> Metadata(params (string Key, object? Value)[] values) =>
        values.ToDictionary(x => x.Key, x => x.Value?.ToString(), StringComparer.Ordinal);
}

public sealed class ImportCaeAuthorizationUseCase
{
    private readonly ICaeRepository _repository;
    private readonly ICaeArtifactVerifier _verifier;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public ImportCaeAuthorizationUseCase(
        ICaeRepository repository,
        ICaeArtifactVerifier verifier,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _repository = repository;
        _verifier = verifier;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<CaeAuthorizationMutationResult> ExecuteAsync(
        ImportCaeAuthorizationCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = CaeAuthorizationGuard.Ensure(_actors, command.OrganizationId, Permissions.FiscalManageCae);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var correlation = _correlations.Current;
            var scope = $"cae.import:{command.OrganizationId}";
            var reservation = await _idempotency.TryReserveAsync(
                new IdempotencyReservation(
                    scope, command.IdempotencyKey, command.RequestHash,
                    actor.ActorId, correlation.CorrelationId, now.AddMinutes(10)), ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                if (!Guid.TryParse(reservation.ResourceId, out var existingId))
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict, "idempotency.invalid_completed_resource",
                        "The prior CAE import cannot be reconstructed safely.");
                var existing = await _repository.GetAuthorizationAsync(command.OrganizationId, existingId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict, "idempotency.missing_completed_resource",
                        "The prior CAE import no longer exists in the authoritative store.");
                return new CaeAuthorizationMutationResult(existing, true);
            }
            CaeCommandSupport.ThrowForIdempotency(reservation, "CAE import");
            await _unitOfWork.SaveChangesAsync(ct);

            var verification = await _verifier.VerifyAsync(
                new CaeArtifactVerificationRequest(
                    command.CfeType, command.AuthorizationNumber, command.Series,
                    command.RangeFrom, command.RangeTo, command.ValidFrom, command.ValidTo,
                    command.SourceArtifactId, command.SourceArtifactHash,
                    command.SourceName, command.SourceReference), ct);
            if (!verification.Verified)
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Validation, "cae.verification_failed",
                    $"CAE metadata verification failed: {string.Join(", ", verification.Findings)}.");

            var duplicateArtifact = await _repository.FindByArtifactAsync(
                command.OrganizationId, command.SourceArtifactHash, ct);
            if (duplicateArtifact is not null)
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict, "cae.duplicate_artifact",
                    "The CAE source artifact was already imported.", conflictType: "duplicate_resource");

            var overlaps = await _repository.FindOverlappingAuthorizationsAsync(
                command.OrganizationId, command.CfeType, command.Series,
                command.RangeFrom, command.RangeTo, ct);
            if (overlaps.Count > 0)
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict, "cae.range_overlap",
                    "The CAE range overlaps an existing authorization for the same CFE type and series.",
                    conflictType: "range_overlap");

            CaeAuthorization authorization;
            try
            {
                authorization = CaeAuthorization.ImportVerified(
                    command.OrganizationId, command.CfeType, command.AuthorizationNumber,
                    command.Series, command.RangeFrom, command.RangeTo,
                    command.ValidFrom, command.ValidTo, verification.VerificationMethod,
                    command.SourceArtifactId, command.SourceArtifactHash,
                    command.SourceName, command.SourceReference, now);
            }
            catch (DomainRuleException ex)
            {
                throw CaeCommandSupport.Map(ex);
            }

            await _repository.AddAuthorizationAsync(authorization, ct);
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(), now, "cae.imported", actor.ActorId, command.OrganizationId,
                null, null, "CaeAuthorization", authorization.Id.ToString(), AuditOutcome.Succeeded,
                correlation.CorrelationId, null,
                CaeCommandSupport.Metadata(
                    ("cfeType", (int)authorization.CfeType), ("series", authorization.Series),
                    ("rangeFrom", authorization.RangeFrom), ("rangeTo", authorization.RangeTo),
                    ("sourceArtifactHash", authorization.SourceArtifactHash))), ct);
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(), now, "cae.verified", actor.ActorId, command.OrganizationId,
                null, null, "CaeAuthorization", authorization.Id.ToString(), AuditOutcome.Succeeded,
                correlation.CorrelationId, null,
                CaeCommandSupport.Metadata(("verificationMethod", verification.VerificationMethod))), ct);

            var integrationEvent = new CaeAuthorizationImportedEvent(
                Guid.NewGuid(), now, authorization.Id, command.OrganizationId,
                (int)authorization.CfeType, authorization.Series);
            await _outbox.EnqueueAsync(
                integrationEvent,
                new OutboxContext(correlation.CorrelationId, null, command.OrganizationId, actor.ActorId), ct);

            await _idempotency.CompleteAsync(new IdempotencyCompletion(
                scope, command.IdempotencyKey, command.RequestHash, "cae.imported",
                "CaeAuthorization", authorization.Id.ToString(), correlation.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new CaeAuthorizationMutationResult(authorization, false);
        }, cancellationToken);
    }
}

public sealed class ActivateCaeAuthorizationUseCase
{
    private readonly ICaeRepository _repository;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public ActivateCaeAuthorizationUseCase(
        ICaeRepository repository,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _repository = repository;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<CaeAuthorizationMutationResult> ExecuteAsync(
        ActivateCaeAuthorizationCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = CaeAuthorizationGuard.Ensure(_actors, command.OrganizationId, Permissions.FiscalManageCae);
        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var correlation = _correlations.Current;
            var scope = $"cae.activate:{command.OrganizationId}:{command.CaeId}";
            var idem = await _idempotency.TryReserveAsync(new IdempotencyReservation(
                scope, command.IdempotencyKey, command.RequestHash, actor.ActorId,
                correlation.CorrelationId, now.AddMinutes(10)), ct);
            if (idem.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replay = await _repository.GetAuthorizationAsync(command.OrganizationId, command.CaeId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict, "idempotency.missing_completed_resource",
                        "The previously activated CAE no longer exists.");
                return new CaeAuthorizationMutationResult(replay, true);
            }
            CaeCommandSupport.ThrowForIdempotency(idem, "CAE activation");
            await _unitOfWork.SaveChangesAsync(ct);

            var authorization = await _repository.GetAuthorizationAsync(command.OrganizationId, command.CaeId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound, "cae.not_found", "CAE authorization was not found.");
            try
            {
                authorization.Activate(DateOnly.FromDateTime(now.UtcDateTime), command.ExpectedVersion, now);
            }
            catch (DomainRuleException ex)
            {
                throw CaeCommandSupport.Map(ex);
            }
            await _repository.SaveAuthorizationAsync(authorization, ct);
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(), now, "cae.activated", actor.ActorId, command.OrganizationId,
                null, null, "CaeAuthorization", authorization.Id.ToString(), AuditOutcome.Succeeded,
                correlation.CorrelationId, null,
                CaeCommandSupport.Metadata(("version", authorization.Version))), ct);
            await _outbox.EnqueueAsync(
                new CaeAuthorizationActivatedEvent(Guid.NewGuid(), now, authorization.Id, command.OrganizationId),
                new OutboxContext(correlation.CorrelationId, null, command.OrganizationId, actor.ActorId), ct);
            await _idempotency.CompleteAsync(new IdempotencyCompletion(
                scope, command.IdempotencyKey, command.RequestHash, "cae.activated",
                "CaeAuthorization", authorization.Id.ToString(), correlation.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new CaeAuthorizationMutationResult(authorization, false);
        }, cancellationToken);
    }
}

public sealed class CreateCaeAllocationUseCase
{
    private readonly ICaeRepository _repository;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public CreateCaeAllocationUseCase(
        ICaeRepository repository,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _repository = repository;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<CaeAllocationMutationResult> ExecuteAsync(
        CreateCaeAllocationCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = CaeAuthorizationGuard.Ensure(
            _actors, command.OrganizationId, Permissions.FiscalManageCae,
            command.LocationId, command.TerminalId);
        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var correlation = _correlations.Current;
            var scope = $"cae.allocate:{command.OrganizationId}:{command.CaeId}";
            var idem = await _idempotency.TryReserveAsync(new IdempotencyReservation(
                scope, command.IdempotencyKey, command.RequestHash, actor.ActorId,
                correlation.CorrelationId, now.AddMinutes(10)), ct);
            if (idem.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                if (!Guid.TryParse(idem.ResourceId, out var allocationId))
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict, "idempotency.invalid_completed_resource",
                        "The prior CAE allocation cannot be reconstructed safely.");
                var replay = await _repository.GetAllocationAsync(
                    command.OrganizationId, command.CaeId, allocationId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict, "idempotency.missing_completed_resource",
                        "The prior CAE allocation no longer exists.");
                return new CaeAllocationMutationResult(replay, true);
            }
            CaeCommandSupport.ThrowForIdempotency(idem, "CAE allocation");
            await _unitOfWork.SaveChangesAsync(ct);

            var authorization = await _repository.GetAuthorizationAsync(command.OrganizationId, command.CaeId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound, "cae.not_found", "CAE authorization was not found.");
            var existing = await _repository.GetAllocationsAsync(command.OrganizationId, command.CaeId, ct);
            CaeAllocation allocation;
            try
            {
                allocation = authorization.CreateAllocation(
                    command.LocationId, command.TerminalId, command.RangeFrom, command.RangeTo,
                    existing, command.ExpectedVersion, DateOnly.FromDateTime(now.UtcDateTime), now);
            }
            catch (DomainRuleException ex)
            {
                throw CaeCommandSupport.Map(ex);
            }

            await _repository.SaveAuthorizationAsync(authorization, ct);
            await _repository.AddAllocationAsync(allocation, ct);
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(), now, "cae.allocation.created", actor.ActorId, command.OrganizationId,
                allocation.LocationId, allocation.TerminalId, "CaeAllocation", allocation.Id.ToString(),
                AuditOutcome.Succeeded, correlation.CorrelationId, null,
                CaeCommandSupport.Metadata(
                    ("caeId", authorization.Id), ("rangeFrom", allocation.RangeFrom),
                    ("rangeTo", allocation.RangeTo))), ct);
            await _outbox.EnqueueAsync(
                new CaeAllocationChangedEvent(
                    Guid.NewGuid(), now, authorization.Id, allocation.Id,
                    command.OrganizationId, allocation.LocationId, "CREATED"),
                new OutboxContext(correlation.CorrelationId, null, command.OrganizationId, actor.ActorId), ct);
            await _idempotency.CompleteAsync(new IdempotencyCompletion(
                scope, command.IdempotencyKey, command.RequestHash, "cae.allocation.created",
                "CaeAllocation", allocation.Id.ToString(), correlation.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new CaeAllocationMutationResult(allocation, false);
        }, cancellationToken);
    }
}

public sealed class CloseCaeAllocationUseCase
{
    private readonly ICaeRepository _repository;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public CloseCaeAllocationUseCase(
        ICaeRepository repository,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _repository = repository;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<CaeAllocationMutationResult> ExecuteAsync(
        CloseCaeAllocationCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = CaeAuthorizationGuard.Ensure(_actors, command.OrganizationId, Permissions.FiscalManageCae);
        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var correlation = _correlations.Current;
            var scope = $"cae.allocation.close:{command.OrganizationId}:{command.CaeId}:{command.AllocationId}";
            var idem = await _idempotency.TryReserveAsync(new IdempotencyReservation(
                scope, command.IdempotencyKey, command.RequestHash, actor.ActorId,
                correlation.CorrelationId, now.AddMinutes(10)), ct);
            if (idem.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replay = await _repository.GetAllocationAsync(
                    command.OrganizationId, command.CaeId, command.AllocationId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict, "idempotency.missing_completed_resource",
                        "The prior CAE allocation close cannot be reconstructed safely.");
                CaeAuthorizationGuard.Ensure(
                    _actors, command.OrganizationId, Permissions.FiscalManageCae,
                    replay.LocationId, replay.TerminalId);
                return new CaeAllocationMutationResult(replay, true);
            }
            CaeCommandSupport.ThrowForIdempotency(idem, "CAE allocation close");
            await _unitOfWork.SaveChangesAsync(ct);

            _ = await _repository.GetAuthorizationAsync(command.OrganizationId, command.CaeId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound, "cae.not_found", "CAE authorization was not found.");
            var allocation = await _repository.GetAllocationAsync(
                command.OrganizationId, command.CaeId, command.AllocationId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound, "cae.allocation_not_found", "CAE allocation was not found.");
            CaeAuthorizationGuard.Ensure(
                _actors, command.OrganizationId, Permissions.FiscalManageCae,
                allocation.LocationId, allocation.TerminalId);
            try
            {
                allocation.Close(command.ExpectedVersion, now);
            }
            catch (DomainRuleException ex)
            {
                throw CaeCommandSupport.Map(ex);
            }

            await _repository.SaveAllocationAsync(allocation, ct);
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(), now, "cae.allocation.closed", actor.ActorId, command.OrganizationId,
                allocation.LocationId, allocation.TerminalId, "CaeAllocation", allocation.Id.ToString(),
                AuditOutcome.Succeeded, correlation.CorrelationId, null,
                CaeCommandSupport.Metadata(("caeId", command.CaeId), ("version", allocation.Version))), ct);
            await _outbox.EnqueueAsync(
                new CaeAllocationChangedEvent(
                    Guid.NewGuid(), now, command.CaeId, allocation.Id,
                    command.OrganizationId, allocation.LocationId, "CLOSED"),
                new OutboxContext(correlation.CorrelationId, null, command.OrganizationId, actor.ActorId), ct);
            await _idempotency.CompleteAsync(new IdempotencyCompletion(
                scope, command.IdempotencyKey, command.RequestHash, "cae.allocation.closed",
                "CaeAllocation", allocation.Id.ToString(), correlation.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new CaeAllocationMutationResult(allocation, false);
        }, cancellationToken);
    }
}
