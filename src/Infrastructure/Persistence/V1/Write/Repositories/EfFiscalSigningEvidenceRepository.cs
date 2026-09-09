using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalSigningEvidenceRepository : IFiscalSigningEvidenceRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalSigningEvidenceRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<FiscalSigningEvidence?> GetByFiscalDocumentAsync(
        string organizationId,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalSigningEvidenceRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.FiscalDocumentId == fiscalDocumentId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public Task AddAsync(
        FiscalSigningEvidence evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        _dbContext.Set<V1FiscalSigningEvidenceRecord>().Add(new V1FiscalSigningEvidenceRecord
        {
            Id = evidence.Id,
            OrganizationId = evidence.OrganizationId,
            FiscalDocumentId = evidence.FiscalDocumentId,
            FiscalContentFingerprint = evidence.FiscalContentFingerprint,
            UnsignedContentHash = evidence.UnsignedContentHash,
            SigningTimestamp = evidence.SigningTimestamp
        });
        return Task.CompletedTask;
    }

    private static FiscalSigningEvidence Map(V1FiscalSigningEvidenceRecord record) =>
        FiscalSigningEvidence.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.FiscalDocumentId,
            record.FiscalContentFingerprint,
            record.UnsignedContentHash,
            record.SigningTimestamp);
}
