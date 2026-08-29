using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfCaeRepository : ICaeRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfCaeRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CaeAuthorization?> GetAuthorizationAsync(
        string organizationId,
        Guid caeId,
        CancellationToken cancellationToken = default)
    {
        var local = _dbContext.CaeAuthorizations.Local
            .SingleOrDefault(x => x.OrganizationId == organizationId && x.Id == caeId);
        if (local is not null)
            return Map(local);

        var record = await _dbContext.CaeAuthorizations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == caeId, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<CaeAuthorization?> FindByArtifactAsync(
        string organizationId,
        string sourceArtifactHash,
        CancellationToken cancellationToken = default)
    {
        var hash = sourceArtifactHash.Trim().ToLowerInvariant();
        var local = _dbContext.CaeAuthorizations.Local
            .SingleOrDefault(x => x.OrganizationId == organizationId && x.SourceArtifactHash == hash);
        if (local is not null)
            return Map(local);

        var record = await _dbContext.CaeAuthorizations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.SourceArtifactHash == hash,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<PageResult<CaeAuthorization>> SearchAuthorizationsAsync(
        CaeAuthorizationSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CaeAuthorizations
            .AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId);
        if (request.CfeType.HasValue)
        {
            var cfeType = (int)request.CfeType.Value;
            query = query.Where(x => x.CfeType == cfeType);
        }
        if (request.Status.HasValue)
        {
            var status = (int)request.Status.Value;
            query = query.Where(x => x.Status == status);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var records = await query
            .OrderBy(x => x.CfeType)
            .ThenBy(x => x.Series)
            .ThenBy(x => x.RangeFrom)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new PageResult<CaeAuthorization>(
            records.Select(Map).ToArray(), request.Page, request.PageSize, total);
    }

    public async Task<IReadOnlyCollection<CaeAuthorization>> FindOverlappingAuthorizationsAsync(
        string organizationId,
        CfeFamily cfeType,
        string series,
        long rangeFrom,
        long rangeTo,
        CancellationToken cancellationToken = default)
    {
        var normalizedSeries = series.Trim().ToUpperInvariant();
        var type = (int)cfeType;
        var records = await _dbContext.CaeAuthorizations
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId
                        && x.CfeType == type
                        && x.Series == normalizedSeries
                        && x.RangeFrom <= rangeTo
                        && rangeFrom <= x.RangeTo)
            .ToListAsync(cancellationToken);
        return records.Select(Map).ToArray();
    }

    public Task AddAuthorizationAsync(CaeAuthorization authorization, CancellationToken cancellationToken = default)
    {
        _dbContext.CaeAuthorizations.Add(ToRecord(authorization));
        return Task.CompletedTask;
    }

    public async Task SaveAuthorizationAsync(CaeAuthorization authorization, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.CaeAuthorizations
            .SingleOrDefaultAsync(
                x => x.OrganizationId == authorization.OrganizationId && x.Id == authorization.Id,
                cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound, "cae.not_found", "CAE authorization was not found.");

        var priorVersion = authorization.Version - 1;
        if (record.Version != priorVersion)
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict, "concurrency_conflict",
                "The CAE authorization changed before this operation could be persisted.",
                conflictType: "stale_version", currentVersion: record.Version.ToString());

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.Status = (int)authorization.Status;
        record.NextNumber = authorization.NextNumber;
        record.Version = authorization.Version;
        record.ActivatedAtUtc = authorization.ActivatedAtUtc;
    }

    public async Task<CaeAllocation?> GetAllocationAsync(
        string organizationId,
        Guid caeId,
        Guid allocationId,
        CancellationToken cancellationToken = default)
    {
        var local = _dbContext.CaeAllocations.Local.SingleOrDefault(
            x => x.OrganizationId == organizationId
                 && x.CaeAuthorizationId == caeId
                 && x.Id == allocationId);
        if (local is not null)
            return Map(local);

        var record = await _dbContext.CaeAllocations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId
                     && x.CaeAuthorizationId == caeId
                     && x.Id == allocationId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<IReadOnlyCollection<CaeAllocation>> GetAllocationsAsync(
        string organizationId,
        Guid caeId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.CaeAllocations
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.CaeAuthorizationId == caeId)
            .OrderBy(x => x.RangeFrom)
            .ToListAsync(cancellationToken);
        return records.Select(Map).ToArray();
    }

    public async Task<PageResult<CaeAllocation>> SearchAllocationsAsync(
        CaeAllocationSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var locations = request.AllowedLocationIds.ToArray();
        var query = _dbContext.CaeAllocations
            .AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId
                        && x.CaeAuthorizationId == request.CaeAuthorizationId
                        && locations.Contains(x.LocationId));
        var total = await query.LongCountAsync(cancellationToken);
        var records = await query
            .OrderBy(x => x.RangeFrom)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new PageResult<CaeAllocation>(
            records.Select(Map).ToArray(), request.Page, request.PageSize, total);
    }

    public Task AddAllocationAsync(CaeAllocation allocation, CancellationToken cancellationToken = default)
    {
        _dbContext.CaeAllocations.Add(ToRecord(allocation));
        return Task.CompletedTask;
    }

    public async Task SaveAllocationAsync(CaeAllocation allocation, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.CaeAllocations
            .SingleOrDefaultAsync(
                x => x.OrganizationId == allocation.OrganizationId
                     && x.CaeAuthorizationId == allocation.CaeAuthorizationId
                     && x.Id == allocation.Id,
                cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound, "cae.allocation_not_found", "CAE allocation was not found.");

        var priorVersion = allocation.Version - 1;
        if (record.Version != priorVersion)
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict, "concurrency_conflict",
                "The CAE allocation changed before this operation could be persisted.",
                conflictType: "stale_version", currentVersion: record.Version.ToString());

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.NextNumber = allocation.NextNumber;
        record.Status = (int)allocation.Status;
        record.Version = allocation.Version;
        record.ClosedAtUtc = allocation.ClosedAtUtc;
    }

    public async Task<FiscalNumberReservation?> GetReservationAsync(
        string organizationId,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        var local = _dbContext.FiscalNumberReservations.Local
            .SingleOrDefault(x => x.OrganizationId == organizationId && x.Id == reservationId);
        if (local is not null)
            return Map(local);

        var record = await _dbContext.FiscalNumberReservations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == reservationId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public Task AddReservationAsync(FiscalNumberReservation reservation, CancellationToken cancellationToken = default)
    {
        _dbContext.FiscalNumberReservations.Add(new V1FiscalNumberReservationRecord
        {
            Id = reservation.Id,
            CaeAuthorizationId = reservation.CaeAuthorizationId,
            AllocationId = reservation.AllocationId,
            OrganizationId = reservation.OrganizationId,
            CfeType = (int)reservation.CfeType,
            Series = reservation.Series,
            Number = reservation.Number,
            LocationId = reservation.LocationId,
            TerminalId = reservation.TerminalId,
            OperationId = reservation.OperationId,
            ReservedAtUtc = reservation.ReservedAtUtc
        });
        return Task.CompletedTask;
    }

    private static V1CaeAuthorizationRecord ToRecord(CaeAuthorization authorization) => new()
    {
        Id = authorization.Id,
        OrganizationId = authorization.OrganizationId,
        CfeType = (int)authorization.CfeType,
        AuthorizationNumber = authorization.AuthorizationNumber,
        Series = authorization.Series,
        RangeFrom = authorization.RangeFrom,
        RangeTo = authorization.RangeTo,
        ValidFrom = authorization.ValidFrom.ToDateTime(TimeOnly.MinValue),
        ValidTo = authorization.ValidTo.ToDateTime(TimeOnly.MinValue),
        Status = (int)authorization.Status,
        VerificationMethod = authorization.VerificationMethod,
        SourceArtifactId = authorization.SourceArtifactId,
        SourceArtifactHash = authorization.SourceArtifactHash,
        SourceName = authorization.SourceName,
        SourceReference = authorization.SourceReference,
        NextNumber = authorization.NextNumber,
        Version = authorization.Version,
        ImportedAtUtc = authorization.ImportedAtUtc,
        ActivatedAtUtc = authorization.ActivatedAtUtc
    };

    private static V1CaeAllocationRecord ToRecord(CaeAllocation allocation) => new()
    {
        Id = allocation.Id,
        CaeAuthorizationId = allocation.CaeAuthorizationId,
        OrganizationId = allocation.OrganizationId,
        LocationId = allocation.LocationId,
        TerminalId = allocation.TerminalId,
        RangeFrom = allocation.RangeFrom,
        RangeTo = allocation.RangeTo,
        NextNumber = allocation.NextNumber,
        Status = (int)allocation.Status,
        Version = allocation.Version,
        CreatedAtUtc = allocation.CreatedAtUtc,
        ClosedAtUtc = allocation.ClosedAtUtc
    };

    private static CaeAuthorization Map(V1CaeAuthorizationRecord record) => CaeAuthorization.Rehydrate(
        record.Id,
        record.OrganizationId,
        (CfeFamily)record.CfeType,
        record.AuthorizationNumber,
        record.Series,
        record.RangeFrom,
        record.RangeTo,
        DateOnly.FromDateTime(record.ValidFrom),
        DateOnly.FromDateTime(record.ValidTo),
        (CaeAuthorizationStatus)record.Status,
        record.VerificationMethod,
        record.SourceArtifactId,
        record.SourceArtifactHash,
        record.SourceName,
        record.SourceReference,
        record.NextNumber,
        record.Version,
        record.ImportedAtUtc,
        record.ActivatedAtUtc);

    private static CaeAllocation Map(V1CaeAllocationRecord record) => CaeAllocation.Rehydrate(
        record.Id,
        record.CaeAuthorizationId,
        record.OrganizationId,
        record.LocationId,
        record.TerminalId,
        record.RangeFrom,
        record.RangeTo,
        record.NextNumber,
        (CaeAllocationStatus)record.Status,
        record.Version,
        record.CreatedAtUtc,
        record.ClosedAtUtc);

    private static FiscalNumberReservation Map(V1FiscalNumberReservationRecord record) => FiscalNumberReservation.Rehydrate(
        record.Id,
        record.CaeAuthorizationId,
        record.AllocationId,
        record.OrganizationId,
        (CfeFamily)record.CfeType,
        record.Series,
        record.Number,
        record.LocationId,
        record.TerminalId,
        record.OperationId,
        record.ReservedAtUtc);
}
