using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalDailyReportReceiverDiscoveryRepository :
    IFiscalDailyReportReceiverDiscoveryRepository,
    IFiscalDailyReportReceiverDiscoveryTargetReader
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalDailyReportReceiverDiscoveryRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoredFiscalDailyReportReceiverDiscovery?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalDailyReportReceiverDiscoveryRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.OperationId == operationId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<StoredFiscalDailyReportReceiverDiscovery?> GetByTargetAsync(
        string organizationId,
        FiscalDailyReportConsultationTargetKind targetKind,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        var records = _dbContext.Set<V1FiscalDailyReportReceiverDiscoveryRecord>().AsNoTracking();
        V1FiscalDailyReportReceiverDiscoveryRecord? record = targetKind switch
        {
            FiscalDailyReportConsultationTargetKind.RootSubmission =>
                await records.SingleOrDefaultAsync(
                    x => x.OrganizationId == organizationId && x.RootSubmissionId == targetId,
                    cancellationToken),
            FiscalDailyReportConsultationTargetKind.BrCorrectionRevision =>
                await records.SingleOrDefaultAsync(
                    x => x.OrganizationId == organizationId && x.BrCorrectionRevisionId == targetId,
                    cancellationToken),
            _ => null
        };
        return record is null ? null : Map(record);
    }

    public async Task AddAsync(
        StoredFiscalDailyReportReceiverDiscovery discovery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(discovery);
        if (discovery.RootSubmissionId.HasValue == discovery.BrCorrectionRevisionId.HasValue)
            throw new InvalidOperationException("Receiver discovery evidence must reference exactly one local Reporte Diario target.");

        await _dbContext.Set<V1FiscalDailyReportReceiverDiscoveryRecord>().AddAsync(
            new V1FiscalDailyReportReceiverDiscoveryRecord
            {
                Id = discovery.Id,
                RootSubmissionId = discovery.RootSubmissionId,
                BrCorrectionRevisionId = discovery.BrCorrectionRevisionId,
                OrganizationId = discovery.OrganizationId,
                IssuerRuc = discovery.IssuerRuc,
                SummaryDate = discovery.SummaryDate.ToDateTime(TimeOnly.MinValue),
                Sequence = discovery.Sequence,
                LocalRevision = discovery.LocalRevision,
                OperationId = discovery.OperationId,
                DgiEmitterId = discovery.DgiEmitterId,
                DgiReceiverId = discovery.DgiReceiverId,
                DgiStateCode = discovery.DgiStateCode,
                DgiReceptionTimestampText = discovery.DgiReceptionTimestampText,
                EvidenceXml = discovery.EvidenceXml,
                EvidenceXmlHash = discovery.EvidenceXmlHash,
                DiscoveredAtUtc = discovery.DiscoveredAtUtc
            },
            cancellationToken);
    }

    public async Task<FiscalDailyReportReceiverDiscoveryTarget?> GetTargetAsync(
        string organizationId,
        FiscalDailyReportConsultationTargetKind targetKind,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        if (targetKind == FiscalDailyReportConsultationTargetKind.RootSubmission)
        {
            var root = await _dbContext.Set<V1FiscalDailyReportSubmissionRecord>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.OrganizationId == organizationId && x.Id == targetId,
                    cancellationToken);
            return root is null ? null : new FiscalDailyReportReceiverDiscoveryTarget(
                FiscalDailyReportConsultationTargetKind.RootSubmission,
                root.Id,
                root.OrganizationId,
                root.IssuerRuc,
                DateOnly.FromDateTime(root.SummaryDate),
                root.Sequence,
                null,
                (FiscalDailyReportSubmissionState)root.State,
                root.DgiReceiverId);
        }

        if (targetKind == FiscalDailyReportConsultationTargetKind.BrCorrectionRevision)
        {
            var revision = await _dbContext.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.OrganizationId == organizationId && x.Id == targetId,
                    cancellationToken);
            return revision is null ? null : new FiscalDailyReportReceiverDiscoveryTarget(
                FiscalDailyReportConsultationTargetKind.BrCorrectionRevision,
                revision.Id,
                revision.OrganizationId,
                revision.IssuerRuc,
                DateOnly.FromDateTime(revision.SummaryDate),
                revision.Sequence,
                revision.LocalRevision,
                (FiscalDailyReportSubmissionState)revision.State,
                revision.DgiReceiverId);
        }

        return null;
    }

    public async Task<IReadOnlyCollection<string>> GetKnownReceiverIdsAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default)
    {
        var date = summaryDate.ToDateTime(TimeOnly.MinValue);

        var rootIds = await _dbContext.Set<V1FiscalDailyReportSubmissionRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId
                && x.IssuerRuc == issuerRuc
                && x.SummaryDate == date
                && x.Sequence == sequence
                && x.DgiReceiverId != null)
            .Select(x => x.DgiReceiverId!)
            .ToListAsync(cancellationToken);

        var revisionIds = await _dbContext.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId
                && x.IssuerRuc == issuerRuc
                && x.SummaryDate == date
                && x.Sequence == sequence
                && x.DgiReceiverId != null)
            .Select(x => x.DgiReceiverId!)
            .ToListAsync(cancellationToken);

        var discoveredIds = await _dbContext.Set<V1FiscalDailyReportReceiverDiscoveryRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId
                && x.IssuerRuc == issuerRuc
                && x.SummaryDate == date
                && x.Sequence == sequence)
            .Select(x => x.DgiReceiverId)
            .ToListAsync(cancellationToken);

        return rootIds
            .Concat(revisionIds)
            .Concat(discoveredIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static StoredFiscalDailyReportReceiverDiscovery Map(
        V1FiscalDailyReportReceiverDiscoveryRecord value) =>
        new(
            value.Id,
            value.RootSubmissionId,
            value.BrCorrectionRevisionId,
            value.OrganizationId,
            value.IssuerRuc,
            DateOnly.FromDateTime(value.SummaryDate),
            value.Sequence,
            value.LocalRevision,
            value.OperationId,
            value.DgiEmitterId,
            value.DgiReceiverId,
            value.DgiStateCode,
            value.DgiReceptionTimestampText,
            value.EvidenceXml,
            value.EvidenceXmlHash,
            value.DiscoveredAtUtc);
}
