using EFactura.Application.Common.Errors;

namespace EFactura.Application.Catalog;

public sealed class TaxSafeUpdateCommercialItemUseCase
{
    private readonly UpdateCommercialItemUseCase _inner;

    public TaxSafeUpdateCommercialItemUseCase(UpdateCommercialItemUseCase inner)
    {
        _inner = inner;
    }

    public Task<CatalogMutationResult> ExecuteAsync(
        UpdateCommercialItemCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ReplaceTaxProfile || command.TaxProfileId.HasValue)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "catalog.tax_profile_assignment_pending",
                "Tax profile assignment is not enabled until the Taxation rule slice provides authoritative validation.");
        }

        return _inner.ExecuteAsync(command, cancellationToken);
    }
}
