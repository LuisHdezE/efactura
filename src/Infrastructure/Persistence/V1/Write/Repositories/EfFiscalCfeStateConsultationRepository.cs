using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalCfeStateConsultationRepository : IFiscalCfeStateConsultationRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalCfeStateConsultationRepository(V1PersistenceDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<StoredFiscalCfeStateConsultation?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalCfeStateConsultationRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.OperationId == operationId,
                cancellationToken);
        return Map(record);
    }

    public Task AddAsync(
        StoredFiscalCfeStateConsultation consultation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consultation);
        _dbContext.Set<V1FiscalCfeStateConsultationRecord>().Add(ToRecord(consultation));
        return Task.CompletedTask;
    }

    private static V1FiscalCfeStateConsultationRecord ToRecord(StoredFiscalCfeStateConsultation value) => new()
    {
        Id = value.Id,
        FiscalDocumentId = value.FiscalDocumentId,
        OrganizationId = value.OrganizationId,
        CfeType = (int)value.CfeType,
        Series = value.Series,
        Number = value.Number,
        OperationId = value.OperationId,
        StateCode = value.StateCode,
        DgiSenderId = value.DgiSenderId,
        DgiReceiverId = value.DgiReceiverId,
        ConsultationToken = value.ConsultationToken,
        ConsultationAvailableAtText = value.ConsultationAvailableAtText,
        ResponseXml = value.ResponseXml,
        ResponseSha256 = value.ResponseSha256,
        ConsultedAtUtc = value.ConsultedAtUtc
    };

    private static StoredFiscalCfeStateConsultation? Map(V1FiscalCfeStateConsultationRecord? value) =>
        value is null ? null : new StoredFiscalCfeStateConsultation(
            value.Id,
            value.FiscalDocumentId,
            value.OrganizationId,
            (CfeFamily)value.CfeType,
            value.Series,
            value.Number,
            value.OperationId,
            value.StateCode,
            value.DgiSenderId,
            value.DgiReceiverId,
            value.ConsultationToken,
            value.ConsultationAvailableAtText,
            value.ResponseXml,
            value.ResponseSha256,
            value.ConsultedAtUtc);
}
