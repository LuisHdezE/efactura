using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;

namespace EFactura.Application.Sales;

public enum SaleFiscalizationWorkflowStatus
{
    NotRequested = 1,
    Pending = 2,
    IdentityCreated = 3
}

public sealed record SaleFiscalDocumentIdentityView(
    Guid Id,
    CfeFamily CfeType,
    string Series,
    long Number,
    DateOnly FiscalDate,
    DateTimeOffset IdentityCreatedAtUtc);

public sealed record SaleFiscalizationStatusView(
    Guid SaleId,
    SaleStatus SaleStatus,
    SaleFiscalizationWorkflowStatus WorkflowStatus,
    Guid? FiscalizationRequestId,
    long? FiscalizationVersion,
    DateTimeOffset? RequestedAtUtc,
    CfeFamily? CfeFamily,
    string? FormatVersion,
    SaleFiscalDocumentIdentityView? FiscalDocument);

/// <summary>
/// Projects the local sale fiscalization workflow only. It never interprets DGI/provider transport
/// or acceptance state and performs no mutation, transaction, idempotency, audit or outbox work.
/// </summary>
public sealed class GetSaleFiscalizationStatusUseCase
{
    private readonly ISaleRepository _sales;
    private readonly IFiscalizationRequestRepository _requests;
    private readonly IFiscalDocumentRepository _documents;
    private readonly IActorContextAccessor _actors;

    public GetSaleFiscalizationStatusUseCase(
        ISaleRepository sales,
        IFiscalizationRequestRepository requests,
        IFiscalDocumentRepository documents,
        IActorContextAccessor actors)
    {
        _sales = sales;
        _requests = requests;
        _documents = documents;
        _actors = actors;
    }

    public async Task<SaleFiscalizationStatusView> ExecuteAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        SalesAuthorization.Ensure(_actors, organizationId, Permissions.SalesRead);

        var sale = await _sales.GetAsync(organizationId, saleId, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "sales.not_found",
                "Sale was not found.");

        var request = await _requests.GetBySaleAsync(organizationId, saleId, cancellationToken);
        if (request is null)
        {
            if (sale.Status is SaleStatus.Draft or SaleStatus.Validated)
            {
                return new SaleFiscalizationStatusView(
                    sale.Id,
                    sale.Status,
                    SaleFiscalizationWorkflowStatus.NotRequested,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null);
            }

            throw Inconsistent("Confirmed sale is missing its durable fiscalization request.");
        }

        if (request.SaleId != sale.Id
            || !string.Equals(request.OrganizationId, sale.OrganizationId, StringComparison.Ordinal))
        {
            throw Inconsistent("Fiscalization request does not match its source sale.");
        }

        if (sale.Status != SaleStatus.Confirmed)
            throw Inconsistent("A fiscalization request exists for a sale that is not confirmed.");

        if (!string.Equals(request.ConfirmationFingerprint, sale.ConfirmationFingerprint, StringComparison.Ordinal)
            || !string.Equals(request.SettlementFingerprint, sale.SettlementFingerprint, StringComparison.Ordinal))
        {
            throw Inconsistent("Fiscalization request evidence does not match the confirmed sale.");
        }

        var document = await _documents.GetByFiscalizationRequestAsync(
            organizationId,
            request.Id,
            cancellationToken);

        return request.Status switch
        {
            FiscalizationRequestStatus.Pending => Pending(sale, request, document),
            FiscalizationRequestStatus.IdentityCreated => IdentityCreated(sale, request, document),
            _ => throw Inconsistent("Fiscalization request uses an unsupported workflow state.")
        };
    }

    private static SaleFiscalizationStatusView Pending(
        Sale sale,
        FiscalizationRequest request,
        FiscalDocument? document)
    {
        if (request.FiscalDocumentId.HasValue
            || request.IdentityCreatedAtUtc.HasValue
            || document is not null)
        {
            throw Inconsistent("Pending fiscalization already carries fiscal-document identity evidence.");
        }

        return new SaleFiscalizationStatusView(
            sale.Id,
            sale.Status,
            SaleFiscalizationWorkflowStatus.Pending,
            request.Id,
            request.Version,
            request.RequestedAtUtc,
            request.CfeFamily,
            request.FormatVersion,
            null);
    }

    private static SaleFiscalizationStatusView IdentityCreated(
        Sale sale,
        FiscalizationRequest request,
        FiscalDocument? document)
    {
        if (!request.FiscalDocumentId.HasValue
            || !request.IdentityCreatedAtUtc.HasValue
            || document is null)
        {
            throw Inconsistent("Identity-created fiscalization is missing durable fiscal-document evidence.");
        }

        if (document.Id != request.FiscalDocumentId.Value
            || document.FiscalizationRequestId != request.Id
            || document.SaleId != sale.Id
            || document.CfeType != request.CfeFamily
            || document.Status != FiscalDocumentStatus.IdentityCreated
            || document.IdentityCreatedAtUtc != request.IdentityCreatedAtUtc.Value
            || !string.Equals(document.FormatVersion, request.FormatVersion, StringComparison.Ordinal)
            || !string.Equals(document.ConfirmationFingerprint, request.ConfirmationFingerprint, StringComparison.Ordinal)
            || !string.Equals(document.SettlementFingerprint, request.SettlementFingerprint, StringComparison.Ordinal))
        {
            throw Inconsistent("Fiscal-document identity no longer matches its fiscalization request.");
        }

        return new SaleFiscalizationStatusView(
            sale.Id,
            sale.Status,
            SaleFiscalizationWorkflowStatus.IdentityCreated,
            request.Id,
            request.Version,
            request.RequestedAtUtc,
            request.CfeFamily,
            request.FormatVersion,
            new SaleFiscalDocumentIdentityView(
                document.Id,
                document.CfeType,
                document.Series,
                document.Number,
                document.FiscalDate,
                document.IdentityCreatedAtUtc));
    }

    private static ApplicationProblemException Inconsistent(string detail) =>
        new(
            ApplicationProblemKind.Conflict,
            "fiscalization.inconsistent_state",
            detail,
            conflictType: "inconsistent_state");
}
