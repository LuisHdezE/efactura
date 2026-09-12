using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalDailyReportBrCorrectionRepository :
    IFiscalDailyReportBrCorrectionRepository,
    IFiscalDailyReportSameSequenceReceiptReader
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalDailyReportBrCorrectionRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<StoredFiscalDailyReportBrCorrectionRevision?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.OperationId == operationId,
                cancellationToken);
        return Map(record);
    }

    public async Task<StoredFiscalDailyReportBrCorrectionRevision?> GetLatestAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default)
    {
        var date = summaryDate.ToDateTime(TimeOnly.MinValue);
        var record = await _dbContext.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId
                && x.IssuerRuc == issuerRuc
                && x.SummaryDate == date
                && x.Sequence == sequence)
            .OrderByDescending(x => x.LocalRevision)
            .FirstOrDefaultAsync(cancellationToken);
        return Map(record);
    }

    public async Task<StoredFiscalDailyReportBrCorrectionRevision?> GetByRevisionAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        int localRevision,
        CancellationToken cancellationToken = default)
    {
        var date = summaryDate.ToDateTime(TimeOnly.MinValue);
        var record = await _dbContext.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId
                && x.IssuerRuc == issuerRuc
                && x.SummaryDate == date
                && x.Sequence == sequence
                && x.LocalRevision == localRevision,
                cancellationToken);
        return Map(record);
    }

    public Task AddAsync(
        StoredFiscalDailyReportBrCorrectionRevision revision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(revision);
        revision.EnsureIntegrity();
        _dbContext.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>().Add(ToRecord(revision));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        StoredFiscalDailyReportBrCorrectionRevision revision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(revision);
        revision.EnsureIntegrity();
        var record = await _dbContext.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .SingleOrDefaultAsync(x => x.Id == revision.Id, cancellationToken)
            ?? throw new InvalidOperationException("Fiscal Daily Report BR correction revision no longer exists.");

        var original = Map(record)!;
        original.EnsureIntegrity();
        if (original.RootSubmissionId != revision.RootSubmissionId
            || original.RootSignedArtifactId != revision.RootSignedArtifactId
            || original.PreviousRevisionId != revision.PreviousRevisionId
            || original.SigningEvidenceId != revision.SigningEvidenceId
            || original.SignedArtifactId != revision.SignedArtifactId
            || !string.Equals(original.OrganizationId, revision.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(original.IssuerRuc, revision.IssuerRuc, StringComparison.Ordinal)
            || original.SummaryDate != revision.SummaryDate
            || original.Sequence != revision.Sequence
            || original.LocalRevision != revision.LocalRevision
            || !string.Equals(original.OperationId, revision.OperationId, StringComparison.Ordinal)
            || !string.Equals(original.RevisionFingerprint, revision.RevisionFingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Immutable Fiscal Daily Report BR correction identity changed during update.");
        }

        record.State = (int)revision.State;
        record.AttemptCount = revision.AttemptCount;
        record.LastAttemptAtUtc = revision.LastAttemptAtUtc;
        record.CompletedAtUtc = revision.CompletedAtUtc;
        record.DgiReceiverId = revision.DgiReceiverId;
        record.AckStateCode = revision.AckStateCode;
        record.AckXml = revision.AckXml;
        record.AckReasonsJson = revision.AckReasonsJson;
        record.FailureCode = revision.FailureCode;
    }

    public Task<bool> HasReceivedCorrectionAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default)
    {
        var date = summaryDate.ToDateTime(TimeOnly.MinValue);
        return _dbContext.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .AsNoTracking()
            .AnyAsync(x => x.OrganizationId == organizationId
                && x.IssuerRuc == issuerRuc
                && x.SummaryDate == date
                && x.Sequence == sequence
                && x.State == (int)FiscalDailyReportSubmissionState.Received
                && x.AckStateCode == "AR",
                cancellationToken);
    }

    private static V1FiscalDailyReportBrCorrectionRevisionRecord ToRecord(
        StoredFiscalDailyReportBrCorrectionRevision value) => new()
    {
        Id = value.Id,
        RootSubmissionId = value.RootSubmissionId,
        RootSignedArtifactId = value.RootSignedArtifactId,
        PreviousRevisionId = value.PreviousRevisionId,
        SigningEvidenceId = value.SigningEvidenceId,
        SignedArtifactId = value.SignedArtifactId,
        OrganizationId = value.OrganizationId,
        IssuerRuc = value.IssuerRuc,
        SummaryDate = value.SummaryDate.ToDateTime(TimeOnly.MinValue),
        Sequence = value.Sequence,
        LocalRevision = value.LocalRevision,
        OperationId = value.OperationId,
        CorrectionReasonCode = value.CorrectionReasonCode,
        SourceAckXmlHash = value.SourceAckXmlHash,
        SourceAckReasonsJson = value.SourceAckReasonsJson,
        FunctionalFormatVersion = value.FunctionalFormatVersion,
        ProjectionFingerprint = value.ProjectionFingerprint,
        UnsignedContentHash = value.UnsignedContentHash,
        SignedContentHash = value.SignedContentHash,
        SigningTimestamp = value.SigningTimestamp.ToUniversalTime(),
        SigningOffsetMinutes = FiscalDailyReportSigningTimestampPersistence.OffsetMinutes(value.SigningTimestamp),
        SignatureProfileId = value.SignatureProfileId,
        CertificateThumbprint = value.CertificateThumbprint,
        CertificateSerialNumber = value.CertificateSerialNumber,
        SchemaSetId = value.SchemaSetId,
        SchemaFunctionalFormatVersion = value.SchemaFunctionalFormatVersion,
        SchemaArchiveVersion = value.SchemaArchiveVersion,
        SchemaSetFingerprint = value.SchemaSetFingerprint,
        SignedXml = value.SignedXml,
        CreatedAtUtc = value.CreatedAtUtc,
        RevisionFingerprint = value.RevisionFingerprint,
        State = (int)value.State,
        AttemptCount = value.AttemptCount,
        PreparedAtUtc = value.PreparedAtUtc,
        LastAttemptAtUtc = value.LastAttemptAtUtc,
        CompletedAtUtc = value.CompletedAtUtc,
        DgiReceiverId = value.DgiReceiverId,
        AckStateCode = value.AckStateCode,
        AckXml = value.AckXml,
        AckReasonsJson = value.AckReasonsJson,
        FailureCode = value.FailureCode
    };

    private static StoredFiscalDailyReportBrCorrectionRevision? Map(
        V1FiscalDailyReportBrCorrectionRevisionRecord? value) =>
        value is null ? null : new StoredFiscalDailyReportBrCorrectionRevision(
            value.Id,
            value.RootSubmissionId,
            value.RootSignedArtifactId,
            value.PreviousRevisionId,
            value.SigningEvidenceId,
            value.SignedArtifactId,
            value.OrganizationId,
            value.IssuerRuc,
            DateOnly.FromDateTime(value.SummaryDate),
            value.Sequence,
            value.LocalRevision,
            value.OperationId,
            value.CorrectionReasonCode,
            value.SourceAckXmlHash,
            value.SourceAckReasonsJson,
            value.FunctionalFormatVersion,
            value.ProjectionFingerprint,
            value.UnsignedContentHash,
            value.SignedContentHash,
            FiscalDailyReportSigningTimestampPersistence.Rehydrate(value.SigningTimestamp, value.SigningOffsetMinutes),
            value.SignatureProfileId,
            value.CertificateThumbprint,
            value.CertificateSerialNumber,
            value.SchemaSetId,
            value.SchemaFunctionalFormatVersion,
            value.SchemaArchiveVersion,
            value.SchemaSetFingerprint,
            value.SignedXml,
            value.CreatedAtUtc,
            value.RevisionFingerprint,
            (FiscalDailyReportSubmissionState)value.State,
            value.AttemptCount,
            value.PreparedAtUtc,
            value.LastAttemptAtUtc,
            value.CompletedAtUtc,
            value.DgiReceiverId,
            value.AckStateCode,
            value.AckXml,
            value.AckReasonsJson,
            value.FailureCode);
}
