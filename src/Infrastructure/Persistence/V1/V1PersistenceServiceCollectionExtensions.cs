using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.V1;

public static class V1PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddV1Persistence(
        this IServiceCollection services,
        V1DatabaseProvider provider,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<V1PersistenceDbContext>(options =>
            V1PersistenceDatabaseConfigurator.Configure(options, provider, connectionString));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ITransactionManager, EfTransactionManager>();
        services.AddScoped<IAuditWriter, EfAuditWriter>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<IOutboxWriter, EfOutboxWriter>();
        services.AddScoped<IInboxStore, EfInboxStore>();

        return services;
    }
}
