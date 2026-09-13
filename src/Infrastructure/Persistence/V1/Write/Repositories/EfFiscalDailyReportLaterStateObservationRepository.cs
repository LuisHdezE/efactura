using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

/// <summary>
/// Append-only EF Core persistence for DGI Reporte Diario later-state observations.
/// SaveChanges and transaction boundaries remain owned by the application Unit of Work.
/// </summary>
public sealed class EfFiscalDailyReportLaterStateObservationRepository :
    IFiscalDailyReportLaterStateObservationRepository,
    IFiscalDailyReportLatestObservationReader
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalDailyReportLaterStateObservationRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoredFiscalDailyReportLaterStateObservation?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalDailyReportLaterStateObservationRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.OperationId == operationId,
                cancellationToken);

        return record is null ? null : Map(record);
    }

    public async Task<IReadOnlyList<StoredFiscalDailyReportLaterStateObservation>> GetLatestCandidatesByReceiverIdAsync(
        string organizationId,
        string dgiReceiverId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Set<V1FiscalDailyReportLaterStateObservationRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.DgiReceiverId == dgiReceiverId)
            .OrderByDescending(x => x.ObservedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        return records.Select(Map).ToArray();
    }

    public async Task AddAsync(
        StoredFiscalDailyReportLaterStateObservation observation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (observation.RootSubmissionId.HasValue == observation.BrCorrectionRevisionId.HasValue)
        {
            throw new InvalidOperationException(
                "Later-state observation must reference exactly one Reporte Diario target.");
        }

        await _dbContext.Set<V1FiscalDailyReportLaterStateObservationRecord>().AddAsync(
            new V1FiscalDailyReportLaterStateObservationRecord
            {
                Id = observation.Id,
                RootSubmissionId = observation.RootSubmissionId,
                BrCorrectionRevisionId = observation.BrCorrectionRevisionId,
                OrganizationId = observation.OrganizationId,
                IssuerRuc = observation.IssuerRuc,
                SummaryDate = observation.SummaryDate.ToDateTime(TimeOnly.MinValue),
                Sequence = observation.Sequence,
                LocalRevision = observation.LocalRevision,
                OperationId = observation.OperationId,
                DgiEmitterId = observation.DgiEmitterId,
                DgiReceiverId = observation.DgiReceiverId,
                State = (int)observation.State,
                DgiStateCode = observation.DgiStateCode,
                DgiReceptionTimestampText = observation.DgiReceptionTimestampText,
                EvidenceXml = observation.EvidenceXml,
                EvidenceXmlHash = observation.EvidenceXmlHash,
                ObservedAtUtc = observation.ObservedAtUtc.ToUniversalTime()
            },
            cancellationToken);
    }

    private static StoredFiscalDailyReportLaterStateObservation Map(
        V1FiscalDailyReportLaterStateObservationRecord record) =>
        new(
            record.Id,
            record.RootSubmissionId,
            record.BrCorrectionRevisionId,
            record.OrganizationId,
            record.IssuerRuc,
            DateOnly.FromDateTime(record.SummaryDate),
            record.Sequence,
            record.LocalRevision,
            record.OperationId,
            record.DgiEmitterId,
            record.DgiReceiverId,
            (FiscalDailyReportLaterState)record.State,
            record.DgiStateCode,
            record.DgiReceptionTimestampText,
            record.EvidenceXml,
            record.EvidenceXmlHash,
            record.ObservedAtUtc.ToUniversalTime());
}
