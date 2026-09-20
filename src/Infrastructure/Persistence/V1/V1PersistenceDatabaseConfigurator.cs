using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.V1;

public static class V1PersistenceDatabaseConfigurator
{
    public static void Configure(
        DbContextOptionsBuilder options,
        V1DatabaseProvider provider,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"A connection string is required for v1 provider {provider}.");
        }

        switch (provider)
        {
            case V1DatabaseProvider.PostgreSql:
                options.UseNpgsql(connectionString);
                break;
            case V1DatabaseProvider.MySql:
                options.UseMySQL(connectionString);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported v1 database provider.");
        }

        // The terminal customizer extends the accepted chain rather than replacing its semantics:
        // V1PersistenceLaterStateModelCustomizer -> V1PersistenceEnvelopeModelCustomizer ->
        // V1PersistenceCfeDocumentResponseModelCustomizer ->
        // V1PersistenceCfeDocumentResponseSignatureModelCustomizer ->
        // V1PersistenceCfeDocumentResponseCertificateTrustModelCustomizer ->
        // V1PersistenceSecurityRoleModelCustomizer ->
        // V1PersistenceSecurityUserModelCustomizer.
        // Keeping this lineage explicit also makes the architecture guard's continuity invariant visible here.
        options.ReplaceService<IModelCustomizer, V1PersistenceSecurityUserModelCustomizer>();
    }
}
