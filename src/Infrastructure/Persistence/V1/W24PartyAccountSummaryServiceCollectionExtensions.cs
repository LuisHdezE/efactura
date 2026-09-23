using EFactura.Application.Parties;
using EFactura.Application.Payables;
using EFactura.Application.Receivables;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.V1;

public static class W24PartyAccountSummaryServiceCollectionExtensions
{
    public static IServiceCollection AddW24PartyAccountSummaryComposition(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<EfReceivableBalanceRepository>();
        services.AddScoped<IReceivableBalanceSourceReader>(sp =>
            sp.GetRequiredService<EfReceivableBalanceRepository>());
        services.AddScoped<IReceivableBalanceEffectRepository>(sp =>
            sp.GetRequiredService<EfReceivableBalanceRepository>());
        services.AddScoped<IPartyReceivableAccountReadModel, PartyReceivableAccountReadModel>();

        services.AddScoped<EfPayableBalanceRepository>();
        services.AddScoped<IPayableBalanceSourceReader>(sp =>
            sp.GetRequiredService<EfPayableBalanceRepository>());
        services.AddScoped<IPayableBalanceEffectRepository>(sp =>
            sp.GetRequiredService<EfPayableBalanceRepository>());
        services.AddScoped<IPartyPayableAccountReadModel, PartyPayableAccountReadModel>();

        services.AddScoped<IPartyAccountSummaryReadModel, PartyAccountSummaryReadModel>();

        return services;
    }
}
