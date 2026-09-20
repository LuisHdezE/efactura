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

public sealed class SecurityUserPersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task User_create_patch_role_replace_replay_and_identity_uniqueness_are_provider_equivalent(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var actor = new FixedActorContextAccessor(new ActorContext(
            "admin-subject",
            "W1.4 Persistence Tester",
            true,
            new HashSet<string>(new[]
            {
                Permissions.SecurityUsersRead,
                Permissions.SecurityUsersManage,
                Permissions.SecurityManageRoles
            }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1", "company-2" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "location-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "terminal-1", "terminal-2" }, StringComparer.Ordinal),
            null));
        var correlation = new FixedCorrelationContextAccessor(
            new CorrelationContext("corr-w14", "trace-w14"));

        await SeedReferenceDataAsync(database);

        string userId;
        await using (var context = database.CreateContext())
        {
            var users = new EfSecurityUserRepository(context);
            var locations = new EfOrganizationRepository(context);
            var create = CreateUseCase(context, users, locations, actor, correlation);

            var created = await create.ExecuteAsync(new CreateUserCommand(
                "company-1",
                "oidc",
                "external-user-1",
                "Operator One",
                "operator@example.test",
                new[] { "location-1", "location-1" },
                new[] { "terminal-1", "terminal-1" },
                "w14-create-user",
                "hash-w14-create-user"));

            Assert.False(created.Replayed);
            Assert.Equal(1, created.User.Version);
            Assert.True(created.User.Active);
            Assert.Equal(new[] { "location-1" }, created.User.LocationScopes);
            Assert.Equal(new[] { "terminal-1" }, created.User.TerminalScopes);
            Assert.Empty(created.User.RoleIds);
            userId = created.User.Id;
        }

        await using (var context = database.CreateContext())
        {
            var users = new EfSecurityUserRepository(context);
            var locations = new EfOrganizationRepository(context);
            var create = CreateUseCase(context, users, locations, actor, correlation);

            var replayed = await create.ExecuteAsync(new CreateUserCommand(
                "company-1",
                "oidc",
                "external-user-1",
                "Operator One",
                "operator@example.test",
                new[] { "location-1", "location-1" },
                new[] { "terminal-1", "terminal-1" },
                "w14-create-user",
                "hash-w14-create-user"));

            Assert.True(replayed.Replayed);
            Assert.Equal(userId, replayed.User.Id);
            Assert.Equal(1, replayed.User.Version);
        }

        await using (var context = database.CreateContext())
        {
            var users = new EfSecurityUserRepository(context);
            var locations = new EfOrganizationRepository(context);
            var create = CreateUseCase(context, users, locations, actor, correlation);

            var duplicate = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
                create.ExecuteAsync(new CreateUserCommand(
                    "company-1",
                    " OIDC ",
                    "external-user-1",
                    "Duplicate",
                    null,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    "w14-duplicate-user",
                    "hash-w14-duplicate-user")));

            Assert.Equal(ApplicationProblemKind.Conflict, duplicate.Kind);
            Assert.Equal("identity.user.identity_duplicate", duplicate.Code);
            Assert.Equal("duplicate_identity_link", duplicate.ConflictType);
        }

        await using (var context = database.CreateContext())
        {
            var users = new EfSecurityUserRepository(context);
            var locations = new EfOrganizationRepository(context);
            var create = CreateUseCase(context, users, locations, actor, correlation);

            var otherCompany = await create.ExecuteAsync(new CreateUserCommand(
                "company-2",
                "OIDC",
                "external-user-1",
                "Same external identity, other company",
                null,
                Array.Empty<string>(),
                Array.Empty<string>(),
                "w14-create-company2-user",
                "hash-w14-create-company2-user"));

            Assert.False(otherCompany.Replayed);
            Assert.Equal(1, otherCompany.User.Version);
        }

        await using (var context = database.CreateContext())
        {
            var users = new EfSecurityUserRepository(context);
            var locations = new EfOrganizationRepository(context);
            var update = UpdateUseCase(context, users, locations, actor, correlation);

            var updated = await update.ExecuteAsync(new UpdateUserCommand(
                "company-1",
                userId,
                "Operator Updated",
                "updated@example.test",
                false,
                new[] { "location-1" },
                new[] { "terminal-2" },
                1,
                "w14-update-user",
                "hash-w14-update-user"));

            Assert.False(updated.Replayed);
            Assert.Equal(2, updated.User.Version);
            Assert.False(updated.User.Active);
            Assert.Equal("Operator Updated", updated.User.DisplayName);
            Assert.Equal("updated@example.test", updated.User.Email);
            Assert.Equal(new[] { "terminal-2" }, updated.User.TerminalScopes);
        }

        await using (var context = database.CreateContext())
        {
            var users = new EfSecurityUserRepository(context);
            var locations = new EfOrganizationRepository(context);
            var update = UpdateUseCase(context, users, locations, actor, correlation);

            var replayed = await update.ExecuteAsync(new UpdateUserCommand(
                "company-1",
                userId,
                "Operator Updated",
                "updated@example.test",
                false,
                new[] { "location-1" },
                new[] { "terminal-2" },
                1,
                "w14-update-user",
                "hash-w14-update-user"));

            Assert.True(replayed.Replayed);
            Assert.Equal(2, replayed.User.Version);
        }

        await using (var context = database.CreateContext())
        {
            var users = new EfSecurityUserRepository(context);
            var locations = new EfOrganizationRepository(context);
            var update = UpdateUseCase(context, users, locations, actor, correlation);

            var stale = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
                update.ExecuteAsync(new UpdateUserCommand(
                    "company-1",
                    userId,
                    "Stale update",
                    null,
                    true,
                    null,
                    null,
                    1,
                    "w14-stale-user",
                    "hash-w14-stale-user")));

            Assert.Equal(ApplicationProblemKind.Conflict, stale.Kind);
            Assert.Equal("concurrency_conflict", stale.Code);
            Assert.Equal("stale_version", stale.ConflictType);
            Assert.Equal("2", stale.CurrentVersion);
        }

        await using (var context = database.CreateContext())
        {
            var users = new EfSecurityUserRepository(context);
            var roles = new EfSecurityRoleRepository(context);
            var assign = AssignRolesUseCase(context, users, roles, actor, correlation);

            var assigned = await assign.ExecuteAsync(new AssignUserRolesCommand(
                "company-1",
                userId,
                new[] { "role-active", "role-active" },
                2,
                "w14-assign-role",
                "hash-w14-assign-role"));

            Assert.False(assigned.Replayed);
            Assert.Equal(3, assigned.User.Version);
            Assert.Equal(new[] { "role-active" }, assigned.User.RoleIds);
        }

        await using (var context = database.CreateContext())
        {
            var users = new EfSecurityUserRepository(context);
            var roles = new EfSecurityRoleRepository(context);
            var assign = AssignRolesUseCase(context, users, roles, actor, correlation);

            var replayed = await assign.ExecuteAsync(new AssignUserRolesCommand(
                "company-1",
                userId,
                new[] { "role-active", "role-active" },
                2,
                "w14-assign-role",
                "hash-w14-assign-role"));

            Assert.True(replayed.Replayed);
            Assert.Equal(3, replayed.User.Version);
            Assert.Equal(new[] { "role-active" }, replayed.User.RoleIds);
        }

        await using (var context = database.CreateContext())
        {
            var users = new EfSecurityUserRepository(context);
            var roles = new EfSecurityRoleRepository(context);
            var assign = AssignRolesUseCase(context, users, roles, actor, correlation);

            var invalid = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
                assign.ExecuteAsync(new AssignUserRolesCommand(
                    "company-1",
                    userId,
                    new[] { "role-inactive" },
                    3,
                    "w14-invalid-role",
                    "hash-w14-invalid-role")));

            Assert.Equal(ApplicationProblemKind.Validation, invalid.Kind);
            Assert.Equal("identity.user.role_invalid", invalid.Code);
        }

        await using var verification = database.CreateContext();
        var user = await verification.Set<V1SecurityUserRecord>()
            .Include(x => x.LocationScopes)
            .Include(x => x.TerminalScopes)
            .Include(x => x.Roles)
            .SingleAsync(x => x.Id == userId);

        Assert.Equal("company-1", user.OrganizationId);
        Assert.Equal("oidc", user.IdentityProvider);
        Assert.Equal("OIDC", user.NormalizedIdentityProvider);
        Assert.Equal("external-user-1", user.ExternalSubject);
        Assert.Equal("Operator Updated", user.DisplayName);
        Assert.False(user.Active);
        Assert.Equal(3, user.Version);
        Assert.Equal(new[] { "location-1" }, user.LocationScopes.Select(x => x.LocationId));
        Assert.Equal(new[] { "terminal-2" }, user.TerminalScopes.Select(x => x.TerminalId));
        Assert.Equal(new[] { "role-active" }, user.Roles.Select(x => x.RoleId));

        Assert.Equal(2, await verification.Set<V1SecurityUserRecord>().CountAsync());
        Assert.Equal(4, await verification.AuditEvents.CountAsync());
        Assert.Equal(4, await verification.OutboxMessages.CountAsync());
        var idempotency = await verification.IdempotencyRecords.ToListAsync();
        Assert.Equal(4, idempotency.Count);
        Assert.All(idempotency, row => Assert.Equal(1, row.State));
        Assert.DoesNotContain(idempotency, row => row.RequestHash == "hash-w14-duplicate-user");
        Assert.DoesNotContain(idempotency, row => row.RequestHash == "hash-w14-stale-user");
        Assert.DoesNotContain(idempotency, row => row.RequestHash == "hash-w14-invalid-role");
    }

    private static async Task SeedReferenceDataAsync(TestDatabase database)
    {
        await using var context = database.CreateContext();
        var now = DateTimeOffset.UtcNow;

        context.Set<V1FiscalLocationRecord>().Add(new V1FiscalLocationRecord
        {
            Id = "location-1",
            OrganizationId = "company-1",
            Name = "Main",
            DgiBranchCode = "0001",
            FiscalAddress = "Test 1",
            City = "Montevideo",
            Department = "Montevideo",
            Active = true,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        context.Set<V1SecurityRoleRecord>().AddRange(
            new V1SecurityRoleRecord
            {
                Id = "role-active",
                OrganizationId = "company-1",
                Name = "Operator",
                NormalizedName = "OPERATOR",
                Description = null,
                Active = true,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new V1SecurityRoleRecord
            {
                Id = "role-inactive",
                OrganizationId = "company-1",
                Name = "Retired",
                NormalizedName = "RETIRED",
                Description = null,
                Active = false,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        await context.SaveChangesAsync();
    }

    private static CreateUserUseCase CreateUseCase(
        V1PersistenceDbContext context,
        EfSecurityUserRepository users,
        EfOrganizationRepository locations,
        IActorContextAccessor actor,
        ICorrelationContextAccessor correlation) =>
        new(
            users,
            locations,
            new EfTransactionManager(context),
            new EfUnitOfWork(context),
            new EfIdempotencyStore(context),
            new EfAuditWriter(context),
            new EfOutboxWriter(context),
            actor,
            correlation);

    private static UpdateUserUseCase UpdateUseCase(
        V1PersistenceDbContext context,
        EfSecurityUserRepository users,
        EfOrganizationRepository locations,
        IActorContextAccessor actor,
        ICorrelationContextAccessor correlation) =>
        new(
            users,
            locations,
            new EfTransactionManager(context),
            new EfUnitOfWork(context),
            new EfIdempotencyStore(context),
            new EfAuditWriter(context),
            new EfOutboxWriter(context),
            actor,
            correlation);

    private static AssignUserRolesUseCase AssignRolesUseCase(
        V1PersistenceDbContext context,
        EfSecurityUserRepository users,
        EfSecurityRoleRepository roles,
        IActorContextAccessor actor,
        ICorrelationContextAccessor correlation) =>
        new(
            users,
            roles,
            new EfTransactionManager(context),
            new EfUnitOfWork(context),
            new EfIdempotencyStore(context),
            new EfAuditWriter(context),
            new EfOutboxWriter(context),
            actor,
            correlation);

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
            connectionBuilder["Database"] = $"ef_users_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";

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
