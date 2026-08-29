using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Fiscal;

public interface ICfeSelectionConfigurationProvider
{
    Task<CfeSelectionConfiguration> GetAsync(
        string organizationId,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default);
}

public sealed class Release1CfeSelectionConfigurationProvider : ICfeSelectionConfigurationProvider
{
    public Task<CfeSelectionConfiguration> GetAsync(
        string organizationId,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);

        // Release 1 deliberately has no implicit export-service documentation default.
        // The organization must later configure ExportCombo or UsualCfe explicitly.
        return Task.FromResult(new CfeSelectionConfiguration(
            ExportServiceDocumentationStrategy.Unconfigured));
    }
}

public sealed record SelectCfeRequest(
    string OrganizationId,
    DateOnly EffectiveOn,
    TaxTreatmentDecision TaxTreatment,
    CfeEligibilityResult Eligibility);

public sealed class SelectCfeUseCase
{
    private readonly ICfeSelectionConfigurationProvider _configuration;
    private readonly CfeSelectionPolicy _policy;

    public SelectCfeUseCase(
        ICfeSelectionConfigurationProvider configuration,
        CfeSelectionPolicy policy)
    {
        _configuration = configuration;
        _policy = policy;
    }

    public async Task<CfeSelectionResult> ExecuteAsync(
        SelectCfeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.TaxTreatment);
        ArgumentNullException.ThrowIfNull(request.Eligibility);

        var configuration = await _configuration.GetAsync(
            request.OrganizationId,
            request.EffectiveOn,
            cancellationToken);

        return _policy.Select(request.Eligibility, request.TaxTreatment, configuration);
    }
}
