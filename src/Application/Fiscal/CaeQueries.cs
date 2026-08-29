using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Application.Common.Security;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public sealed class Release1CaeMetadataVerifier : ICaeArtifactVerifier
{
    public Task<CaeArtifactVerificationResult> VerifyAsync(
        CaeArtifactVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var findings = new List<string>();

        if (!Enum.IsDefined(request.CfeType)) findings.Add("cae.unsupported_cfe_type");
        if (string.IsNullOrWhiteSpace(request.AuthorizationNumber)) findings.Add("cae.authorization_number_required");
        if (string.IsNullOrWhiteSpace(request.Series)) findings.Add("cae.series_required");
        if (request.RangeFrom <= 0 || request.RangeTo < request.RangeFrom) findings.Add("cae.invalid_range");
        if (request.ValidTo < request.ValidFrom) findings.Add("cae.invalid_validity");
        if (string.IsNullOrWhiteSpace(request.SourceArtifactId)) findings.Add("cae.source_artifact_id_required");
        if (string.IsNullOrWhiteSpace(request.SourceArtifactHash)) findings.Add("cae.source_artifact_hash_required");
        if (string.IsNullOrWhiteSpace(request.SourceName)) findings.Add("cae.source_name_required");
        if (string.IsNullOrWhiteSpace(request.SourceReference)) findings.Add("cae.source_reference_required");

        // This bounded verifier establishes structural metadata consistency only. It deliberately
        // does not claim cryptographic DGI authenticity or certificate/provider verification.
        return Task.FromResult(new CaeArtifactVerificationResult(
            findings.Count == 0,
            "METADATA_CONSISTENCY_V1",
            findings));
    }
}

internal static class CaeAuthorizationGuard
{
    public static ActorContext Ensure(
        IActorContextAccessor actors,
        string organizationId,
        string permission,
        string? locationId = null,
        string? terminalId = null)
    {
        var actor = actors.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(permission))
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden, "permission_denied",
                "The actor is not allowed to perform this fiscal operation.");
        if (!actor.CompanyScopes.Contains(organizationId))
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden, "organization_scope_denied",
                "The actor is outside the requested organization scope.");
        if (!string.IsNullOrWhiteSpace(locationId) && !actor.LocationScopes.Contains(locationId.Trim()))
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden, "location_scope_denied",
                "The actor is outside the requested CAE allocation location scope.");
        if (!string.IsNullOrWhiteSpace(terminalId)
            && actor.TerminalScopes.Count > 0
            && !actor.TerminalScopes.Contains(terminalId.Trim()))
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden, "terminal_scope_denied",
                "The actor is outside the requested CAE allocation terminal scope.");
        return actor;
    }
}

public sealed class ListCaeAuthorizationsUseCase
{
    private readonly ICaeRepository _repository;
    private readonly IActorContextAccessor _actors;

    public ListCaeAuthorizationsUseCase(ICaeRepository repository, IActorContextAccessor actors)
    {
        _repository = repository;
        _actors = actors;
    }

    public Task<PageResult<CaeAuthorization>> ExecuteAsync(
        string organizationId,
        CfeFamily? cfeType,
        CaeAuthorizationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        CaeAuthorizationGuard.Ensure(_actors, organizationId, Permissions.FiscalRead);
        return _repository.SearchAuthorizationsAsync(
            new CaeAuthorizationSearchRequest(organizationId, cfeType, status, page, pageSize),
            cancellationToken);
    }
}

public sealed class GetCaeAuthorizationUseCase
{
    private readonly ICaeRepository _repository;
    private readonly IActorContextAccessor _actors;

    public GetCaeAuthorizationUseCase(ICaeRepository repository, IActorContextAccessor actors)
    {
        _repository = repository;
        _actors = actors;
    }

    public async Task<CaeAuthorization> ExecuteAsync(
        string organizationId,
        Guid caeId,
        CancellationToken cancellationToken = default)
    {
        CaeAuthorizationGuard.Ensure(_actors, organizationId, Permissions.FiscalRead);
        return await _repository.GetAuthorizationAsync(organizationId, caeId, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound, "cae.not_found", "CAE authorization was not found.");
    }
}

public sealed class ListCaeAllocationsUseCase
{
    private readonly ICaeRepository _repository;
    private readonly IActorContextAccessor _actors;

    public ListCaeAllocationsUseCase(ICaeRepository repository, IActorContextAccessor actors)
    {
        _repository = repository;
        _actors = actors;
    }

    public async Task<PageResult<CaeAllocation>> ExecuteAsync(
        string organizationId,
        Guid caeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var actor = CaeAuthorizationGuard.Ensure(_actors, organizationId, Permissions.FiscalRead);
        _ = await _repository.GetAuthorizationAsync(organizationId, caeId, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound, "cae.not_found", "CAE authorization was not found.");

        return await _repository.SearchAllocationsAsync(
            new CaeAllocationSearchRequest(
                organizationId, caeId, actor.LocationScopes.ToArray(), page, pageSize),
            cancellationToken);
    }
}
