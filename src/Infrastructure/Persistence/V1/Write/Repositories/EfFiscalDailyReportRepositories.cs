using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalDailyReportVersionRepository : IFiscalDailyReportVersionRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalDailyReportVersionRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> AcquireOrganizationSequenceLockAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Daily Report sequence lock requires an active transaction.");

        var provider = _dbContext.Database.ProviderName ?? string.Empty;
        IQueryable<V1CompanyFiscalProfileRecord> query;
        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            query = _dbContext.Set<V1CompanyFiscalProfileRecord>()
                .FromSqlInterpolated($"SELECT * FROM \"v1_company_fiscal_profiles\" WHERE \"OrganizationId\" = {organizationId} FOR UPDATE");
        }
        else if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        {
            query = _dbContext.Set<V1CompanyFiscalProfileRecord>()
                .FromSqlInterpolated($"SELECT * FROM `v1_company_fiscal_profiles` WHERE `OrganizationId` = {organizationId} FOR UPDATE");
        }
        else
        {
            throw new InvalidOperationException($"Unsupported provider for Daily Report sequence lock: {provider}");
        }

        var locked = await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return locked.Count == 1;
    }

    public async Task<StoredFiscalDailyReportVersion?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalDailyReportVersionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.OperationId == operationId,
                cancellationToken);
        return Map(record);
    }

    public async Task<StoredFiscalDailyReportVersion?> GetLatestAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        CancellationToken cancellationToken = default)
    {
        var date = summaryDate.ToDateTime(TimeOnly.MinValue);
        var record = await _dbContext.Set<V1FiscalDailyReportVersionRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.IssuerRuc == issuerRuc && x.SummaryDate == date)
            .OrderByDescending(x => x.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
        return Map(record);
    }

    public async Task<StoredFiscalDailyReportVersion?> GetByIdentityAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default)
    {
        var date = summaryDate.ToDateTime(TimeOnly.MinValue);
        var record = await _dbContext.Set<V1FiscalDailyReportVersionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId
                    && x.IssuerRuc == issuerRuc
                    && x.SummaryDate == date
                    && x.Sequence == sequence,
                cancellationToken);
        return Map(record);
    }

    public Task AddAsync(StoredFiscalDailyReportVersion version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);
        version.EnsureIntegrity();
        _dbContext.Set<V1FiscalDailyReportVersionRecord>().Add(new V1FiscalDailyReportVersionRecord
        {
            Id = version.Id,
            OrganizationId = version.OrganizationId,
            IssuerRuc = version.IssuerRuc,
            SummaryDate = version.SummaryDate.ToDateTime(TimeOnly.MinValue),
            Sequence = version.Sequence,
            PreviousVersionId = version.PreviousVersionId,
            OperationId = version.OperationId,
            RevisionKind = (int)version.RevisionKind,
            ReasonCode = version.ReasonCode,
            ReconciliationFingerprint = version.ReconciliationFingerprint,
            RequiresFxReliquidation = version.RequiresFxReliquidation,
            CreatedAtUtc = version.CreatedAtUtc,
            VersionFingerprint = version.VersionFingerprint
        });
        return Task.CompletedTask;
    }

    private static StoredFiscalDailyReportVersion? Map(V1FiscalDailyReportVersionRecord? record) =>
        record is null ? null : new StoredFiscalDailyReportVersion(
            record.Id,
            record.OrganizationId,
            record.IssuerRuc,
            DateOnly.FromDateTime(record.SummaryDate),
            record.Sequence,
            record.PreviousVersionId,
            record.OperationId,
            (FiscalDailyReportRevisionKind)record.RevisionKind,
            record.ReasonCode,
            record.ReconciliationFingerprint,
            record.RequiresFxReliquidation,
            record.CreatedAtUtc,
            record.VersionFingerprint);
}

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
            FiscalDailyReportSigningTimestampPersistence.Rehydrate(record.SigningTimestamp, record.SigningOffsetMinutes));
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
            SigningTimestamp = evidence.SigningTimestamp.ToUniversalTime(),
            SigningOffsetMinutes = FiscalDailyReportSigningTimestampPersistence.OffsetMinutes(evidence.SigningTimestamp)
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
            FiscalDailyReportSigningTimestampPersistence.Rehydrate(record.SigningTimestamp, record.SigningOffsetMinutes),
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
            SigningTimestamp = artifact.SigningTimestamp.ToUniversalTime(),
            SigningOffsetMinutes = FiscalDailyReportSigningTimestampPersistence.OffsetMinutes(artifact.SigningTimestamp),
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

internal static class FiscalDailyReportSigningTimestampPersistence
{
    private const int MinimumOffsetMinutes = -14 * 60;
    private const int MaximumOffsetMinutes = 14 * 60;

    public static int OffsetMinutes(DateTimeOffset value)
    {
        var minutes = checked((int)value.Offset.TotalMinutes);
        if (minutes is < MinimumOffsetMinutes or > MaximumOffsetMinutes)
            throw new InvalidOperationException("Fiscal Daily Report signing offset is outside DateTimeOffset bounds.");
        return minutes;
    }

    public static DateTimeOffset Rehydrate(DateTimeOffset persistedUtc, int offsetMinutes)
    {
        if (offsetMinutes is < MinimumOffsetMinutes or > MaximumOffsetMinutes)
            throw new InvalidOperationException("Persisted Fiscal Daily Report signing offset is invalid.");

        return persistedUtc.ToUniversalTime().ToOffset(TimeSpan.FromMinutes(offsetMinutes));
    }
}
