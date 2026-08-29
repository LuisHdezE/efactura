using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Npgsql;

namespace Infrastructure.Persistence.V1.Transactions;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private const string InventoryPositionUniqueIndex =
        "IX_v1_inventory_positions_OrganizationId_ItemId_LocationId";

    private readonly Write.V1PersistenceDbContext _dbContext;

    public EfUnitOfWork(Write.V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The resource changed before this operation could be committed.",
                conflictType: "stale_version");
        }
        catch (DbUpdateException ex) when (IsInventoryPositionUniqueViolation(ex))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The inventory position was created concurrently before this operation could be committed.",
                conflictType: "duplicate_position");
        }
    }

    private static bool IsInventoryPositionUniqueViolation(DbUpdateException exception)
    {
        if (!exception.Entries.Any(entry =>
                entry.Entity is V1InventoryPositionRecord && entry.State == EntityState.Added))
        {
            return false;
        }

        return exception.InnerException switch
        {
            PostgresException postgres =>
                postgres.SqlState == PostgresErrorCodes.UniqueViolation
                && string.Equals(postgres.ConstraintName, InventoryPositionUniqueIndex, StringComparison.Ordinal),

            MySqlException mysql =>
                mysql.Number == 1062
                && mysql.Message.Contains(InventoryPositionUniqueIndex, StringComparison.OrdinalIgnoreCase),

            _ => false
        };
    }
}
