using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalizationRequestRepository : IFiscalizationRequestRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalizationRequestRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(FiscalizationRequest request, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<V1FiscalizationRequestRecord>().Add(MapRecord(request));
        return Task.CompletedTask;
    }

    public async Task<FiscalizationRequest?> GetAsync(
        string organizationId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalizationRequestRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == requestId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<FiscalizationRequest?> GetBySaleAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalizationRequestRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.SaleId == saleId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task SaveAsync(
        FiscalizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalizationRequestRecord>()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == request.OrganizationId && x.Id == request.Id,
                cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "fiscalization.not_found",
                "Fiscalization request was not found.");

        var priorVersion = request.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The fiscalization request changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.Status = (int)request.Status;
        record.Version = request.Version;
        record.FiscalDocumentId = request.FiscalDocumentId;
        record.IdentityCreatedAtUtc = request.IdentityCreatedAtUtc;
    }

    private static V1FiscalizationRequestRecord MapRecord(FiscalizationRequest request)
    {
        var confirmationEvidenceJson = request.ConfirmationEvidence is null
            ? null
            : FiscalSnapshotJson.SerializeConfirmation(request.ConfirmationEvidence);

        return new V1FiscalizationRequestRecord
        {
            Id = request.Id,
            OrganizationId = request.OrganizationId,
            SaleId = request.SaleId,
            LocationId = request.LocationId,
            TerminalId = request.TerminalId,
            CfeFamily = (int)request.CfeFamily,
            ReceiverIdentification = request.ReceiverIdentification.HasValue
                ? (int)request.ReceiverIdentification.Value
                : null,
            FormatVersion = request.FormatVersion,
            ConfirmationFingerprint = request.ConfirmationFingerprint,
            SettlementFingerprint = request.SettlementFingerprint,
            CurrencyCode = request.CurrencyCode,
            NetAmount = request.NetAmount,
            VatAmount = request.VatAmount,
            TotalAmount = request.TotalAmount,
            ConfirmationEvidenceFingerprint = request.ConfirmationEvidence?.EvidenceFingerprint,
            ConfirmationEvidenceJson = confirmationEvidenceJson,
            Status = (int)request.Status,
            Version = request.Version,
            RequestedAtUtc = request.RequestedAtUtc,
            FiscalDocumentId = request.FiscalDocumentId,
            IdentityCreatedAtUtc = request.IdentityCreatedAtUtc
        };
    }

    private static FiscalizationRequest Map(V1FiscalizationRequestRecord record) =>
        FiscalizationRequest.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.SaleId,
            record.LocationId,
            record.TerminalId,
            (CfeFamily)record.CfeFamily,
            record.ReceiverIdentification.HasValue
                ? (ReceiverIdentificationRequirement)record.ReceiverIdentification.Value
                : null,
            record.FormatVersion,
            record.ConfirmationFingerprint,
            record.SettlementFingerprint,
            record.CurrencyCode,
            record.NetAmount,
            record.VatAmount,
            record.TotalAmount,
            (FiscalizationRequestStatus)record.Status,
            record.Version,
            record.RequestedAtUtc,
            record.FiscalDocumentId,
            record.IdentityCreatedAtUtc,
            MapConfirmationEvidence(record));

    private static FiscalConfirmationEvidence? MapConfirmationEvidence(
        V1FiscalizationRequestRecord record)
    {
        var hasJson = !string.IsNullOrWhiteSpace(record.ConfirmationEvidenceJson);
        var hasFingerprint = !string.IsNullOrWhiteSpace(record.ConfirmationEvidenceFingerprint);
        if (!hasJson && !hasFingerprint)
            return null;

        if (!hasJson || !hasFingerprint)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "fiscal.snapshot.persisted_evidence_invalid",
                "Persisted fiscal confirmation evidence metadata is incomplete.",
                conflictType: "inconsistent_state");
        }

        return FiscalSnapshotJson.DeserializeConfirmation(
            record.ConfirmationEvidenceJson!,
            record.ConfirmationEvidenceFingerprint!);
    }
}
