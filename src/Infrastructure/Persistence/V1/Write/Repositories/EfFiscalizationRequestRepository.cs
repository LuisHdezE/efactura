using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfFiscalizationRequestRepository : IFiscalizationRequestRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalizationRequestRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(FiscalizationRequest request, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<V1FiscalizationRequestRecord>().Add(new V1FiscalizationRequestRecord
        {
            Id = request.Id,
            OrganizationId = request.OrganizationId,
            SaleId = request.SaleId,
            LocationId = request.LocationId,
            TerminalId = request.TerminalId,
            CfeFamily = (int)request.CfeFamily,
            ReceiverIdentification = request.ReceiverIdentification.HasValue
                ? (int)request.ReceiverIdentification.Value
                : null,
            FormatVersion = request.FormatVersion,
            ConfirmationFingerprint = request.ConfirmationFingerprint,
            SettlementFingerprint = request.SettlementFingerprint,
            CurrencyCode = request.CurrencyCode,
            NetAmount = request.NetAmount,
            VatAmount = request.VatAmount,
            TotalAmount = request.TotalAmount,
            Status = (int)request.Status,
            Version = request.Version,
            RequestedAtUtc = request.RequestedAtUtc
        });
        return Task.CompletedTask;
    }

    public async Task<FiscalizationRequest?> GetAsync(
        string organizationId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalizationRequestRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == requestId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<FiscalizationRequest?> GetBySaleAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalizationRequestRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.SaleId == saleId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    private static FiscalizationRequest Map(V1FiscalizationRequestRecord record) =>
        FiscalizationRequest.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.SaleId,
            record.LocationId,
            record.TerminalId,
            (CfeFamily)record.CfeFamily,
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
            (FiscalizationRequestStatus)record.Status,
            record.Version,
            record.RequestedAtUtc);
}
