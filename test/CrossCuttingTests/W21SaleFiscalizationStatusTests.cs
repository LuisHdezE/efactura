using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Application.Common.Security;
using EFactura.Application.Fiscal;
using EFactura.Application.Sales;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;
using Xunit;

namespace CrossCuttingTests;

public sealed class W21SaleFiscalizationStatusTests
{
    private const string OrganizationId = "org-a";
    private const string ConfirmationFingerprint = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string SettlementFingerprint = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public async Task Draft_without_request_projects_not_requested()
    {
        var sale = SaleOf(SaleStatus.Draft);
        var result = await UseCase(sale).ExecuteAsync(OrganizationId, sale.Id);

        Assert.Equal(SaleFiscalizationWorkflowStatus.NotRequested, result.WorkflowStatus);
        Assert.Null(result.FiscalizationRequestId);
        Assert.Null(result.FiscalDocument);
    }

    [Fact]
    public async Task Validated_without_request_projects_not_requested()
    {
        var sale = SaleOf(SaleStatus.Validated);
        var result = await UseCase(sale).ExecuteAsync(OrganizationId, sale.Id);

        Assert.Equal(SaleFiscalizationWorkflowStatus.NotRequested, result.WorkflowStatus);
        Assert.Equal(SaleStatus.Validated, result.SaleStatus);
    }

    [Fact]
    public async Task Confirmed_with_pending_request_projects_local_pending_only()
    {
        var sale = SaleOf(SaleStatus.Confirmed);
        var request = RequestFor(sale);
        var result = await UseCase(sale, request).ExecuteAsync(OrganizationId, sale.Id);

        Assert.Equal(SaleFiscalizationWorkflowStatus.Pending, result.WorkflowStatus);
        Assert.Equal(request.Id, result.FiscalizationRequestId);
        Assert.Equal(request.Version, result.FiscalizationVersion);
        Assert.Equal(CfeFamily.ETicket, result.CfeFamily);
        Assert.Null(result.FiscalDocument);
    }

    [Fact]
    public async Task Confirmed_with_matching_identity_projects_bounded_document_identity()
    {
        var sale = SaleOf(SaleStatus.Confirmed);
        var request = RequestFor(sale);
        var document = DocumentFor(sale, request);
        request.MarkIdentityCreated(document.Id, document.IdentityCreatedAtUtc, request.Version);

        var result = await UseCase(sale, request, document).ExecuteAsync(OrganizationId, sale.Id);

        Assert.Equal(SaleFiscalizationWorkflowStatus.IdentityCreated, result.WorkflowStatus);
        Assert.NotNull(result.FiscalDocument);
        Assert.Equal(document.Id, result.FiscalDocument!.Id);
        Assert.Equal(document.CfeType, result.FiscalDocument.CfeType);
        Assert.Equal(document.Series, result.FiscalDocument.Series);
        Assert.Equal(document.Number, result.FiscalDocument.Number);
    }

    [Fact]
    public async Task Confirmed_without_request_fails_closed()
    {
        var sale = SaleOf(SaleStatus.Confirmed);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => UseCase(sale).ExecuteAsync(OrganizationId, sale.Id));

