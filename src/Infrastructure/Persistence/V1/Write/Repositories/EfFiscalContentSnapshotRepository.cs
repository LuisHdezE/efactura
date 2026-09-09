using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalContentSnapshotRepository : IFiscalContentSnapshotRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalContentSnapshotRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<StoredFiscalContentSnapshot?> GetByFiscalDocumentAsync(
        string organizationId,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalContentSnapshotRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.FiscalDocumentId == fiscalDocumentId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public Task AddAsync(
        StoredFiscalContentSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Id == Guid.Empty
            || snapshot.FiscalDocumentId == Guid.Empty
            || snapshot.FiscalizationRequestId == Guid.Empty
            || snapshot.SaleId == Guid.Empty
            || string.IsNullOrWhiteSpace(snapshot.OrganizationId))
        {
            throw Inconsistent("Fiscal content snapshot persistence association is incomplete.");
        }

        snapshot.Snapshot.EnsureIntegrity();
        if (!string.Equals(snapshot.Snapshot.OrganizationId, snapshot.OrganizationId, StringComparison.Ordinal)
            || snapshot.Snapshot.SaleId != snapshot.SaleId)
        {
            throw Inconsistent("Fiscal content snapshot persistence association does not match the immutable content.");
        }

        _dbContext.Set<V1FiscalContentSnapshotRecord>().Add(new V1FiscalContentSnapshotRecord
        {
            Id = snapshot.Id,
            OrganizationId = snapshot.OrganizationId,
            FiscalDocumentId = snapshot.FiscalDocumentId,
            FiscalizationRequestId = snapshot.FiscalizationRequestId,
            SaleId = snapshot.SaleId,
            ContentFingerprint = snapshot.Snapshot.ContentFingerprint,
            SnapshotJson = FiscalSnapshotJson.SerializeContent(snapshot.Snapshot),
            CreatedAtUtc = snapshot.CreatedAtUtc
        });
        return Task.CompletedTask;
    }

    private static StoredFiscalContentSnapshot Map(V1FiscalContentSnapshotRecord record)
    {
        var snapshot = FiscalSnapshotJson.DeserializeContent(
            record.SnapshotJson,
            record.ContentFingerprint);
        if (!string.Equals(snapshot.OrganizationId, record.OrganizationId, StringComparison.Ordinal)
            || snapshot.SaleId != record.SaleId)
        {
            throw Inconsistent("Persisted fiscal content snapshot association does not match its immutable payload.");
        }

        return new StoredFiscalContentSnapshot(
            record.Id,
            record.OrganizationId,
            record.FiscalDocumentId,
            record.FiscalizationRequestId,
            record.SaleId,
            snapshot,
            record.CreatedAtUtc);
    }

    private static ApplicationProblemException Inconsistent(string message) =>
        new(
            ApplicationProblemKind.Conflict,
            "fiscal.snapshot.persisted_evidence_invalid",
            message,
            conflictType: "inconsistent_state");
}
