using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Sales;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public sealed record StoredFiscalContentSnapshot(
    Guid Id,
    string OrganizationId,
    Guid FiscalDocumentId,
    Guid FiscalizationRequestId,
    Guid SaleId,
    FiscalContentSnapshot Snapshot,
    DateTimeOffset CreatedAtUtc);

public interface IFiscalContentSnapshotRepository
{
    Task<StoredFiscalContentSnapshot?> GetByFiscalDocumentAsync(
        string organizationId,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalContentSnapshot snapshot,
        CancellationToken cancellationToken = default);
}

public sealed record CreateFiscalContentSnapshotCommand(
    string OrganizationId,
    Guid FiscalDocumentId);

public sealed record FiscalContentSnapshotResult(
    Guid SnapshotId,
    Guid FiscalDocumentId,
    Guid FiscalizationRequestId,
    Guid SaleId,
    string ContentFingerprint,
    DateTimeOffset CreatedAtUtc,
    bool Replayed);

public sealed record FiscalContentSnapshotCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SnapshotId,
    Guid FiscalDocumentId,
    Guid FiscalizationRequestId,
    Guid SaleId,
    string OrganizationId,
    string ContentFingerprint,
    string ConfirmationEvidenceFingerprint) : IIntegrationEvent;

/// <summary>
/// Creates the immutable CFE content snapshot only after a FiscalDocument identity already exists.
/// The use case consumes fiscal calculation evidence frozen at Sale confirmation and snapshots
/// mutable Organization/Location/Party masters exactly once. XML, XSD validation, signing and
/// transport remain outside this boundary.
/// </summary>
public sealed class CreateFiscalContentSnapshotUseCase
{
    private readonly IFiscalDocumentRepository _documents;
    private readonly IFiscalizationRequestRepository _requests;
    private readonly ISaleRepository _sales;
    private readonly IFiscalContentSnapshotFactory _factory;
    private readonly IFiscalContentSnapshotRepository _snapshots;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public CreateFiscalContentSnapshotUseCase(
        IFiscalDocumentRepository documents,
        IFiscalizationRequestRepository requests,
        ISaleRepository sales,
        IFiscalContentSnapshotFactory factory,
        IFiscalContentSnapshotRepository snapshots,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _documents = documents;
        _requests = requests;
        _sales = sales;
        _factory = factory;
        _snapshots = snapshots;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<FiscalContentSnapshotResult> ExecuteAsync(
        CreateFiscalContentSnapshotCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId))
            throw Validation("fiscal.snapshot.organization_required", "Organization id is required.");
        if (command.FiscalDocumentId == Guid.Empty)
            throw Validation("fiscal.snapshot.document_id_required", "Fiscal document id is required.");

        var organizationId = command.OrganizationId.Trim();
        return _transactions.ExecuteAsync(async ct =>
        {
            var document = await _documents.GetAsync(organizationId, command.FiscalDocumentId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound,
                    "fiscal.document_not_found",
                    "Fiscal document was not found.");

            var existing = await _snapshots.GetByFiscalDocumentAsync(organizationId, document.Id, ct);
            if (existing is not null)
            {
                EnsureStoredMatchesDocument(document, existing);
                return Result(existing, true);
            }

            if (document.Status != FiscalDocumentStatus.IdentityCreated)
            {
                throw Conflict(
                    "fiscal.snapshot.document_state_invalid",
                    "Fiscal content snapshot requires an already-created fiscal document identity.",
                    "invalid_state");
            }

            var request = await _requests.GetAsync(organizationId, document.FiscalizationRequestId, ct)
                ?? throw Conflict(
                    "fiscal.snapshot.fiscalization_request_missing",
                    "The fiscalization work item linked to the fiscal document no longer exists.",
                    "inconsistent_state");
            EnsureRequestMatchesDocument(request, document);

            if (request.ConfirmationEvidence is null)
            {
                throw Conflict(
                    "fiscal.snapshot.confirmation_evidence_missing",
                    "This fiscalization work item predates durable line-level fiscal confirmation evidence and cannot be snapshotted safely.",
                    "missing_prerequisite");
            }

            var sale = await _sales.GetAsync(organizationId, document.SaleId, ct)
                ?? throw Conflict(
                    "fiscal.snapshot.source_sale_missing",
                    "The confirmed source sale no longer exists.",
                    "inconsistent_state");

            var content = await _factory.CreateAsync(request, sale, ct);
            EnsureContentMatchesDocument(content, document);

            var now = DateTimeOffset.UtcNow;
            var stored = new StoredFiscalContentSnapshot(
                Guid.NewGuid(),
                organizationId,
                document.Id,
                document.FiscalizationRequestId,
                document.SaleId,
                content,
                now);
            await _snapshots.AddAsync(stored, ct);

            var actor = _actors.Current;
            var correlation = _correlations.Current;
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(),
                now,
                "FISCAL_CONTENT_SNAPSHOT_CREATED",
                actor.ActorId,
                organizationId,
                document.LocationId,
                document.TerminalId,
                "FiscalContentSnapshot",
                stored.Id.ToString(),
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                null,
                new Dictionary<string, string?>
                {
                    ["fiscalDocumentId"] = document.Id.ToString(),
                    ["fiscalizationRequestId"] = document.FiscalizationRequestId.ToString(),
                    ["saleId"] = document.SaleId.ToString(),
                    ["cfeType"] = ((int)document.CfeType).ToString(),
                    ["formatVersion"] = document.FormatVersion,
                    ["contentFingerprint"] = content.ContentFingerprint,
                    ["confirmationEvidenceFingerprint"] = request.ConfirmationEvidence.EvidenceFingerprint,
                    ["issuerCompanyVersion"] = content.Issuer.CompanyVersion.ToString(),
                    ["issuerLocationVersion"] = content.Issuer.LocationVersion.ToString(),
                    ["receiverPartyVersion"] = content.Receiver?.PartyVersion.ToString(),
                    ["lineCount"] = content.Lines.Count.ToString()
                }),
                ct);

