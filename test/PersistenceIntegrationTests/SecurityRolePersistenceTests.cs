using System.Data.Common;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Security;
using EFactura.Application.IdentityAccess;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class SecurityRolePersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Role_create_and_replace_are_atomic_audited_idempotent_and_provider_equivalent(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var actor = new FixedActorContextAccessor(new ActorContext(
            "actor-w13",
            "W1.3 Persistence Tester",
            true,
            new HashSet<string>(new[]
            {
                Permissions.SecurityRolesRead,
                Permissions.SecurityManageRoles
            }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            null));
        var correlation = new FixedCorrelationContextAccessor(
            new CorrelationContext("corr-w13", "trace-w13"));

        string roleId;

        await using (var context = database.CreateContext())
        {
            var repository = new EfSecurityRoleRepository(context);
            var create = new CreateRoleUseCase(
                repository,
                new EfTransactionManager(context),
                new EfUnitOfWork(context),
                new EfIdempotencyStore(context),
                new EfAuditWriter(context),
                new EfOutboxWriter(context),
                actor,
                correlation);

            var created = await create.ExecuteAsync(new CreateRoleCommand(
                "company-1",
                "Operator",
                "Initial role",
                new[] { Permissions.SalesRead, Permissions.CatalogRead, Permissions.SalesRead },
                "w13-create-role",
                "hash-w13-create-role"));

            Assert.False(created.Replayed);
            Assert.Equal(1, created.Role.Version);
            Assert.Equal(new[] { Permissions.CatalogRead, Permissions.SalesRead }, created.Role.Permissions);
            roleId = created.Role.Id;
        }

        await using (var context = database.CreateContext())
        {
            var repository = new EfSecurityRoleRepository(context);
            var update = new UpdateRoleUseCase(
                repository,
                new EfTransactionManager(context),
                new EfUnitOfWork(context),
                new EfIdempotencyStore(context),
                new EfAuditWriter(context),
                new EfOutboxWriter(context),
                actor,
                correlation);

            var updated = await update.ExecuteAsync(new UpdateRoleCommand(
                "company-1",
                roleId,
                "Supervisor",
                "Replacement role",
                false,
                new[] { Permissions.AuditRead, Permissions.CatalogRead },
                1,
                "w13-update-role",
                "hash-w13-update-role"));

            Assert.False(updated.Replayed);
            Assert.Equal(2, updated.Role.Version);
            Assert.False(updated.Role.Active);
            Assert.Equal(new[] { Permissions.AuditRead, Permissions.CatalogRead }, updated.Role.Permissions);
        }

        await using (var context = database.CreateContext())
        {
            var repository = new EfSecurityRoleRepository(context);
            var create = new CreateRoleUseCase(
                repository,
                new EfTransactionManager(context),
                new EfUnitOfWork(context),
                new EfIdempotencyStore(context),
                new EfAuditWriter(context),
                new EfOutboxWriter(context),
                actor,
                correlation);

            var invalid = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
                create.ExecuteAsync(new CreateRoleCommand(
                    "company-1",
                    "Invalid role",
                    null,
                    new[] { "security.not-a-real-permission" },
                    "w13-invalid-role",
                    "hash-w13-invalid-role")));

            Assert.Equal(ApplicationProblemKind.Validation, invalid.Kind);
            Assert.Equal("identity.role.permission_unknown", invalid.Code);
        }

        await using var verification = database.CreateContext();
        var role = await verification.Set<V1SecurityRoleRecord>()
            .Include(x => x.Permissions)
            .SingleAsync(x => x.Id == roleId);

        Assert.Equal("company-1", role.OrganizationId);
        Assert.Equal("Supervisor", role.Name);
        Assert.Equal("SUPERVISOR", role.NormalizedName);
        Assert.Equal("Replacement role", role.Description);
        Assert.False(role.Active);
        Assert.Equal(2, role.Version);
        Assert.Equal(
            new[] { Permissions.AuditRead, Permissions.CatalogRead },
            role.Permissions.Select(x => x.PermissionCode).OrderBy(x => x, StringComparer.Ordinal));

        Assert.Equal(2, await verification.AuditEvents.CountAsync());
        Assert.Equal(2, await verification.OutboxMessages.CountAsync());
        Assert.Equal(2, await verification.IdempotencyRecords.CountAsync());
        Assert.Equal(1, await verification.Set<V1SecurityRoleRecord>().CountAsync());
        Assert.Equal(2, await verification.Set<V1SecurityRolePermissionRecord>().CountAsync());
    }

    private sealed class FixedActorContextAccessor : IActorContextAccessor
    {
        public FixedActorContextAccessor(ActorContext current) => Current = current;
        public ActorContext Current { get; }
    }

    private sealed class FixedCorrelationContextAccessor : ICorrelationContextAccessor
    {
        public FixedCorrelationContextAccessor(CorrelationContext current) => Current = current;
        public CorrelationContext Current { get; }
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly DbContextOptions<V1PersistenceDbContext> _options;

        private TestDatabase(DbContextOptions<V1PersistenceDbContext> options)
        {
            _options = options;
        }

        public static async Task<TestDatabase?> CreateAsync(V1DatabaseProvider provider)
        {
            var variable = provider == V1DatabaseProvider.PostgreSql
                ? "POSTGRES_TEST_CONNECTION"
                : "MYSQL_TEST_CONNECTION";

            var baseConnectionString = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(baseConnectionString))
            {
                if (string.Equals(
                    Environment.GetEnvironmentVariable("PERSISTENCE_INTEGRATION_REQUIRED"),
                    "true",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Required integration test connection variable {variable} is missing.");
                }

                return null;
            }

            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = baseConnectionString };
            connectionBuilder["Database"] = $"ef_roles_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";

            var optionsBuilder = new DbContextOptionsBuilder<V1PersistenceDbContext>();
            V1PersistenceDatabaseConfigurator.Configure(optionsBuilder, provider, connectionBuilder.ConnectionString);

            var database = new TestDatabase(optionsBuilder.Options);
            await using var context = database.CreateContext();
            await context.Database.EnsureCreatedAsync();
            return database;
        }

        public V1PersistenceDbContext CreateContext() => new(_options);

        public async ValueTask DisposeAsync()
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }
    }
}
