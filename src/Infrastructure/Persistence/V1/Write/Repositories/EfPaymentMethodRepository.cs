using EFactura.Application.Common.Errors;
using EFactura.Application.Payments;
using EFactura.Domain.Payments;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfPaymentMethodRepository : IPaymentMethodRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfPaymentMethodRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        _dbContext.Set<V1PaymentMethodRecord>().Add(new V1PaymentMethodRecord
        {
            Id = paymentMethod.Id,
            OrganizationId = paymentMethod.OrganizationId,
            Name = paymentMethod.Name,
            Enabled = paymentMethod.Enabled,
            Version = paymentMethod.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        return Task.CompletedTask;
    }

    public async Task<PaymentMethod?> GetAsync(
        string organizationId,
        Guid paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1PaymentMethodRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == paymentMethodId,
                cancellationToken);

        return record is null
            ? null
            : PaymentMethod.Rehydrate(record.Id, record.OrganizationId, record.Name, record.Enabled, record.Version);
    }

    public async Task SaveAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        var records = _dbContext.Set<V1PaymentMethodRecord>();
        var record = await records.SingleOrDefaultAsync(
            x => x.OrganizationId == paymentMethod.OrganizationId && x.Id == paymentMethod.Id,
            cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "payments.method_not_found",
                "The requested payment method was not found.");

        var priorVersion = paymentMethod.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The payment method changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.Name = paymentMethod.Name;
        record.Enabled = paymentMethod.Enabled;
        record.Version = paymentMethod.Version;
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
