using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Application.Sales;
using EFactura.Domain.Sales;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfSaleRepository : ISaleRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfSaleRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        _dbContext.Sales.Add(MapRecord(sale, now, now));
        return Task.CompletedTask;
    }

    public async Task<Sale?> GetAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Sales
            .AsNoTracking()
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == saleId, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<PageResult<Sale>> SearchAsync(
        SaleSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Sales
            .AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.OrganizationId == request.OrganizationId);

        if (request.From.HasValue)
        {
            var from = request.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.EffectiveOnUtc >= from);
        }
        if (request.To.HasValue)
        {
            var to = request.To.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.EffectiveOnUtc <= to);
        }
        if (request.CustomerPartyId.HasValue)
            query = query.Where(x => x.CustomerPartyId == request.CustomerPartyId.Value);
        if (request.Status.HasValue)
        {
            var status = (int)request.Status.Value;
            query = query.Where(x => x.Status == status);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var records = await query
            .OrderByDescending(x => x.EffectiveOnUtc)
            .ThenByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<Sale>(records.Select(Map).ToArray(), request.Page, request.PageSize, total);
    }

    public async Task SaveAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Sales
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.OrganizationId == sale.OrganizationId && x.Id == sale.Id, cancellationToken)
            ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "sales.not_found", "Sale was not found.");

        var priorVersion = sale.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The sale changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.CustomerPartyId = sale.CustomerPartyId;
        record.Intent = (int)sale.Intent;
        record.CurrencyCode = sale.CurrencyCode;
        record.EffectiveOnUtc = sale.EffectiveOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        record.DeliveryCountry = sale.DeliveryCountry;
        record.GoodsExportConfirmed = sale.GoodsExportConfirmed;
        record.Status = (int)sale.Status;
        record.ValidationFingerprint = sale.ValidationFingerprint;
        record.ValidatedAtUtc = sale.ValidatedAtUtc;
        record.Version = sale.Version;
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.SaleLines.RemoveRange(record.Lines);
        record.Lines.Clear();
        foreach (var line in sale.Lines)
            record.Lines.Add(MapLine(line, sale.Id));
    }

    private static V1SaleRecord MapRecord(Sale sale, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc) => new()
    {
        Id = sale.Id,
        OrganizationId = sale.OrganizationId,
        LocationId = sale.LocationId,
        TerminalId = sale.TerminalId,
        CustomerPartyId = sale.CustomerPartyId,
        Intent = (int)sale.Intent,
        CurrencyCode = sale.CurrencyCode,
        EffectiveOnUtc = sale.EffectiveOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        DeliveryCountry = sale.DeliveryCountry,
        GoodsExportConfirmed = sale.GoodsExportConfirmed,
        Status = (int)sale.Status,
        ValidationFingerprint = sale.ValidationFingerprint,
        ValidatedAtUtc = sale.ValidatedAtUtc,
        Version = sale.Version,
        CreatedAtUtc = createdAtUtc,
        UpdatedAtUtc = updatedAtUtc,
        Lines = sale.Lines.Select(line => MapLine(line, sale.Id)).ToList()
    };

    private static V1SaleLineRecord MapLine(SaleLine line, Guid saleId) => new()
    {
        Id = line.Id,
        SaleId = saleId,
        ItemId = line.ItemId,
        ItemCode = line.ItemCode,
        ItemName = line.ItemName,
        Kind = (int)line.Kind,
        Quantity = line.Quantity,
        UnitPrice = line.UnitPrice,
        TaxProfileId = line.TaxProfileId,
        ServicePerformanceScope = (int)line.ServicePerformanceScope,
        ServiceUseCountry = line.ServiceUseCountry,
        ExportServiceKind = (int)line.ExportServiceKind,
        RecipientIsPersonAbroad = (int)line.RecipientIsPersonAbroad,
        ExclusiveUseAbroad = (int)line.ExclusiveUseAbroad,
        ForeignEconomicRelation = (int)line.ForeignEconomicRelation,
        RecipientInstalledInFreeZone = (int)line.RecipientInstalledInFreeZone,
        ProviderFromNonFreeNationalTerritory = (int)line.ProviderFromNonFreeNationalTerritory
    };

    private static Sale Map(V1SaleRecord record) => Sale.Rehydrate(
        record.Id,
        record.OrganizationId,
        record.LocationId,
        record.TerminalId,
        record.CustomerPartyId,
        (SaleCommercialIntent)record.Intent,
        record.CurrencyCode,
        DateOnly.FromDateTime(record.EffectiveOnUtc),
        record.DeliveryCountry,
        record.GoodsExportConfirmed,
        record.Lines.OrderBy(x => x.Id).Select(MapLine).ToArray(),
        (SaleStatus)record.Status,
        record.ValidationFingerprint,
        record.ValidatedAtUtc,
        record.Version);

    private static SaleLine MapLine(V1SaleLineRecord line) => SaleLine.Rehydrate(
        line.Id,
        line.ItemId,
        line.ItemCode,
        line.ItemName,
        (SaleLineKind)line.Kind,
        line.Quantity,
        line.UnitPrice,
        line.TaxProfileId,
        (SaleServicePerformanceScope)line.ServicePerformanceScope,
        line.ServiceUseCountry,
        (SaleExportServiceKind)line.ExportServiceKind,
        (SaleRegulatoryFactStatus)line.RecipientIsPersonAbroad,
        (SaleRegulatoryFactStatus)line.ExclusiveUseAbroad,
        (SaleRegulatoryFactStatus)line.ForeignEconomicRelation,
        (SaleRegulatoryFactStatus)line.RecipientInstalledInFreeZone,
        (SaleRegulatoryFactStatus)line.ProviderFromNonFreeNationalTerritory);
}
