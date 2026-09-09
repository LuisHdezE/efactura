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
    private const string CaeArtifactUniqueIndex =
        "UX_v1_cae_auth_org_artifact";
    private const string FiscalNumberUniqueIndex =
        "UX_v1_fiscal_res_identity";
    private const string FiscalOperationUniqueIndex =
        "UX_v1_fiscal_res_operation";
    private const string FiscalDocumentRequestUniqueIndex =
        "UX_v1_fd_org_req";
    private const string FiscalDocumentIdentityUniqueIndex =
        "UX_v1_fd_identity";
    private const string FiscalDocumentReservationUniqueIndex =
        "UX_v1_fd_res";
    private const string FiscalLocationBranchUniqueIndex =
        "UX_v1_location_org_branch";
    private const string FiscalContentSnapshotDocumentUniqueIndex =
        "UX_v1_fcs_document";

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
        catch (DbUpdateException ex) when (IsLocationBranchUniqueViolation(ex))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "organization.location.branch_code_duplicate",
                "The DGI branch code is already assigned to another location in this organization.",
                conflictType: "duplicate_branch_code");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, CaeArtifactUniqueIndex, typeof(V1CaeAuthorizationRecord)))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "cae.duplicate_artifact",
                "The CAE source artifact was imported concurrently by another operation.",
                conflictType: "duplicate_resource");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, FiscalNumberUniqueIndex, typeof(V1FiscalNumberReservationRecord)))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The fiscal number was reserved concurrently by another transaction.",
                conflictType: "duplicate_fiscal_number");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, FiscalOperationUniqueIndex, typeof(V1FiscalNumberReservationRecord)))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "fiscal.operation_already_reserved",
                "The fiscal operation already owns a number reservation.",
                conflictType: "duplicate_resource");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, FiscalDocumentRequestUniqueIndex, typeof(V1FiscalDocumentRecord)))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "fiscalization.identity_already_created",
                "The fiscalization request already owns a fiscal-document identity.",
                conflictType: "duplicate_resource");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, FiscalDocumentIdentityUniqueIndex, typeof(V1FiscalDocumentRecord)))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The fiscal document identity was created concurrently by another transaction.",
                conflictType: "duplicate_fiscal_identity");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, FiscalDocumentReservationUniqueIndex, typeof(V1FiscalDocumentRecord)))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "fiscal.number_already_consumed",
                "The fiscal number reservation is already linked to a fiscal document.",
                conflictType: "duplicate_resource");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, FiscalContentSnapshotDocumentUniqueIndex, typeof(V1FiscalContentSnapshotRecord)))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "fiscal.snapshot.already_created",
                "The fiscal document already owns an immutable content snapshot.",
                conflictType: "duplicate_resource");
        }
    }

    private static bool IsLocationBranchUniqueViolation(DbUpdateException exception)
    {
        if (!exception.Entries.Any(entry =>
                entry.Entity is V1FiscalLocationRecord
                && entry.State is EntityState.Added or EntityState.Modified))
        {
            return false;
        }

        return MatchesUniqueConstraint(exception, FiscalLocationBranchUniqueIndex);
    }

    private static bool IsInventoryPositionUniqueViolation(DbUpdateException exception)
    {
        if (!exception.Entries.Any(entry =>
                entry.Entity is V1InventoryPositionRecord && entry.State == EntityState.Added))
        {
            return false;
        }

        return MatchesUniqueConstraint(exception, InventoryPositionUniqueIndex);
    }

    private static bool IsUniqueViolation(DbUpdateException exception, string indexName, Type entityType)
    {
        if (!exception.Entries.Any(entry =>
                entityType.IsInstanceOfType(entry.Entity) && entry.State == EntityState.Added))
        {
            return false;
        }

        return MatchesUniqueConstraint(exception, indexName);
    }

    private static bool MatchesUniqueConstraint(DbUpdateException exception, string indexName) =>
        exception.InnerException switch
        {
            PostgresException postgres =>
                postgres.SqlState == PostgresErrorCodes.UniqueViolation
                && string.Equals(postgres.ConstraintName, indexName, StringComparison.Ordinal),

            MySqlException mysql =>
                mysql.Number == 1062
                && mysql.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase),

            _ => false
        };
}
