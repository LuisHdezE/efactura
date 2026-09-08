using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using EFactura.Domain.Catalog;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalDocumentIdentityPersistenceTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Pending_work_item_reserves_number_creates_identity_and_replays_without_duplicates(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, activateCae: true);

        FiscalDocumentIdentityResult first;
        await using (var context = database.CreateContext())
        {
            first = await UseCase(context).ExecuteAsync(
                new PrepareFiscalDocumentIdentityCommand("company-1", seed.FiscalizationRequestId));
            Assert.False(first.Replayed);
            Assert.Equal(CfeFamily.EFactura, first.CfeType);
            Assert.Equal("A", first.Series);
            Assert.Equal(1, first.Number);
            Assert.Equal("CAE-IDENTITY-001", first.CaeAuthorizationNumber);
        }

        await AssertPersistedAsync(database, seed, first.FiscalDocumentId);

        FiscalDocumentIdentityResult replay;
        await using (var context = database.CreateContext())
        {
            replay = await UseCase(context).ExecuteAsync(
                new PrepareFiscalDocumentIdentityCommand("company-1", seed.FiscalizationRequestId));
            Assert.True(replay.Replayed);
            Assert.Equal(first.FiscalDocumentId, replay.FiscalDocumentId);
            Assert.Equal(first.Series, replay.Series);
            Assert.Equal(first.Number, replay.Number);
        }

        await AssertPersistedAsync(database, seed, first.FiscalDocumentId);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Failure_after_final_flush_rolls_back_number_document_request_audit_and_outbox(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, activateCae: true);

        await using (var context = database.CreateContext())
        {
            var failingUnitOfWork = new ThrowAfterSaveUnitOfWork(new EfUnitOfWork(context), 1);
            var useCase = UseCase(context, failingUnitOfWork);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.ExecuteAsync(
                    new PrepareFiscalDocumentIdentityCommand("company-1", seed.FiscalizationRequestId)));
        }

        await using var verification = database.CreateContext();
        var request = await verification.Set<V1FiscalizationRequestRecord>()
            .SingleAsync(x => x.Id == seed.FiscalizationRequestId);
        var cae = await verification.CaeAuthorizations.SingleAsync(x => x.Id == seed.CaeAuthorizationId);

        Assert.Equal((int)FiscalizationRequestStatus.Pending, request.Status);
        Assert.Equal(1, request.Version);
        Assert.Null(request.FiscalDocumentId);
        Assert.Null(request.IdentityCreatedAtUtc);
        Assert.Equal(1, cae.NextNumber);
        Assert.Equal(0, await verification.FiscalNumberReservations.CountAsync());
        Assert.Equal(0, await verification.Set<V1FiscalDocumentRecord>().CountAsync());
        Assert.Equal(0, await verification.AuditEvents.CountAsync());
        Assert.Equal(0, await verification.OutboxMessages.CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Missing_active_CAE_leaves_pending_work_item_without_partial_identity(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, activateCae: false);

        await using (var context = database.CreateContext())
        {
            var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
                UseCase(context).ExecuteAsync(
                    new PrepareFiscalDocumentIdentityCommand("company-1", seed.FiscalizationRequestId)));
            Assert.Equal("cae.active_authorization_not_found", error.Code);
        }

        await using var verification = database.CreateContext();
        var request = await verification.Set<V1FiscalizationRequestRecord>()
            .SingleAsync(x => x.Id == seed.FiscalizationRequestId);
        Assert.Equal((int)FiscalizationRequestStatus.Pending, request.Status);
        Assert.Equal(0, await verification.FiscalNumberReservations.CountAsync());
        Assert.Equal(0, await verification.Set<V1FiscalDocumentRecord>().CountAsync());
        Assert.Equal(0, await verification.AuditEvents.CountAsync());
        Assert.Equal(0, await verification.OutboxMessages.CountAsync());
    }

    private static PrepareFiscalDocumentIdentityUseCase UseCase(
        V1PersistenceDbContext context,
        IUnitOfWork? unitOfWork = null)
    {
        var actor = Actor();
        var correlation = new FixedCorrelationContextAccessor(
            new CorrelationContext("corr-fiscal-identity", "trace-fiscal-identity"));
        var audit = new EfAuditWriter(context);
        var outbox = new EfOutboxWriter(context);

        return new PrepareFiscalDocumentIdentityUseCase(
            new EfFiscalizationRequestRepository(context),
            new EfFiscalDocumentRepository(context),
            new EfSaleRepository(context),
            new FiscalNumberAllocator(
                new EfCaeRepository(context),
                audit,
                outbox,
                actor,
                correlation),
            new EfTransactionManager(context),
            unitOfWork ?? new EfUnitOfWork(context),
            audit,
            outbox,
            actor,
            correlation);
    }

    private static async Task<Seed> SeedAsync(TestDatabase database, bool activateCae)
    {
        var now = DateTimeOffset.UtcNow;
        var saleId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        await using var context = database.CreateContext();

        await new EfCommercialItemRepository(context).AddAsync(
            CommercialItem.Create(
                itemId,
                "company-1",
                "IDENTITY-ITEM",
                "Fiscal identity item",
                null,
                CommercialItemKind.Product,
                "UNIT",
                false,
                null,
                null));

        var sale = Sale.Create(
            saleId,
            "company-1",
            "loc-1",
            "term-1",
            null,
            SaleCommercialIntent.TaxpayerInvoice,
            "UYU",
            new DateOnly(2026, 9, 8),
            "UY",
            false,
            new[]
            {
                SaleLine.Create(
                    Guid.NewGuid(),
                    itemId,
                    "IDENTITY-ITEM",
                    "Fiscal identity item",
                    SaleLineKind.Product,
                    1m,
                    100m,
                    null)
            });
        sale.MarkValidated("validated-identity-seed", now, 1);
        sale.MarkConfirmed(Confirmation, Settlement, now, 2);
        await new EfSaleRepository(context).AddAsync(sale);

        await new EfFiscalizationRequestRepository(context).AddAsync(
            FiscalizationRequest.CreateFromSale(
                requestId,
                "company-1",
                saleId,
                "loc-1",
                "term-1",
                CfeFamily.EFactura,
                ReceiverIdentificationRequirement.Required,
                "25.2",
                Confirmation,
                Settlement,
                "UYU",
                100m,
                22m,
                122m,
                now));

        var cae = CaeAuthorization.ImportVerified(
            "company-1",
            CfeFamily.EFactura,
            "CAE-IDENTITY-001",
            "A",
            1,
            100,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "integration-test",
            "artifact-identity-001",
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "DGI test fixture",
            "https://example.invalid/cae-identity",
            now);
        if (activateCae)
            cae.Activate(new DateOnly(2026, 9, 8), cae.Version, now);

        await new EfCaeRepository(context).AddAuthorizationAsync(cae);
        await new EfUnitOfWork(context).SaveChangesAsync();

        return new Seed(saleId, requestId, cae.Id);
    }

    private static async Task AssertPersistedAsync(
        TestDatabase database,
        Seed seed,
        Guid fiscalDocumentId)
    {
        await using var verification = database.CreateContext();
        var request = await verification.Set<V1FiscalizationRequestRecord>()
            .SingleAsync(x => x.Id == seed.FiscalizationRequestId);
        var document = await verification.Set<V1FiscalDocumentRecord>()
            .SingleAsync(x => x.Id == fiscalDocumentId);
        var reservation = await verification.FiscalNumberReservations.SingleAsync();
        var cae = await verification.CaeAuthorizations.SingleAsync(x => x.Id == seed.CaeAuthorizationId);

        Assert.Equal((int)FiscalizationRequestStatus.IdentityCreated, request.Status);
        Assert.Equal(2, request.Version);
        Assert.Equal(fiscalDocumentId, request.FiscalDocumentId);
        Assert.NotNull(request.IdentityCreatedAtUtc);

        Assert.Equal(seed.SaleId, document.SaleId);
        Assert.Equal(seed.FiscalizationRequestId, document.FiscalizationRequestId);
        Assert.Equal(reservation.Id, document.FiscalNumberReservationId);
        Assert.Equal(seed.CaeAuthorizationId, document.CaeAuthorizationId);
        Assert.Equal((int)CfeFamily.EFactura, document.CfeType);
        Assert.Equal("A", document.Series);
        Assert.Equal(1, document.Number);
        Assert.Equal("CAE-IDENTITY-001", document.CaeAuthorizationNumber);
        Assert.Equal("25.2", document.FormatVersion);
        Assert.Equal(Confirmation, document.ConfirmationFingerprint);
        Assert.Equal(Settlement, document.SettlementFingerprint);

        Assert.Equal(2, cae.NextNumber);
        Assert.Equal(1, await verification.FiscalNumberReservations.CountAsync());
        Assert.Equal(1, await verification.Set<V1FiscalDocumentRecord>().CountAsync());
        Assert.Equal(2, await verification.AuditEvents.CountAsync());
        Assert.Equal(2, await verification.OutboxMessages.CountAsync());
    }

    private static FixedActorContextAccessor Actor() =>
        new(new ActorContext(
            "system-fiscalizer",
            "Fiscalization Worker",
            true,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "loc-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "term-1" }, StringComparer.Ordinal),
            null));

    private sealed record Seed(
        Guid SaleId,
        Guid FiscalizationRequestId,
        Guid CaeAuthorizationId);

    private sealed class FixedActorContextAccessor : IActorContextAccessor
    {
        public FixedActorContextAccessor(ActorContext current) => Current = current;
        public ActorContext Current { get; }
    }

    private sealed class ThrowAfterSaveUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        private readonly int _throwAfterCall;
        private int _calls;

        public ThrowAfterSaveUnitOfWork(IUnitOfWork inner, int throwAfterCall)
        {
            _inner = inner;
            _throwAfterCall = throwAfterCall;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await _inner.SaveChangesAsync(cancellationToken);
            if (++_calls == _throwAfterCall)
                throw new InvalidOperationException("Injected failure after SaveChanges.");
            return result;
        }
    }
}
