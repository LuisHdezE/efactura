using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalDailyReportSigningEvidenceRepository : IFiscalDailyReportSigningEvidenceRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalDailyReportSigningEvidenceRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<StoredFiscalDailyReportSigningEvidence?> GetByIdentityAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default)
    {
        var date = summaryDate.ToDateTime(TimeOnly.MinValue);
        var record = await _dbContext.Set<V1FiscalDailyReportSigningEvidenceRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId
                    && x.IssuerRuc == issuerRuc
                    && x.SummaryDate == date
                    && x.Sequence == sequence,
                cancellationToken);

        return record is null ? null : new StoredFiscalDailyReportSigningEvidence(
            record.Id,
            record.OrganizationId,
            record.IssuerRuc,
            DateOnly.FromDateTime(record.SummaryDate),
            record.Sequence,
            record.FunctionalFormatVersion,
            record.ProjectionFingerprint,
            record.UnsignedContentHash,
            record.SigningTimestamp);
    }

    public Task AddAsync(
        StoredFiscalDailyReportSigningEvidence evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        _dbContext.Set<V1FiscalDailyReportSigningEvidenceRecord>().Add(new V1FiscalDailyReportSigningEvidenceRecord
        {
            Id = evidence.Id,
            OrganizationId = evidence.OrganizationId,
            IssuerRuc = evidence.IssuerRuc,
            SummaryDate = evidence.SummaryDate.ToDateTime(TimeOnly.MinValue),
            Sequence = evidence.Sequence,
            FunctionalFormatVersion = evidence.FunctionalFormatVersion,
            ProjectionFingerprint = evidence.ProjectionFingerprint,
            UnsignedContentHash = evidence.UnsignedContentHash,
            SigningTimestamp = evidence.SigningTimestamp
        });
        return Task.CompletedTask;
    }
}

public sealed class EfFiscalDailyReportSignedArtifactRepository : IFiscalDailyReportSignedArtifactRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalDailyReportSignedArtifactRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default)
    {
        var date = summaryDate.ToDateTime(TimeOnly.MinValue);
        var record = await _dbContext.Set<V1FiscalDailyReportSignedArtifactRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId
                    && x.IssuerRuc == issuerRuc
                    && x.SummaryDate == date
                    && x.Sequence == sequence,
                cancellationToken);

        return record is null ? null : new StoredFiscalDailyReportSignedArtifact(
            record.Id,
            record.SigningEvidenceId,
            record.OrganizationId,
            record.IssuerRuc,
            DateOnly.FromDateTime(record.SummaryDate),
            record.Sequence,
            record.FunctionalFormatVersion,
            record.ProjectionFingerprint,
            record.UnsignedContentHash,
            record.SignedContentHash,
            record.SigningTimestamp,
            record.SignatureProfileId,
            record.CertificateThumbprint,
            record.CertificateSerialNumber,
            record.SchemaSetId,
            record.SchemaFunctionalFormatVersion,
            record.SchemaArchiveVersion,
            record.SchemaSetFingerprint,
            record.SignedXml);
    }

    public Task AddAsync(
        StoredFiscalDailyReportSignedArtifact artifact,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _dbContext.Set<V1FiscalDailyReportSignedArtifactRecord>().Add(new V1FiscalDailyReportSignedArtifactRecord
        {
            Id = artifact.Id,
            SigningEvidenceId = artifact.SigningEvidenceId,
            OrganizationId = artifact.OrganizationId,
            IssuerRuc = artifact.IssuerRuc,
            SummaryDate = artifact.SummaryDate.ToDateTime(TimeOnly.MinValue),
            Sequence = artifact.Sequence,
            FunctionalFormatVersion = artifact.FunctionalFormatVersion,
            ProjectionFingerprint = artifact.ProjectionFingerprint,
            UnsignedContentHash = artifact.UnsignedContentHash,
            SignedContentHash = artifact.SignedContentHash,
            SigningTimestamp = artifact.SigningTimestamp,
            SignatureProfileId = artifact.SignatureProfileId,
            CertificateThumbprint = artifact.CertificateThumbprint,
            CertificateSerialNumber = artifact.CertificateSerialNumber,
            SchemaSetId = artifact.SchemaSetId,
            SchemaFunctionalFormatVersion = artifact.SchemaFunctionalFormatVersion,
            SchemaArchiveVersion = artifact.SchemaArchiveVersion,
            SchemaSetFingerprint = artifact.SchemaSetFingerprint,
            SignedXml = artifact.SignedXml
        });
        return Task.CompletedTask;
    }
}
