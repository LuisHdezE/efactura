using EFactura.Domain.Common;

namespace EFactura.Domain.Fiscal;

public enum CaeAuthorizationStatus
{
    Verified = 1,
    Active = 2,
    Exhausted = 3,
    Expired = 4
}

public enum CaeAllocationStatus
{
    Active = 1,
    Closed = 2,
    Exhausted = 3
}

public sealed record CaeNumberRange(long From, long To)
{
    public bool Contains(long number) => number >= From && number <= To;
    public bool Overlaps(CaeNumberRange other) => From <= other.To && other.From <= To;
}

public sealed class FiscalNumberReservation
{
    private FiscalNumberReservation(
        Guid id,
        Guid caeAuthorizationId,
        Guid? allocationId,
        string organizationId,
        CfeFamily cfeType,
        string series,
        long number,
        string? locationId,
        string? terminalId,
        string operationId,
        DateTimeOffset reservedAtUtc)
    {
        Id = id;
        CaeAuthorizationId = caeAuthorizationId;
        AllocationId = allocationId;
        OrganizationId = organizationId;
        CfeType = cfeType;
        Series = series;
        Number = number;
        LocationId = locationId;
        TerminalId = terminalId;
        OperationId = operationId;
        ReservedAtUtc = reservedAtUtc;
    }

    public Guid Id { get; }
    public Guid CaeAuthorizationId { get; }
    public Guid? AllocationId { get; }
    public string OrganizationId { get; }
    public CfeFamily CfeType { get; }
    public string Series { get; }
    public long Number { get; }
    public string? LocationId { get; }
    public string? TerminalId { get; }
    public string OperationId { get; }
    public DateTimeOffset ReservedAtUtc { get; }

    public static FiscalNumberReservation Create(
        Guid caeAuthorizationId,
        Guid? allocationId,
        string organizationId,
        CfeFamily cfeType,
        string series,
        long number,
        string? locationId,
        string? terminalId,
        string operationId,
        DateTimeOffset reservedAtUtc) =>
        new(
            Guid.NewGuid(), caeAuthorizationId, allocationId,
            Required(organizationId, 200, "cae.organization_required"),
            cfeType,
            Required(series, 20, "cae.series_required"),
            number,
            Optional(locationId, 200),
            Optional(terminalId, 200),
            Required(operationId, 200, "cae.operation_id_required"),
            reservedAtUtc);

    public static FiscalNumberReservation Rehydrate(
        Guid id,
        Guid caeAuthorizationId,
        Guid? allocationId,
        string organizationId,
        CfeFamily cfeType,
        string series,
        long number,
        string? locationId,
        string? terminalId,
        string operationId,
        DateTimeOffset reservedAtUtc) =>
        new(id, caeAuthorizationId, allocationId, organizationId, cfeType, series, number,
            locationId, terminalId, operationId, reservedAtUtc);

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainRuleException(code, "Required CAE value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new DomainRuleException(code, $"CAE value cannot exceed {max} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new DomainRuleException("cae.value_too_long", $"CAE value cannot exceed {max} characters.");
        return normalized;
    }
}

public sealed class CaeAllocation
{
    private CaeAllocation(
        Guid id,
        Guid caeAuthorizationId,
        string organizationId,
        string locationId,
        string? terminalId,
        long rangeFrom,
        long rangeTo,
        long nextNumber,
        CaeAllocationStatus status,
        long version,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? closedAtUtc)
    {
        Id = id;
        CaeAuthorizationId = caeAuthorizationId;
        OrganizationId = Required(organizationId, 200, "cae.organization_required");
        LocationId = Required(locationId, 200, "cae.location_required");
        TerminalId = Optional(terminalId, 200);
        if (rangeFrom <= 0 || rangeTo < rangeFrom)
            throw new DomainRuleException("cae.invalid_allocation_range", "CAE allocation range is invalid.");
        RangeFrom = rangeFrom;
        RangeTo = rangeTo;
        NextNumber = nextNumber;
        Status = status;
        Version = version;
        CreatedAtUtc = createdAtUtc;
        ClosedAtUtc = closedAtUtc;
    }

