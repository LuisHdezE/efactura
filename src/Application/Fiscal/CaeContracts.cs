using EFactura.Application.Common.Results;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public sealed record CaeAuthorizationSearchRequest(
    string OrganizationId,
    CfeFamily? CfeType,
    CaeAuthorizationStatus? Status,
    int Page = 1,
    int PageSize = 50);

public sealed record CaeAllocationSearchRequest(
    string OrganizationId,
    Guid CaeAuthorizationId,
    IReadOnlyCollection<string> AllowedLocationIds,
    int Page = 1,
    int PageSize = 50);

public interface ICaeRepository
{
    Task<CaeAuthorization?> GetAuthorizationAsync(
        string organizationId,
        Guid caeId,
        CancellationToken cancellationToken = default);

    Task<CaeAuthorization?> FindByArtifactAsync(
        string organizationId,
        string sourceArtifactHash,
        CancellationToken cancellationToken = default);

    Task<PageResult<CaeAuthorization>> SearchAuthorizationsAsync(
        CaeAuthorizationSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CaeAuthorization>> FindOverlappingAuthorizationsAsync(
        string organizationId,
        CfeFamily cfeType,
        string series,
        long rangeFrom,
        long rangeTo,
        CancellationToken cancellationToken = default);

    Task AddAuthorizationAsync(CaeAuthorization authorization, CancellationToken cancellationToken = default);
    Task SaveAuthorizationAsync(CaeAuthorization authorization, CancellationToken cancellationToken = default);

    Task<CaeAllocation?> GetAllocationAsync(
        string organizationId,
        Guid caeId,
        Guid allocationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CaeAllocation>> GetAllocationsAsync(
        string organizationId,
        Guid caeId,
        CancellationToken cancellationToken = default);

    Task<PageResult<CaeAllocation>> SearchAllocationsAsync(
        CaeAllocationSearchRequest request,
        CancellationToken cancellationToken = default);

    Task AddAllocationAsync(CaeAllocation allocation, CancellationToken cancellationToken = default);
    Task SaveAllocationAsync(CaeAllocation allocation, CancellationToken cancellationToken = default);

    Task<FiscalNumberReservation?> GetReservationAsync(
        string organizationId,
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task AddReservationAsync(FiscalNumberReservation reservation, CancellationToken cancellationToken = default);
}

public sealed record CaeArtifactVerificationRequest(
    CfeFamily CfeType,
    string AuthorizationNumber,
    string Series,
    long RangeFrom,
    long RangeTo,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string SourceArtifactId,
    string SourceArtifactHash,
    string SourceName,
    string SourceReference);

public sealed record CaeArtifactVerificationResult(
    bool Verified,
    string VerificationMethod,
    IReadOnlyCollection<string> Findings);

public interface ICaeArtifactVerifier
{
    Task<CaeArtifactVerificationResult> VerifyAsync(
        CaeArtifactVerificationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record FiscalNumberReservationRequest(
    string OrganizationId,
    CfeFamily CfeType,
    DateOnly FiscalDate,
    string OperationId,
    string? LocationId,
    string? TerminalId);

public sealed record FiscalNumberReservationResult(
    Guid ReservationId,
    Guid CaeAuthorizationId,
    Guid? AllocationId,
    CfeFamily CfeType,
    string Series,
    long Number,
    DateTimeOffset ReservedAtUtc,
    bool AuthorizationExhausted,
    bool AllocationExhausted);

public interface IFiscalNumberAllocator
{
    /// <summary>
    /// Stages one authoritative fiscal-number reservation inside the caller's existing
    /// Application transaction. It never starts/commits a transaction and never calls SaveChanges.
    /// </summary>
    Task<FiscalNumberReservationResult> ReserveAsync(
        FiscalNumberReservationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ImportCaeAuthorizationCommand(
    string OrganizationId,
    CfeFamily CfeType,
    string AuthorizationNumber,
    string Series,
    long RangeFrom,
    long RangeTo,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string SourceArtifactId,
    string SourceArtifactHash,
    string SourceName,
    string SourceReference,
    string IdempotencyKey,
    string RequestHash);

public sealed record ActivateCaeAuthorizationCommand(
    string OrganizationId,
    Guid CaeId,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed record CreateCaeAllocationCommand(
    string OrganizationId,
    Guid CaeId,
    string LocationId,
    string? TerminalId,
    long RangeFrom,
    long RangeTo,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed record CloseCaeAllocationCommand(
    string OrganizationId,
    Guid CaeId,
    Guid AllocationId,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed record CaeAuthorizationMutationResult(CaeAuthorization Authorization, bool Replayed);
public sealed record CaeAllocationMutationResult(CaeAllocation Allocation, bool Replayed);
