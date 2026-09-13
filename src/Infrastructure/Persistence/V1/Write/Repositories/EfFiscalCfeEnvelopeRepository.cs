using System.Text.Json;
using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalCfeEnvelopeRepository : IFiscalCfeEnvelopeRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalCfeEnvelopeRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoredFiscalCfeEnvelope?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalCfeEnvelopeRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.OperationId == operationId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<StoredFiscalCfeEnvelope?> GetByIdentityAsync(
        string organizationId,
        string issuerRuc,
        string receiverRut,
        long senderEnvelopeId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalCfeEnvelopeRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId
                    && x.IssuerRuc == issuerRuc
                    && x.ReceiverRut == receiverRut
                    && x.SenderEnvelopeId == senderEnvelopeId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public Task AddAsync(
        StoredFiscalCfeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        _dbContext.Set<V1FiscalCfeEnvelopeRecord>().Add(new V1FiscalCfeEnvelopeRecord
        {
            Id = envelope.Id,
            OrganizationId = envelope.OrganizationId,
            ReceiverRut = envelope.ReceiverRut,
            IssuerRuc = envelope.IssuerRuc,
            SenderEnvelopeId = envelope.SenderEnvelopeId,
            CreatedAtUtc = envelope.CreatedAt.ToUniversalTime(),
            CreatedAtOffsetMinutes = checked((int)envelope.CreatedAt.Offset.TotalMinutes),
            FiscalDocumentIdsJson = JsonSerializer.Serialize(envelope.FiscalDocumentIds),
            OperationId = envelope.OperationId,
            CfeCount = envelope.CfeCount,
            CertificateThumbprint = envelope.CertificateThumbprint,
            CertificateSerialNumber = envelope.CertificateSerialNumber,
            EnvelopeXml = envelope.EnvelopeXml,
            EnvelopeSha256 = envelope.EnvelopeSha256,
            SchemaSetId = envelope.SchemaSetId,
            SchemaVersion = envelope.SchemaVersion,
            SchemaSetFingerprint = envelope.SchemaSetFingerprint
        });
        return Task.CompletedTask;
    }

    private static StoredFiscalCfeEnvelope Map(V1FiscalCfeEnvelopeRecord record)
    {
        Guid[] ids;
        try
        {
            ids = JsonSerializer.Deserialize<Guid[]>(record.FiscalDocumentIdsJson)
                ?? throw new InvalidDataException("Persisted Sobre CFE identity list is null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Persisted Sobre CFE identity list is invalid JSON.", ex);
        }

        if (ids.Length != record.CfeCount
            || ids.Length is < 1 or > 250
            || ids.Any(x => x == Guid.Empty)
            || ids.Distinct().Count() != ids.Length
            || record.CreatedAtOffsetMinutes is < -840 or > 840)
        {
            throw new InvalidDataException("Persisted Sobre identity evidence is inconsistent.");
        }

        var createdAt = record.CreatedAtUtc.ToUniversalTime()
            .ToOffset(TimeSpan.FromMinutes(record.CreatedAtOffsetMinutes));

        return new StoredFiscalCfeEnvelope(
            record.Id,
            record.OrganizationId,
            record.ReceiverRut,
            record.IssuerRuc,
            record.SenderEnvelopeId,
            createdAt,
            ids,
            record.OperationId,
            record.CfeCount,
            record.CertificateThumbprint,
            record.CertificateSerialNumber,
            record.EnvelopeXml,
            record.EnvelopeSha256,
            record.SchemaSetId,
            record.SchemaVersion,
            record.SchemaSetFingerprint);
    }
}
