using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalDocumentRepository : IFiscalDocumentRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalDocumentRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(FiscalDocument document, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<V1FiscalDocumentRecord>().Add(MapRecord(document));
        return Task.CompletedTask;
    }

    public async Task<FiscalDocument?> GetAsync(
        string organizationId,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalDocumentRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == fiscalDocumentId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<FiscalDocument?> GetByFiscalizationRequestAsync(
        string organizationId,
        Guid fiscalizationRequestId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalDocumentRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId
                     && x.FiscalizationRequestId == fiscalizationRequestId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    private static V1FiscalDocumentRecord MapRecord(FiscalDocument document) => new()
    {
        Id = document.Id,
        OrganizationId = document.OrganizationId,
        FiscalizationRequestId = document.FiscalizationRequestId,
        SaleId = document.SaleId,
        FiscalNumberReservationId = document.FiscalNumberReservationId,
        CaeAuthorizationId = document.CaeAuthorizationId,
        CaeAllocationId = document.CaeAllocationId,
        CfeType = (int)document.CfeType,
        Series = document.Series,
        Number = document.Number,
        CaeAuthorizationNumber = document.CaeAuthorizationNumber,
        CaeRangeFrom = document.CaeRangeFrom,
        CaeRangeTo = document.CaeRangeTo,
        CaeValidFrom = document.CaeValidFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        CaeValidTo = document.CaeValidTo.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        FiscalDate = document.FiscalDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        LocationId = document.LocationId,
        TerminalId = document.TerminalId,
        ReceiverIdentification = document.ReceiverIdentification.HasValue
            ? (int)document.ReceiverIdentification.Value
            : null,
        FormatVersion = document.FormatVersion,
        ConfirmationFingerprint = document.ConfirmationFingerprint,
        SettlementFingerprint = document.SettlementFingerprint,
        CurrencyCode = document.CurrencyCode,
        NetAmount = document.NetAmount,
        VatAmount = document.VatAmount,
        TotalAmount = document.TotalAmount,
        Status = (int)document.Status,
        IdentityCreatedAtUtc = document.IdentityCreatedAtUtc
    };

    private static FiscalDocument Map(V1FiscalDocumentRecord record) =>
        FiscalDocument.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.FiscalizationRequestId,
            record.SaleId,
            record.FiscalNumberReservationId,
            record.CaeAuthorizationId,
            record.CaeAllocationId,
            (CfeFamily)record.CfeType,
            record.Series,
            record.Number,
            record.CaeAuthorizationNumber,
            record.CaeRangeFrom,
            record.CaeRangeTo,
            DateOnly.FromDateTime(record.CaeValidFrom),
            DateOnly.FromDateTime(record.CaeValidTo),
            DateOnly.FromDateTime(record.FiscalDate),
            record.LocationId,
            record.TerminalId,
            record.ReceiverIdentification.HasValue
                ? (ReceiverIdentificationRequirement)record.ReceiverIdentification.Value
                : null,
            record.FormatVersion,
            record.ConfirmationFingerprint,
            record.SettlementFingerprint,
            record.CurrencyCode,
            record.NetAmount,
            record.VatAmount,
            record.TotalAmount,
            (FiscalDocumentStatus)record.Status,
            record.IdentityCreatedAtUtc);
}