    public Guid Id { get; }
    public Guid CaeAuthorizationId { get; }
    public string OrganizationId { get; }
    public string LocationId { get; }
    public string? TerminalId { get; }
    public long RangeFrom { get; }
    public long RangeTo { get; }
    public long NextNumber { get; private set; }
    public CaeAllocationStatus Status { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public CaeNumberRange Range => new(RangeFrom, RangeTo);

    public static CaeAllocation Create(
        Guid caeAuthorizationId,
        string organizationId,
        string locationId,
        string? terminalId,
        long rangeFrom,
        long rangeTo,
        DateTimeOffset createdAtUtc) =>
        new(Guid.NewGuid(), caeAuthorizationId, organizationId, locationId, terminalId,
            rangeFrom, rangeTo, rangeFrom, CaeAllocationStatus.Active, 1, createdAtUtc, null);

    public static CaeAllocation Rehydrate(
        Guid id,
        Guid caeAuthorizationId,
        string organizationId,
        string locationId,
        string? terminalId,
        long rangeFrom,
        long rangeTo,
        long nextNumber,
        CaeAllocationStatus status,
        long version,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? closedAtUtc) =>
        new(id, caeAuthorizationId, organizationId, locationId, terminalId,
            rangeFrom, rangeTo, nextNumber, status, version, createdAtUtc, closedAtUtc);

    public FiscalNumberReservation Reserve(
        CaeAuthorization authorization,
        string operationId,
        DateTimeOffset now,
        long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new DomainRuleException("concurrency.stale_version", "The CAE allocation changed before reservation.");
        if (Status != CaeAllocationStatus.Active)
            throw new DomainRuleException("cae.allocation_not_active", "The CAE allocation is not active.");
        if (NextNumber > RangeTo)
        {
            Status = CaeAllocationStatus.Exhausted;
            Version++;
            throw new DomainRuleException("cae.allocation_exhausted", "The CAE allocation is exhausted.");
        }

        var number = NextNumber;
        NextNumber++;
        Version++;
        if (NextNumber > RangeTo)
            Status = CaeAllocationStatus.Exhausted;

        return FiscalNumberReservation.Create(
            authorization.Id, Id, authorization.OrganizationId, authorization.CfeType,
            authorization.Series, number, LocationId, TerminalId, operationId, now);
    }

    public void Close(long expectedVersion, DateTimeOffset now)
    {
        if (Version != expectedVersion)
            throw new DomainRuleException("concurrency.stale_version", "The CAE allocation changed before close.");
        if (Status == CaeAllocationStatus.Closed)
            return;
        Status = CaeAllocationStatus.Closed;
        ClosedAtUtc = now;
        Version++;
    }

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainRuleException(code, "Required CAE value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new DomainRuleException(code, $"CAE value cannot exceed {max} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new DomainRuleException("cae.value_too_long", $"CAE value cannot exceed {max} characters.");
        return normalized;
    }
}

public sealed class CaeAuthorization
{
    private CaeAuthorization(
        Guid id,
        string organizationId,
        CfeFamily cfeType,
        string authorizationNumber,
        string series,
        long rangeFrom,
        long rangeTo,
        DateOnly validFrom,
        DateOnly validTo,
        CaeAuthorizationStatus status,
        string verificationMethod,
        string sourceArtifactId,
        string sourceArtifactHash,
        string sourceName,
        string sourceReference,
        long nextNumber,
        long version,
        DateTimeOffset importedAtUtc,
        DateTimeOffset? activatedAtUtc)
    {
        Id = id;
        OrganizationId = Required(organizationId, 200, "cae.organization_required");
        CfeType = cfeType;
        AuthorizationNumber = Required(authorizationNumber, 80, "cae.authorization_number_required");
        Series = Required(series, 20, "cae.series_required").ToUpperInvariant();
        if (rangeFrom <= 0 || rangeTo < rangeFrom)
            throw new DomainRuleException("cae.invalid_range", "CAE range is invalid.");
        if (validTo < validFrom)
            throw new DomainRuleException("cae.invalid_validity", "CAE validity range is invalid.");
        RangeFrom = rangeFrom;
        RangeTo = rangeTo;
        ValidFrom = validFrom;
        ValidTo = validTo;
        Status = status;
        VerificationMethod = Required(verificationMethod, 80, "cae.verification_method_required");
        SourceArtifactId = Required(sourceArtifactId, 200, "cae.source_artifact_id_required");
        SourceArtifactHash = Required(sourceArtifactHash, 128, "cae.source_artifact_hash_required").ToLowerInvariant();
        SourceName = Required(sourceName, 250, "cae.source_name_required");
        SourceReference = Required(sourceReference, 1000, "cae.source_reference_required");
        NextNumber = nextNumber;
        Version = version;
        ImportedAtUtc = importedAtUtc;
        ActivatedAtUtc = activatedAtUtc;
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public CfeFamily CfeType { get; }
    public string AuthorizationNumber { get; }
    public string Series { get; }
    public long RangeFrom { get; }
    public long RangeTo { get; }
    public DateOnly ValidFrom { get; }
    public DateOnly ValidTo { get; }
    public CaeAuthorizationStatus Status { get; private set; }
    public string VerificationMethod { get; }
    public string SourceArtifactId { get; }
    public string SourceArtifactHash { get; }
    public string SourceName { get; }
    public string SourceReference { get; }
    public long NextNumber { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset ImportedAtUtc { get; }
    public DateTimeOffset? ActivatedAtUtc { get; private set; }
    public CaeNumberRange Range => new(RangeFrom, RangeTo);

    public static CaeAuthorization ImportVerified(
        string organizationId,
        CfeFamily cfeType,
        string authorizationNumber,
        string series,
        long rangeFrom,
        long rangeTo,
        DateOnly validFrom,
        DateOnly validTo,
        string verificationMethod,
        string sourceArtifactId,
        string sourceArtifactHash,
        string sourceName,
        string sourceReference,
        DateTimeOffset importedAtUtc) =>
        new(Guid.NewGuid(), organizationId, cfeType, authorizationNumber, series,
            rangeFrom, rangeTo, validFrom, validTo, CaeAuthorizationStatus.Verified,
            verificationMethod, sourceArtifactId, sourceArtifactHash, sourceName, sourceReference,
            rangeFrom, 1, importedAtUtc, null);

    public static CaeAuthorization Rehydrate(
        Guid id,
        string organizationId,
        CfeFamily cfeType,
        string authorizationNumber,
        string series,
        long rangeFrom,
        long rangeTo,
        DateOnly validFrom,
        DateOnly validTo,
        CaeAuthorizationStatus status,
        string verificationMethod,
        string sourceArtifactId,
        string sourceArtifactHash,
        string sourceName,
        string sourceReference,
        long nextNumber,
        long version,
        DateTimeOffset importedAtUtc,
        DateTimeOffset? activatedAtUtc) =>
        new(id, organizationId, cfeType, authorizationNumber, series, rangeFrom, rangeTo,
            validFrom, validTo, status, verificationMethod, sourceArtifactId, sourceArtifactHash,
            sourceName, sourceReference, nextNumber, version, importedAtUtc, activatedAtUtc);

    public CaeAuthorizationStatus EffectiveStatus(DateOnly on) =>
        on > ValidTo ? CaeAuthorizationStatus.Expired : Status;

    public void Activate(DateOnly on, long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (on < ValidFrom || on > ValidTo)
            throw new DomainRuleException("cae.not_currently_valid", "CAE is outside its validity window.");
        if (Status == CaeAuthorizationStatus.Exhausted)
            throw new DomainRuleException("cae.exhausted", "Exhausted CAE cannot be activated.");
        Status = CaeAuthorizationStatus.Active;
        ActivatedAtUtc = now;
        Version++;
    }

    public CaeAllocation CreateAllocation(
        string locationId,
        string? terminalId,
        long rangeFrom,
        long rangeTo,
        IReadOnlyCollection<CaeAllocation> existingAllocations,
        long expectedVersion,
        DateOnly on,
        DateTimeOffset now)
    {
        EnsureUsable(on);
        EnsureVersion(expectedVersion);
        var requested = new CaeNumberRange(rangeFrom, rangeTo);
        if (rangeFrom < RangeFrom || rangeTo > RangeTo || rangeTo < rangeFrom)
            throw new DomainRuleException("cae.allocation_out_of_range", "Allocation must stay inside the CAE range.");
        if (rangeFrom < NextNumber)
            throw new DomainRuleException("cae.allocation_consumed_range", "Allocation cannot include numbers already passed by direct consumption.");
        if (existingAllocations.Any(x => x.Range.Overlaps(requested)))
            throw new DomainRuleException("cae.allocation_overlap", "CAE allocation overlaps an existing or closed allocation.");

        Version++;
        return CaeAllocation.Create(Id, OrganizationId, locationId, terminalId, rangeFrom, rangeTo, now);
    }

    public FiscalNumberReservation ReserveDirect(
        string operationId,
        string? locationId,
        string? terminalId,
        IReadOnlyCollection<CaeAllocation> allocations,
        long expectedVersion,
        DateOnly on,
        DateTimeOffset now)
    {
        EnsureUsable(on);
        EnsureVersion(expectedVersion);

        var candidate = FindNextDirectCandidate(NextNumber, allocations);
        if (candidate > RangeTo)
            throw new DomainRuleException("cae.exhausted", "CAE has no unallocated number available for direct reservation.");

        NextNumber = candidate + 1;
        if (!HasRemainingNumber(allocations))
            Status = CaeAuthorizationStatus.Exhausted;
        Version++;

        return FiscalNumberReservation.Create(
            Id, null, OrganizationId, CfeType, Series, candidate,
            locationId, terminalId, operationId, now);
    }

    public void MarkExhausted(long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        if (Status == CaeAuthorizationStatus.Exhausted)
            return;
        Status = CaeAuthorizationStatus.Exhausted;
        Version++;
    }

    private bool HasRemainingNumber(IReadOnlyCollection<CaeAllocation> allocations)
    {
        if (allocations.Any(x => x.Status == CaeAllocationStatus.Active && x.NextNumber <= x.RangeTo))
            return true;

        return FindNextDirectCandidate(NextNumber, allocations) <= RangeTo;
    }

    private static long FindNextDirectCandidate(
        long start,
        IReadOnlyCollection<CaeAllocation> allocations)
    {
        var candidate = start;
        foreach (var allocation in allocations.OrderBy(x => x.RangeFrom))
        {
            // Once a subrange is allocated it never re-enters the direct pool, even after
            // the allocation is closed or exhausted.
            if (allocation.Range.Contains(candidate))
                candidate = allocation.RangeTo + 1;
            if (candidate < allocation.RangeFrom)
                break;
        }
        return candidate;
    }

    private void EnsureUsable(DateOnly on)
    {
        if (Status != CaeAuthorizationStatus.Active)
            throw new DomainRuleException("cae.not_active", "CAE is not active.");
        if (on < ValidFrom)
            throw new DomainRuleException("cae.not_yet_valid", "CAE validity has not started.");
        if (on > ValidTo)
            throw new DomainRuleException("cae.expired", "Expired CAE cannot reserve or allocate numbers.");
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new DomainRuleException("concurrency.stale_version", "The CAE authorization changed before this operation.");
    }

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainRuleException(code, "Required CAE value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new DomainRuleException(code, $"CAE value cannot exceed {max} characters.");
        return normalized;
    }
}
