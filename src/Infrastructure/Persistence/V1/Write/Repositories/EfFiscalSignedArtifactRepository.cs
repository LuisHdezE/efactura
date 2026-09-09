using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalSignedArtifactRepository : IFiscalSignedArtifactRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalSignedArtifactRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<StoredFiscalSignedArtifact?> GetByFiscalDocumentAsync(
        string organizationId,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalSignedArtifactRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.FiscalDocumentId == fiscalDocumentId,
                cancellationToken);

        return record is null ? null : new StoredFiscalSignedArtifact(
            record.Id,
            record.OrganizationId,
            record.FiscalDocumentId,
            record.SigningEvidenceId,
            record.FiscalContentFingerprint,
            record.UnsignedContentHash,
            record.SigningPayloadHash,
            record.SignedContentHash,
            record.SigningTimestamp,
            record.SignatureProfileId,
            record.CertificateThumbprint,
            record.CertificateSerialNumber,
            record.SignedXml);
    }

    public Task AddAsync(
        StoredFiscalSignedArtifact artifact,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _dbContext.Set<V1FiscalSignedArtifactRecord>().Add(new V1FiscalSignedArtifactRecord
        {
            Id = artifact.Id,
            OrganizationId = artifact.OrganizationId,
            FiscalDocumentId = artifact.FiscalDocumentId,
            SigningEvidenceId = artifact.SigningEvidenceId,
            FiscalContentFingerprint = artifact.FiscalContentFingerprint,
            UnsignedContentHash = artifact.UnsignedContentHash,
            SigningPayloadHash = artifact.SigningPayloadHash,
            SignedContentHash = artifact.SignedContentHash,
            SigningTimestamp = artifact.SigningTimestamp,
            SignatureProfileId = artifact.SignatureProfileId,
            CertificateThumbprint = artifact.CertificateThumbprint,
            CertificateSerialNumber = artifact.CertificateSerialNumber,
            SignedXml = artifact.SignedXml
        });
        return Task.CompletedTask;
    }
}
