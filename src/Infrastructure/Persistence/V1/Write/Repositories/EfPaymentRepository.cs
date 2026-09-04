using EFactura.Application.Payments;
using EFactura.Domain.Payments;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfPaymentRepository : IPaymentRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfPaymentRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<V1PaymentRecord>().Add(MapRecord(payment));
        return Task.CompletedTask;
    }

    public async Task<Payment?> GetAsync(
        string organizationId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1PaymentRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == paymentId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<IReadOnlyCollection<Payment>> ListBySaleAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Set<V1PaymentRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.SaleId == saleId)
            .OrderBy(x => x.Sequence)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return records.Select(Map).ToArray();
    }

    private static V1PaymentRecord MapRecord(Payment payment) => new()
    {
        Id = payment.Id,
        OrganizationId = payment.OrganizationId,
        SaleId = payment.SaleId,
        Sequence = payment.Sequence,
        PaymentMethodId = payment.PaymentMethodId,
        PaymentMethodVersion = payment.PaymentMethodVersion,
        Amount = payment.Amount,
        CurrencyCode = payment.CurrencyCode,
        ExternalReference = payment.ExternalReference,
        ConfirmationFingerprint = payment.ConfirmationFingerprint,
        SettlementFingerprint = payment.SettlementFingerprint,
        RecordedAtUtc = payment.RecordedAtUtc
    };

    private static Payment Map(V1PaymentRecord record) =>
        Payment.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.SaleId,
            record.Sequence,
            record.PaymentMethodId,
            record.PaymentMethodVersion,
            record.Amount,
            record.CurrencyCode,
            record.ExternalReference,
            record.ConfirmationFingerprint,
            record.SettlementFingerprint,
            record.RecordedAtUtc);
}
