using EFactura.Application.Taxation;

namespace EFactura.Application.Catalog;

public sealed class TaxSafeUpdateCommercialItemUseCase
{
    private readonly UpdateCommercialItemUseCase _inner;
    private readonly ITaxProfileAssignmentValidator _taxProfiles;

    public TaxSafeUpdateCommercialItemUseCase(
        UpdateCommercialItemUseCase inner,
        ITaxProfileAssignmentValidator taxProfiles)
    {
        _inner = inner;
        _taxProfiles = taxProfiles;
    }

    public async Task<CatalogMutationResult> ExecuteAsync(
        UpdateCommercialItemCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ReplaceTaxProfile && command.TaxProfileId.HasValue)
        {
            await _taxProfiles.ValidateAssignableAsync(
                command.OrganizationId,
                command.TaxProfileId.Value,
                DateOnly.FromDateTime(DateTime.UtcNow),
                cancellationToken);
        }

        return await _inner.ExecuteAsync(command, cancellationToken);
    }
}
