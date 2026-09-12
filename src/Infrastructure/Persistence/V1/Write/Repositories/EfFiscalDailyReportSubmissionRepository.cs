using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalDailyReportSubmissionRepository : IFiscalDailyReportSubmissionRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalDailyReportSubmissionRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<StoredFiscalDailyReportSubmission?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalDailyReportSubmissionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.OperationId == operationId,
                cancellationToken);
        return Map(record);
    }

    public async Task<StoredFiscalDailyReportSubmission?> GetByIdentityAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default)
    {
        var date = summaryDate.ToDateTime(TimeOnly.MinValue);
        var records = _dbContext.Set<V1FiscalDailyReportSubmissionRecord>();

        V1FiscalDailyReportSubmissionRecord? record;
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            var provider = _dbContext.Database.ProviderName ?? string.Empty;
            if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                record = await records
                    .FromSqlInterpolated($"SELECT * FROM \"v1_fiscal_daily_report_submissions\" WHERE \"OrganizationId\" = {organizationId} AND \"IssuerRuc\" = {issuerRuc} AND \"SummaryDate\" = {summaryDate} AND \"Sequence\" = {sequence} FOR UPDATE")
                    .AsNoTracking()
                    .SingleOrDefaultAsync(cancellationToken);
            }
            else if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                record = await records
                    .FromSqlInterpolated($"SELECT * FROM `v1_fiscal_daily_report_submissions` WHERE `OrganizationId` = {organizationId} AND `IssuerRuc` = {issuerRuc} AND `SummaryDate` = {date} AND `Sequence` = {sequence} FOR UPDATE")
                    .AsNoTracking()
                    .SingleOrDefaultAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported provider for Daily Report submission lock: {provider}");
            }
        }
        else
        {
            record = await records
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.OrganizationId == organizationId
                        && x.IssuerRuc == issuerRuc
                        && x.SummaryDate == date
                        && x.Sequence == sequence,
                    cancellationToken);
        }

        return Map(record);
    }

    public Task AddAsync(
        StoredFiscalDailyReportSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        _dbContext.Set<V1FiscalDailyReportSubmissionRecord>().Add(ToRecord(submission));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        StoredFiscalDailyReportSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        var record = await _dbContext.Set<V1FiscalDailyReportSubmissionRecord>()
            .SingleOrDefaultAsync(x => x.Id == submission.Id, cancellationToken)
            ?? throw new InvalidOperationException("Fiscal Daily Report submission record no longer exists.");

        if (!string.Equals(record.OrganizationId, submission.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(record.IssuerRuc, submission.IssuerRuc, StringComparison.Ordinal)
            || record.SummaryDate != submission.SummaryDate.ToDateTime(TimeOnly.MinValue)
            || record.Sequence != submission.Sequence
            || record.SignedArtifactId != submission.SignedArtifactId
            || !string.Equals(record.OperationId, submission.OperationId, StringComparison.Ordinal)
            || !string.Equals(record.SignedContentHash, submission.SignedContentHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Immutable Fiscal Daily Report submission identity changed during update.");
        }

        record.State = (int)submission.State;
        record.AttemptCount = submission.AttemptCount;
        record.LastAttemptAtUtc = submission.LastAttemptAtUtc;
        record.CompletedAtUtc = submission.CompletedAtUtc;
        record.DgiReceiverId = submission.DgiReceiverId;
        record.AckStateCode = submission.AckStateCode;
        record.AckXml = submission.AckXml;
        record.FailureCode = submission.FailureCode;
    }

    private static V1FiscalDailyReportSubmissionRecord ToRecord(StoredFiscalDailyReportSubmission value) =>
        new()
        {
            Id = value.Id,
            SignedArtifactId = value.SignedArtifactId,
            OrganizationId = value.OrganizationId,
            IssuerRuc = value.IssuerRuc,
            SummaryDate = value.SummaryDate.ToDateTime(TimeOnly.MinValue),
            Sequence = value.Sequence,
            OperationId = value.OperationId,
            SignedContentHash = value.SignedContentHash,
            State = (int)value.State,
            AttemptCount = value.AttemptCount,
            PreparedAtUtc = value.PreparedAtUtc,
            LastAttemptAtUtc = value.LastAttemptAtUtc,
            CompletedAtUtc = value.CompletedAtUtc,
            DgiReceiverId = value.DgiReceiverId,
            AckStateCode = value.AckStateCode,
            AckXml = value.AckXml,
            FailureCode = value.FailureCode
        };

    private static StoredFiscalDailyReportSubmission? Map(V1FiscalDailyReportSubmissionRecord? value) =>
        value is null ? null : new StoredFiscalDailyReportSubmission(
            value.Id,
            value.SignedArtifactId,
            value.OrganizationId,
            value.IssuerRuc,
            DateOnly.FromDateTime(value.SummaryDate),
            value.Sequence,
            value.OperationId,
            value.SignedContentHash,
            (FiscalDailyReportSubmissionState)value.State,
            value.AttemptCount,
            value.PreparedAtUtc,
            value.LastAttemptAtUtc,
            value.CompletedAtUtc,
            value.DgiReceiverId,
            value.AckStateCode,
            value.AckXml,
            value.FailureCode);
}
