using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Sales;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;

namespace EFactura.Application.Fiscal;

public interface IFiscalDocumentRepository
{
    Task<FiscalDocument?> GetAsync(
        string organizationId,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default);

    Task<FiscalDocument?> GetByFiscalizationRequestAsync(
        string organizationId,
        Guid fiscalizationRequestId,
        CancellationToken cancellationToken = default);

    Task AddAsync(FiscalDocument document, CancellationToken cancellationToken = default);
}

public sealed record PrepareFiscalDocumentIdentityCommand(
    string OrganizationId,
    Guid FiscalizationRequestId);

public sealed record FiscalDocumentIdentityResult(
    Guid FiscalDocumentId,
    Guid FiscalizationRequestId,
    Guid SaleId,
    CfeFamily CfeType,
    string Series,
    long Number,
    string CaeAuthorizationNumber,
    string FormatVersion,
    DateTimeOffset IdentityCreatedAtUtc,
    bool Replayed);

public sealed record FiscalDocumentIdentityCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid FiscalDocumentId,
    Guid FiscalizationRequestId,
    Guid SaleId,
    string OrganizationId,
    int CfeType,
    string Series,
    long Number,
    Guid FiscalNumberReservationId,
    Guid CaeAuthorizationId) : IIntegrationEvent;

/// <summary>
/// Consumes one durable fiscalization work item and atomically creates the server-owned fiscal
/// identity. The work-item id is the retry identity. The use case deliberately stops before XML,
/// validation, signing, artifact persistence and transport.
/// </summary>
public sealed class PrepareFiscalDocumentIdentityUseCase
{
    private readonly IFiscalizationRequestRepository _requests;
    private readonly IFiscalDocumentRepository _documents;
    private readonly ISaleRepository _sales;
    private readonly IFiscalNumberAllocator _numbers;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public PrepareFiscalDocumentIdentityUseCase(
        IFiscalizationRequestRepository requests,
        IFiscalDocumentRepository documents,
        ISaleRepository sales,
        IFiscalNumberAllocator numbers,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _requests = requests;
        _documents = documents;
        _sales = sales;
        _numbers = numbers;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<FiscalDocumentIdentityResult> ExecuteAsync(
        PrepareFiscalDocumentIdentityCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId))
            throw Validation("fiscalization.organization_required", "Organization id is required.");
        if (command.FiscalizationRequestId == Guid.Empty)
            throw Validation("fiscalization.request_id_required", "Fiscalization request id is required.");

        return _transactions.ExecuteAsync(async ct =>
        {
            var request = await _requests.GetAsync(
                command.OrganizationId,
                command.FiscalizationRequestId,
                ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound,
                    "fiscalization.not_found",
                    "Fiscalization request was not found.");

            if (request.Status == FiscalizationRequestStatus.IdentityCreated)
                return await ReplayAsync(request, ct);

            if (request.Status != FiscalizationRequestStatus.Pending)
                throw Conflict(
                    "fiscalization.invalid_state",
                    "Fiscalization request is not eligible for identity creation.",
                    "invalid_state");

            var preexisting = await _documents.GetByFiscalizationRequestAsync(
                command.OrganizationId,
                request.Id,
                ct);
            if (preexisting is not null)
                throw Conflict(
                    "fiscalization.inconsistent_identity_state",
                    "A fiscal document exists while its fiscalization request is still pending.",
                    "inconsistent_state");

            var sale = await _sales.GetAsync(command.OrganizationId, request.SaleId, ct)
                ?? throw Conflict(
                    "fiscalization.source_sale_missing",
                    "The confirmed source sale no longer exists.",
                    "inconsistent_state");
            EnsureConfirmedSaleMatches(request, sale);

            var reservation = await _numbers.ReserveAsync(
                new FiscalNumberReservationRequest(
                    request.OrganizationId,
                    request.CfeFamily,
                    sale.EffectiveOn,
                    OperationId(request.Id),
                    request.LocationId,
                    request.TerminalId),
                ct);

            var now = DateTimeOffset.UtcNow;
            FiscalDocument document;
            try
            {
                document = FiscalDocument.CreateIdentity(
                    Guid.NewGuid(),
                    request.OrganizationId,
                    request.Id,
                    request.SaleId,
                    reservation.ReservationId,
                    reservation.CaeAuthorizationId,
                    reservation.AllocationId,
                    reservation.CfeType,
                    reservation.Series,
                    reservation.Number,
                    reservation.CaeAuthorizationNumber,
                    reservation.CaeRangeFrom,
                    reservation.CaeRangeTo,
                    reservation.CaeValidFrom,
                    reservation.CaeValidTo,
                    sale.EffectiveOn,
                    request.LocationId,
                    request.TerminalId,
                    request.ReceiverIdentification,
                    request.FormatVersion,
                    request.ConfirmationFingerprint,
                    request.SettlementFingerprint,
                    request.CurrencyCode,
                    request.NetAmount,
                    request.VatAmount,
                    request.TotalAmount,
                    now);

                request.MarkIdentityCreated(document.Id, now, request.Version);
            }
            catch (DomainRuleException ex)
            {
                throw Map(ex);
            }

            await _documents.AddAsync(document, ct);
            await _requests.SaveAsync(request, ct);

            var actor = _actors.Current;
            var correlation = _correlations.Current;
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(),
                now,
                "FISCAL_DOCUMENT_IDENTITY_CREATED",
                actor.ActorId,
                request.OrganizationId,
                request.LocationId,
                request.TerminalId,
                "FiscalDocument",
                document.Id.ToString(),
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                null,
                new Dictionary<string, string?>
                {
                    ["fiscalizationRequestId"] = request.Id.ToString(),
                    ["saleId"] = request.SaleId.ToString(),
                    ["fiscalNumberReservationId"] = reservation.ReservationId.ToString(),
                    ["caeAuthorizationId"] = reservation.CaeAuthorizationId.ToString(),
                    ["caeAllocationId"] = reservation.AllocationId?.ToString(),
                    ["cfeType"] = ((int)document.CfeType).ToString(),
                    ["series"] = document.Series,
                    ["number"] = document.Number.ToString(),
                    ["formatVersion"] = document.FormatVersion,
                    ["confirmationFingerprint"] = document.ConfirmationFingerprint,
                    ["settlementFingerprint"] = document.SettlementFingerprint
                }),
                ct);

