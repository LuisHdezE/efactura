using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public sealed record FiscalNumberReservedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ReservationId,
    Guid CaeAuthorizationId,
    Guid? AllocationId,
    string OrganizationId,
    int CfeType,
    string Series,
    long Number) : IIntegrationEvent;

public sealed record CaeAvailabilityAlertEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid CaeAuthorizationId,
    string OrganizationId,
    string AlertCode) : IIntegrationEvent;

public sealed class FiscalNumberAllocator : IFiscalNumberAllocator
{
    private readonly ICaeRepository _repository;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public FiscalNumberAllocator(
        ICaeRepository repository,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _repository = repository;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public async Task<FiscalNumberReservationResult> ReserveAsync(
        FiscalNumberReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationId);

        var all = await _repository.SearchAuthorizationsAsync(
            new CaeAuthorizationSearchRequest(request.OrganizationId, request.CfeType, null, 1, 200),
            cancellationToken);
        var candidates = all.Items
            .Where(x => x.Status == CaeAuthorizationStatus.Active)
            .Where(x => request.FiscalDate >= x.ValidFrom && request.FiscalDate <= x.ValidTo)
            .OrderBy(x => x.ValidTo)
            .ThenBy(x => x.Series, StringComparer.Ordinal)
            .ThenBy(x => x.RangeFrom)
            .ToArray();

        if (candidates.Length == 0)
        {
            var sameType = all.Items.ToArray();
            if (sameType.Any(x => x.Status == CaeAuthorizationStatus.Active && request.FiscalDate > x.ValidTo))
                throw new ApplicationProblemException(
                    ApplicationProblemKind.BusinessRule, "cae.expired",
                    "No non-expired active CAE is available for this fiscal date.");
            if (sameType.Any(x => x.Status == CaeAuthorizationStatus.Exhausted))
                throw new ApplicationProblemException(
                    ApplicationProblemKind.BusinessRule, "cae.exhausted",
                    "No non-exhausted active CAE is available for this CFE type.");
            throw new ApplicationProblemException(
                ApplicationProblemKind.BusinessRule, "cae.active_authorization_not_found",
                "No active CAE is available for this CFE type and fiscal date.");
        }

        ApplicationProblemException? lastUnavailable = null;
        foreach (var authorization in candidates)
        {
            var allocations = (await _repository.GetAllocationsAsync(
                request.OrganizationId, authorization.Id, cancellationToken)).ToArray();
            var allocation = ResolveOperationalAllocation(allocations, request.LocationId, request.TerminalId);

            try
            {
                FiscalNumberReservation reservation;
                bool allocationExhausted;
                bool authorizationExhausted;

                if (allocation is not null)
                {
                    var priorVersion = allocation.Version;
                    reservation = allocation.Reserve(
                        authorization, request.OperationId, DateTimeOffset.UtcNow, priorVersion);
                    await _repository.SaveAllocationAsync(allocation, cancellationToken);
                    allocationExhausted = allocation.Status == CaeAllocationStatus.Exhausted;

                    authorizationExhausted = !HasRemainingNumber(authorization, allocations, allocation);
                    if (authorizationExhausted)
                    {
                        authorization.MarkExhausted(authorization.Version);
                        await _repository.SaveAuthorizationAsync(authorization, cancellationToken);
                    }
                }
                else
                {
                    var priorVersion = authorization.Version;
                    reservation = authorization.ReserveDirect(
                        request.OperationId, request.LocationId, request.TerminalId,
                        allocations, priorVersion, request.FiscalDate, DateTimeOffset.UtcNow);
                    await _repository.SaveAuthorizationAsync(authorization, cancellationToken);
                    allocationExhausted = false;
                    authorizationExhausted = authorization.Status == CaeAuthorizationStatus.Exhausted;
                }

                await _repository.AddReservationAsync(reservation, cancellationToken);
                await AppendReservationEvidenceAsync(
                    authorization, reservation, authorizationExhausted, allocationExhausted, cancellationToken);

                return new FiscalNumberReservationResult(
                    reservation.Id,
                    reservation.CaeAuthorizationId,
                    reservation.AllocationId,
                    reservation.CfeType,
                    reservation.Series,
                    reservation.Number,
                    reservation.ReservedAtUtc,
                    authorizationExhausted,
                    allocationExhausted);
            }
            catch (DomainRuleException ex) when (ex.Code is "cae.exhausted" or "cae.allocation_exhausted")
            {
                lastUnavailable = new ApplicationProblemException(
                    ApplicationProblemKind.BusinessRule,
                    ex.Code,
                    ex.Message);
            }
            catch (DomainRuleException ex)
            {
                throw CaeCommandSupport.Map(ex);
            }
        }

        throw lastUnavailable ?? new ApplicationProblemException(
            ApplicationProblemKind.BusinessRule, "cae.exhausted",
            "All matching CAE authorizations are exhausted.");
    }