            await _outbox.EnqueueAsync(
                new FiscalContentSnapshotCreatedIntegrationEvent(
                    Guid.NewGuid(),
                    now,
                    stored.Id,
                    document.Id,
                    document.FiscalizationRequestId,
                    document.SaleId,
                    organizationId,
                    content.ContentFingerprint,
                    request.ConfirmationEvidence.EvidenceFingerprint),
                new OutboxContext(
                    correlation.CorrelationId,
                    null,
                    organizationId,
                    actor.ActorId),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return Result(stored, false);
        }, cancellationToken);
    }

    private static void EnsureRequestMatchesDocument(
        FiscalizationRequest request,
        FiscalDocument document)
    {
        if (request.Status != FiscalizationRequestStatus.IdentityCreated
            || request.FiscalDocumentId != document.Id
            || request.Id != document.FiscalizationRequestId
            || request.SaleId != document.SaleId
            || !string.Equals(request.OrganizationId, document.OrganizationId, StringComparison.Ordinal)
            || request.CfeFamily != document.CfeType
            || request.ReceiverIdentification != document.ReceiverIdentification
            || !string.Equals(request.FormatVersion, document.FormatVersion, StringComparison.Ordinal)
            || !string.Equals(request.ConfirmationFingerprint, document.ConfirmationFingerprint, StringComparison.Ordinal)
            || !string.Equals(request.SettlementFingerprint, document.SettlementFingerprint, StringComparison.Ordinal)
            || !string.Equals(request.CurrencyCode, document.CurrencyCode, StringComparison.Ordinal)
            || request.NetAmount != document.NetAmount
            || request.VatAmount != document.VatAmount
            || request.TotalAmount != document.TotalAmount)
        {
            throw Conflict(
                "fiscal.snapshot.identity_evidence_mismatch",
                "Fiscalization work item no longer matches its immutable fiscal document identity.",
                "inconsistent_state");
        }
    }

    private static void EnsureStoredMatchesDocument(
        FiscalDocument document,
        StoredFiscalContentSnapshot stored)
    {
        if (stored.Id == Guid.Empty
            || stored.FiscalDocumentId != document.Id
            || stored.FiscalizationRequestId != document.FiscalizationRequestId
            || stored.SaleId != document.SaleId
            || !string.Equals(stored.OrganizationId, document.OrganizationId, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.snapshot.persisted_association_mismatch",
                "Persisted fiscal content snapshot association does not match the fiscal document.",
                "inconsistent_replay");
        }

        try
        {
            stored.Snapshot.EnsureIntegrity();
        }
        catch (EFactura.Domain.Common.DomainRuleException ex)
        {
            throw Conflict(ex.Code, ex.Message, "inconsistent_replay");
        }

        EnsureContentMatchesDocument(stored.Snapshot, document, "inconsistent_replay");
    }

    private static void EnsureContentMatchesDocument(
        FiscalContentSnapshot content,
        FiscalDocument document,
        string conflictType = "inconsistent_state")
    {
        if (!string.Equals(content.OrganizationId, document.OrganizationId, StringComparison.Ordinal)
            || content.SaleId != document.SaleId
            || content.CfeFamily != document.CfeType
            || content.ReceiverIdentification != document.ReceiverIdentification
            || !string.Equals(content.FormatVersion, document.FormatVersion, StringComparison.Ordinal)
            || content.EffectiveOn != document.FiscalDate
            || !string.Equals(content.ConfirmationFingerprint, document.ConfirmationFingerprint, StringComparison.Ordinal)
            || !string.Equals(content.SettlementFingerprint, document.SettlementFingerprint, StringComparison.Ordinal)
            || !string.Equals(content.FiscalEvidence.CurrencyCode, document.CurrencyCode, StringComparison.Ordinal)
            || content.FiscalEvidence.Totals.NetAmount != document.NetAmount
            || content.FiscalEvidence.Totals.VatAmount != document.VatAmount
            || content.FiscalEvidence.Totals.TotalAmount != document.TotalAmount
            || !string.Equals(content.Issuer.LocationId, document.LocationId, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.snapshot.document_summary_mismatch",
                "Fiscal content snapshot does not match the immutable fiscal document summary.",
                conflictType);
        }
    }

    private static FiscalContentSnapshotResult Result(
        StoredFiscalContentSnapshot stored,
        bool replayed) =>
        new(
            stored.Id,
            stored.FiscalDocumentId,
            stored.FiscalizationRequestId,
            stored.SaleId,
            stored.Snapshot.ContentFingerprint,
            stored.CreatedAtUtc,
            replayed);

    private static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException Conflict(
        string code,
        string message,
        string conflictType) =>
        new(
            ApplicationProblemKind.Conflict,
            code,
            message,
            conflictType: conflictType);
}
