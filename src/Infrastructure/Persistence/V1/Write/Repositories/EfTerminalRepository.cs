using EFactura.Application.Common.Errors;
using EFactura.Application.Organizations;
using EFactura.Domain.Organizations;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfTerminalRepository : ITerminalRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfTerminalRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyCollection<Terminal>> ListAsync(
        string organizationId,
        bool? active,
        string? locationId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<V1TerminalRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (active.HasValue)
            query = query.Where(x => x.Active == active.Value);

        if (!string.IsNullOrWhiteSpace(locationId))
            query = query.Where(x => x.LocationId == locationId);

        return (await query
                .OrderBy(x => x.NormalizedCode)
                .ThenBy(x => x.Id)
                .ToArrayAsync(cancellationToken))
            .Select(Map)
            .ToArray();
    }

    public async Task<Terminal?> GetAsync(
        string organizationId,
        string terminalId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1TerminalRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == terminalId,
                cancellationToken);

        return record is null ? null : Map(record);
    }

    public Task<bool> CodeExistsAsync(
        string organizationId,
        string normalizedCode,
        CancellationToken cancellationToken = default) =>
        _dbContext.Set<V1TerminalRecord>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId && x.NormalizedCode == normalizedCode,
                cancellationToken);

    public Task<bool> HasActiveAtLocationAsync(
        string organizationId,
        string locationId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Set<V1TerminalRecord>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId
                     && x.LocationId == locationId
                     && x.Active,
                cancellationToken);

    public Task AddAsync(Terminal terminal, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        _dbContext.Set<V1TerminalRecord>().Add(new V1TerminalRecord
        {
            Id = terminal.Id,
            OrganizationId = terminal.OrganizationId,
            Code = terminal.Code,
            NormalizedCode = terminal.Code,
            Name = terminal.Name,
            LocationId = terminal.LocationId,
            Active = terminal.Active,
            Version = terminal.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        return Task.CompletedTask;
    }

    public async Task SaveAsync(Terminal terminal, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1TerminalRecord>()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == terminal.OrganizationId && x.Id == terminal.Id,
                cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "organization.terminal_not_found",
                "The requested terminal was not found.");

        var priorVersion = terminal.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The terminal changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.Name = terminal.Name;
        record.LocationId = terminal.LocationId;
        record.Active = terminal.Active;
        record.Version = terminal.Version;
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static Terminal Map(V1TerminalRecord record) =>
        Terminal.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.Code,
            record.Name,
            record.LocationId,
            record.Active,
            record.Version);
}