            await _outbox.EnqueueAsync(
                new FiscalDocumentIdentityCreatedIntegrationEvent(
                    Guid.NewGuid(),
                    now,
                    document.Id,
                    request.Id,
                    request.SaleId,
                    request.OrganizationId,
                    (int)document.CfeType,
                    document.Series,
                    document.Number,
                    reservation.ReservationId,
                    reservation.CaeAuthorizationId),
                new OutboxContext(
                    correlation.CorrelationId,
                    null,
                    request.OrganizationId,
                    actor.ActorId),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return Result(document, false);
        }, cancellationToken);
    }

    private async Task<FiscalDocumentIdentityResult> ReplayAsync(
        FiscalizationRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.FiscalDocumentId.HasValue || !request.IdentityCreatedAtUtc.HasValue)
            throw Conflict(
                "fiscalization.inconsistent_identity_state",
                "Completed fiscalization identity state is missing durable identity evidence.",
                "inconsistent_replay");

        var document = await _documents.GetByFiscalizationRequestAsync(
            request.OrganizationId,
            request.Id,
            cancellationToken)
            ?? throw Conflict(
                "fiscalization.inconsistent_identity_state",
                "Completed fiscalization identity state no longer has its fiscal document.",
                "inconsistent_replay");

        if (document.Id != request.FiscalDocumentId.Value
            || document.SaleId != request.SaleId
            || document.CfeType != request.CfeFamily
            || !string.Equals(document.ConfirmationFingerprint, request.ConfirmationFingerprint, StringComparison.Ordinal)
            || !string.Equals(document.SettlementFingerprint, request.SettlementFingerprint, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscalization.inconsistent_identity_state",
                "Completed fiscalization identity evidence no longer matches its source work item.",
                "inconsistent_replay");
        }

        return Result(document, true);
    }

    private static void EnsureConfirmedSaleMatches(FiscalizationRequest request, Sale sale)
    {
        if (sale.Status != SaleStatus.Confirmed
            || string.IsNullOrWhiteSpace(sale.ConfirmationFingerprint)
            || string.IsNullOrWhiteSpace(sale.SettlementFingerprint)
            || !sale.ConfirmedAtUtc.HasValue)
        {
            throw Conflict(
                "fiscalization.source_sale_not_confirmed",
                "Fiscalization requires the immutable confirmed source sale.",
                "invalid_state");
        }

        if (!string.Equals(sale.ConfirmationFingerprint, request.ConfirmationFingerprint, StringComparison.Ordinal)
            || !string.Equals(sale.SettlementFingerprint, request.SettlementFingerprint, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscalization.source_evidence_mismatch",
                "Fiscalization work item no longer matches the confirmed source sale evidence.",
                "inconsistent_state");
        }
    }

    private static string OperationId(Guid requestId) => $"fiscalization:{requestId:N}";

    private static FiscalDocumentIdentityResult Result(FiscalDocument document, bool replayed) =>
        new(
            document.Id,
            document.FiscalizationRequestId,
            document.SaleId,
            document.CfeType,
            document.Series,
            document.Number,
            document.CaeAuthorizationNumber,
            document.FormatVersion,
            document.IdentityCreatedAtUtc,
            replayed);

    private static ApplicationProblemException Map(DomainRuleException ex) =>
        ex.Code == "concurrency.stale_version"
            ? Conflict(ex.Code, ex.Message, "stale_version")
            : Validation(ex.Code, ex.Message);

    private static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(
            ApplicationProblemKind.Conflict,
            code,
            message,
            conflictType: conflictType);
}
