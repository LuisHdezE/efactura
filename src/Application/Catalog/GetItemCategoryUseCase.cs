using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Context;

namespace EFactura.Application.Catalog;

public sealed class GetItemCategoryUseCase
{
    private readonly IItemCategoryRepository _categories;
    private readonly IActorContextAccessor _actorContext;

    public GetItemCategoryUseCase(IItemCategoryRepository categories, IActorContextAccessor actorContext)
    {
        _categories = categories;
        _actorContext = actorContext;
    }

    public async Task<ItemCategoryView> ExecuteAsync(
        string organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        ListCommercialItemsUseCase.EnsureCatalogReadAuthorized(_actorContext.Current, organizationId);
        var category = await _categories.GetAsync(organizationId, categoryId, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "catalog.category_not_found",
                "The requested category was not found.");

        return ItemCategoryView.FromDomain(category);
    }
}