        Assert.Equal(ApplicationProblemKind.Conflict, error.Kind);
        Assert.Equal("fiscalization.inconsistent_state", error.Code);
        Assert.Equal("inconsistent_state", error.ConflictType);
    }

    [Fact]
    public async Task Non_confirmed_sale_with_request_fails_closed()
    {
        var sale = SaleOf(SaleStatus.Validated);
        var request = RequestFor(sale);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => UseCase(sale, request).ExecuteAsync(OrganizationId, sale.Id));

        Assert.Equal("fiscalization.inconsistent_state", error.Code);
    }

    [Fact]
    public async Task Pending_request_with_preexisting_document_fails_closed()
    {
        var sale = SaleOf(SaleStatus.Confirmed);
        var request = RequestFor(sale);
        var document = DocumentFor(sale, request);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => UseCase(sale, request, document).ExecuteAsync(OrganizationId, sale.Id));

        Assert.Equal("fiscalization.inconsistent_state", error.Code);
    }

    [Fact]
    public async Task Identity_created_without_document_fails_closed()
    {
        var sale = SaleOf(SaleStatus.Confirmed);
        var request = RequestFor(sale);
        var document = DocumentFor(sale, request);
        request.MarkIdentityCreated(document.Id, document.IdentityCreatedAtUtc, request.Version);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => UseCase(sale, request).ExecuteAsync(OrganizationId, sale.Id));

        Assert.Equal("fiscalization.inconsistent_state", error.Code);
    }

    [Fact]
    public async Task Missing_sales_read_permission_is_forbidden()
    {
        var sale = SaleOf(SaleStatus.Draft);
        var actor = Actor(OrganizationId, permissions: Array.Empty<string>());

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => UseCase(sale, actor: actor).ExecuteAsync(OrganizationId, sale.Id));

        Assert.Equal(ApplicationProblemKind.Forbidden, error.Kind);
        Assert.Equal("permission_denied", error.Code);
    }

    [Fact]
    public async Task Cross_organization_sale_is_masked_as_not_found()
    {
        var sale = SaleOf(SaleStatus.Draft);
        const string otherOrganization = "org-b";
        var actor = Actor(otherOrganization, Permissions.SalesRead);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => UseCase(sale, actor: actor).ExecuteAsync(otherOrganization, sale.Id));

        Assert.Equal(ApplicationProblemKind.NotFound, error.Kind);
        Assert.Equal("sales.not_found", error.Code);
    }

    private static GetSaleFiscalizationStatusUseCase UseCase(
        Sale sale,
        FiscalizationRequest? request = null,
        FiscalDocument? document = null,
        ActorContext? actor = null) =>
        new(
            new SaleRepository(sale),
            new RequestRepository(request),
            new DocumentRepository(document),
            new ActorAccessor(actor ?? Actor(OrganizationId, Permissions.SalesRead)));

    private static ActorContext Actor(string organizationId, params string[] permissions) => new(
        "actor-w21",
        "W2.1 Reader",
        true,
        new HashSet<string>(permissions, StringComparer.Ordinal),
        new HashSet<string>(new[] { organizationId }, StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal),
        null);

    private static Sale SaleOf(SaleStatus status)
    {
        var line = SaleLine.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SKU-1",
            "Item",
            SaleLineKind.Product,
            1m,
            100m,
            null);
        var now = DateTimeOffset.UtcNow;

        return Sale.Rehydrate(
            Guid.NewGuid(),
            OrganizationId,
            "loc-1",
            "term-1",
            null,
            SaleCommercialIntent.ConsumerFinal,
            "UYU",
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            false,
            new[] { line },
            status,
            status is SaleStatus.Validated or SaleStatus.Confirmed ? "validation-fingerprint" : null,
            status is SaleStatus.Validated or SaleStatus.Confirmed ? now.AddMinutes(-2) : null,
            status switch
            {
                SaleStatus.Draft => 1,
                SaleStatus.Validated => 2,
                SaleStatus.Confirmed => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(status))
            },
            status == SaleStatus.Confirmed ? ConfirmationFingerprint : null,
            status == SaleStatus.Confirmed ? SettlementFingerprint : null,
            status == SaleStatus.Confirmed ? now.AddMinutes(-1) : null);
    }

    private static FiscalizationRequest RequestFor(Sale sale) =>
        FiscalizationRequest.CreateFromSale(
            Guid.NewGuid(),
            OrganizationId,
            sale.Id,
            sale.LocationId,
            sale.TerminalId,
            CfeFamily.ETicket,
            null,
            "25.2",
            ConfirmationFingerprint,
            SettlementFingerprint,
            "UYU",
            100m,
            22m,
            122m,
            DateTimeOffset.UtcNow);

    private static FiscalDocument DocumentFor(Sale sale, FiscalizationRequest request)
    {
        var createdAt = DateTimeOffset.UtcNow;
        var fiscalDate = sale.EffectiveOn;
        return FiscalDocument.CreateIdentity(
            Guid.NewGuid(),
            OrganizationId,
            request.Id,
            sale.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            CfeFamily.ETicket,
            "A",
            1,
            "CAE-1",
            1,
            100,
            fiscalDate.AddDays(-1),
            fiscalDate.AddDays(1),
            fiscalDate,
            sale.LocationId,
            sale.TerminalId,
            null,
            "25.2",
            ConfirmationFingerprint,
            SettlementFingerprint,
            "UYU",
            100m,
            22m,
            122m,
            createdAt);
    }

    private sealed class ActorAccessor : IActorContextAccessor
    {
        public ActorAccessor(ActorContext current) => Current = current;
        public ActorContext Current { get; }
    }

    private sealed class SaleRepository(Sale sale) : ISaleRepository
    {
        public Task<Sale?> GetAsync(string organizationId, Guid saleId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Sale?>(
                string.Equals(organizationId, sale.OrganizationId, StringComparison.Ordinal) && saleId == sale.Id
                    ? sale
                    : null);

        public Task AddAsync(Sale value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PageResult<Sale>> SearchAsync(SaleSearchRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(Sale value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RequestRepository(FiscalizationRequest? request) : IFiscalizationRequestRepository
    {
        public Task<FiscalizationRequest?> GetAsync(string organizationId, Guid requestId, CancellationToken cancellationToken = default) =>
            Task.FromResult<FiscalizationRequest?>(
                request is not null
                && string.Equals(request.OrganizationId, organizationId, StringComparison.Ordinal)
                && request.Id == requestId
                    ? request
                    : null);

        public Task<FiscalizationRequest?> GetBySaleAsync(string organizationId, Guid saleId, CancellationToken cancellationToken = default) =>
            Task.FromResult<FiscalizationRequest?>(
                request is not null
                && string.Equals(request.OrganizationId, organizationId, StringComparison.Ordinal)
                && request.SaleId == saleId
                    ? request
                    : null);

        public Task AddAsync(FiscalizationRequest value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(FiscalizationRequest value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DocumentRepository(FiscalDocument? document) : IFiscalDocumentRepository
    {
        public Task<FiscalDocument?> GetAsync(string organizationId, Guid fiscalDocumentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<FiscalDocument?>(
                document is not null
                && string.Equals(document.OrganizationId, organizationId, StringComparison.Ordinal)
                && document.Id == fiscalDocumentId
                    ? document
                    : null);

        public Task<FiscalDocument?> GetByFiscalizationRequestAsync(string organizationId, Guid fiscalizationRequestId, CancellationToken cancellationToken = default) =>
            Task.FromResult<FiscalDocument?>(
                document is not null
                && string.Equals(document.OrganizationId, organizationId, StringComparison.Ordinal)
                && document.FiscalizationRequestId == fiscalizationRequestId
                    ? document
                    : null);

        public Task AddAsync(FiscalDocument value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