    private async Task AppendReservationEvidenceAsync(
        CaeAuthorization authorization,
        FiscalNumberReservation reservation,
        bool authorizationExhausted,
        bool allocationExhausted,
        CancellationToken cancellationToken)
    {
        var actor = _actors.Current;
        var correlation = _correlations.Current;
        await _audit.AppendAsync(new AuditEvent(
            Guid.NewGuid(), reservation.ReservedAtUtc, "fiscal.number.reserved", actor.ActorId,
            reservation.OrganizationId, reservation.LocationId, reservation.TerminalId,
            "FiscalNumberReservation", reservation.Id.ToString(), AuditOutcome.Succeeded,
            correlation.CorrelationId, null,
            CaeCommandSupport.Metadata(
                ("caeId", reservation.CaeAuthorizationId), ("allocationId", reservation.AllocationId),
                ("cfeType", (int)reservation.CfeType), ("series", reservation.Series),
                ("number", reservation.Number), ("operationId", reservation.OperationId))),
            cancellationToken);
        await _outbox.EnqueueAsync(
            new FiscalNumberReservedEvent(
                Guid.NewGuid(), reservation.ReservedAtUtc, reservation.Id,
                reservation.CaeAuthorizationId, reservation.AllocationId,
                reservation.OrganizationId, (int)reservation.CfeType,
                reservation.Series, reservation.Number),
            new OutboxContext(
                correlation.CorrelationId, null, reservation.OrganizationId, actor.ActorId),
            cancellationToken);

        if (!authorizationExhausted && !allocationExhausted)
            return;

        var alertCode = authorizationExhausted ? "cae.exhausted" : "cae.allocation_exhausted";
        await _audit.AppendAsync(new AuditEvent(
            Guid.NewGuid(), reservation.ReservedAtUtc, alertCode, actor.ActorId,
            reservation.OrganizationId, reservation.LocationId, reservation.TerminalId,
            "CaeAuthorization", authorization.Id.ToString(), AuditOutcome.Succeeded,
            correlation.CorrelationId, null,
            CaeCommandSupport.Metadata(("lastReservationId", reservation.Id))),
            cancellationToken);
        await _outbox.EnqueueAsync(
            new CaeAvailabilityAlertEvent(
                Guid.NewGuid(), reservation.ReservedAtUtc, authorization.Id,
                reservation.OrganizationId, alertCode),
            new OutboxContext(
                correlation.CorrelationId, null, reservation.OrganizationId, actor.ActorId),
            cancellationToken);
    }

    private static CaeAllocation? ResolveOperationalAllocation(
        IReadOnlyCollection<CaeAllocation> allocations,
        string? locationId,
        string? terminalId)
    {
        if (string.IsNullOrWhiteSpace(locationId))
            return null;

        var location = locationId.Trim();
        var terminal = string.IsNullOrWhiteSpace(terminalId) ? null : terminalId.Trim();
        var available = allocations
            .Where(x => x.Status == CaeAllocationStatus.Active && x.NextNumber <= x.RangeTo)
            .Where(x => string.Equals(x.LocationId, location, StringComparison.Ordinal))
            .OrderBy(x => x.RangeFrom)
            .ToArray();

        if (terminal is not null)
        {
            var exact = available.FirstOrDefault(
                x => string.Equals(x.TerminalId, terminal, StringComparison.Ordinal));
            if (exact is not null)
                return exact;
        }

        return available.FirstOrDefault(x => x.TerminalId is null);
    }

    private static bool HasRemainingNumber(
        CaeAuthorization authorization,
        IReadOnlyCollection<CaeAllocation> allocations,
        CaeAllocation? updatedAllocation)
    {
        var effectiveAllocations = allocations
            .Select(x => updatedAllocation is not null && x.Id == updatedAllocation.Id ? updatedAllocation : x)
            .ToArray();

        if (effectiveAllocations.Any(x =>
                x.Status == CaeAllocationStatus.Active && x.NextNumber <= x.RangeTo))
            return true;

        var candidate = authorization.NextNumber;
        foreach (var allocation in effectiveAllocations.OrderBy(x => x.RangeFrom))
        {
            if (allocation.Range.Contains(candidate))
                candidate = allocation.RangeTo + 1;
            if (candidate < allocation.RangeFrom)
                break;
        }
        return candidate <= authorization.RangeTo;
    }
}
