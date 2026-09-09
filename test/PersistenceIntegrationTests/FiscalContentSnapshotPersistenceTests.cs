using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using EFactura.Application.Organizations;
using EFactura.Domain.Catalog;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Organizations;
using EFactura.Domain.Parties;
using EFactura.Domain.Sales;
using EFactura.Domain.Taxation;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalContentSnapshotPersistenceTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Snapshot_round_trip_replays_and_survives_later_master_changes(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, withConfirmationEvidence: true);
        var fiscalDocumentId = await CreateIdentityAsync(database, seed.FiscalizationRequestId);

        FiscalContentSnapshotResult first;
        await using (var context = database.CreateContext())
        {
            first = await SnapshotUseCase(context).ExecuteAsync(
                new CreateFiscalContentSnapshotCommand("company-1", fiscalDocumentId));
            Assert.False(first.Replayed);
            Assert.Equal(64, first.ContentFingerprint.Length);
        }

        await AssertOriginalSnapshotAsync(database, fiscalDocumentId, first.SnapshotId);
        await MutateMastersAsync(database, seed.PartyId);

        FiscalContentSnapshotResult replay;
        await using (var context = database.CreateContext())
        {
            replay = await SnapshotUseCase(context).ExecuteAsync(
                new CreateFiscalContentSnapshotCommand("company-1", fiscalDocumentId));
            Assert.True(replay.Replayed);
            Assert.Equal(first.SnapshotId, replay.SnapshotId);
            Assert.Equal(first.ContentFingerprint, replay.ContentFingerprint);
        }

        await AssertOriginalSnapshotAsync(database, fiscalDocumentId, first.SnapshotId);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Failure_after_snapshot_flush_rolls_back_snapshot_audit_and_outbox(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, withConfirmationEvidence: true);
        var fiscalDocumentId = await CreateIdentityAsync(database, seed.FiscalizationRequestId);

        int auditBefore;
        int outboxBefore;
        await using (var baseline = database.CreateContext())
        {
            auditBefore = await baseline.AuditEvents.CountAsync();
            outboxBefore = await baseline.OutboxMessages.CountAsync();
        }

        await using (var context = database.CreateContext())
        {
            var failing = new ThrowAfterSaveUnitOfWork(new EfUnitOfWork(context), 1);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                SnapshotUseCase(context, failing).ExecuteAsync(
                    new CreateFiscalContentSnapshotCommand("company-1", fiscalDocumentId)));
            Assert.Equal("Injected failure after SaveChanges.", error.Message);
        }

        await using var verification = database.CreateContext();
        Assert.Equal(0, await verification.Set<V1FiscalContentSnapshotRecord>().CountAsync());
        Assert.Equal(auditBefore, await verification.AuditEvents.CountAsync());
        Assert.Equal(outboxBefore, await verification.OutboxMessages.CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Historical_identity_without_confirmation_evidence_fails_closed(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, withConfirmationEvidence: false);
        var fiscalDocumentId = await CreateIdentityAsync(database, seed.FiscalizationRequestId);

        await using (var context = database.CreateContext())
        {
            var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
                SnapshotUseCase(context).ExecuteAsync(
                    new CreateFiscalContentSnapshotCommand("company-1", fiscalDocumentId)));
            Assert.Equal("fiscal.snapshot.confirmation_evidence_missing", error.Code);
            Assert.Equal(ApplicationProblemKind.Conflict, error.Kind);
        }

        await using var verification = database.CreateContext();
        Assert.Equal(0, await verification.Set<V1FiscalContentSnapshotRecord>().CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Historical_sale_without_unit_evidence_fails_closed(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, withConfirmationEvidence: true, unitOfMeasure: null);
        var fiscalDocumentId = await CreateIdentityAsync(database, seed.FiscalizationRequestId);

        await using var context = database.CreateContext();
        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            SnapshotUseCase(context).ExecuteAsync(
                new CreateFiscalContentSnapshotCommand("company-1", fiscalDocumentId)));
        Assert.Equal("fiscal.snapshot.item_unit_missing", error.Code);
        Assert.Equal(ApplicationProblemKind.Conflict, error.Kind);
        Assert.Equal("missing_prerequisite", error.ConflictType);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Sale_unit_longer_than_DGI_field_fails_without_truncation(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, withConfirmationEvidence: true, unitOfMeasure: "UNIDAD");
        var fiscalDocumentId = await CreateIdentityAsync(database, seed.FiscalizationRequestId);

        await using var context = database.CreateContext();
        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            SnapshotUseCase(context).ExecuteAsync(
                new CreateFiscalContentSnapshotCommand("company-1", fiscalDocumentId)));
        Assert.Equal("fiscal.snapshot.item_unit_not_dgi_compatible", error.Code);
        Assert.Equal(ApplicationProblemKind.Conflict, error.Kind);
        Assert.Equal("missing_prerequisite", error.ConflictType);
    }

    private static async Task<Guid> CreateIdentityAsync(TestDatabase database, Guid requestId)
    {
        await using var context = database.CreateContext();
        var result = await IdentityUseCase(context).ExecuteAsync(
            new PrepareFiscalDocumentIdentityCommand("company-1", requestId));
        return result.FiscalDocumentId;
    }

    private static PrepareFiscalDocumentIdentityUseCase IdentityUseCase(V1PersistenceDbContext context)
    {
        var actor = Actor();
        var correlation = new FixedCorrelationContextAccessor(
            new CorrelationContext("corr-snapshot-identity", "trace-snapshot-identity"));
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
            new EfUnitOfWork(context),
            audit,
            outbox,
            actor,
            correlation);
    }

    private static CreateFiscalContentSnapshotUseCase SnapshotUseCase(
        V1PersistenceDbContext context,
        IUnitOfWork? unitOfWork = null)
    {
        var actor = Actor();
        var correlation = new FixedCorrelationContextAccessor(
            new CorrelationContext("corr-content-snapshot", "trace-content-snapshot"));
        var audit = new EfAuditWriter(context);
        var outbox = new EfOutboxWriter(context);
        var organizations = new EfOrganizationRepository(context);

        return new CreateFiscalContentSnapshotUseCase(
            new EfFiscalDocumentRepository(context),
            new EfFiscalizationRequestRepository(context),
            new EfSaleRepository(context),
            new FiscalContentSnapshotFactory(
                organizations,
                organizations,
                new EfPartyRepository(context)),
            new EfFiscalContentSnapshotRepository(context),
            new EfTransactionManager(context),
            unitOfWork ?? new EfUnitOfWork(context),
            audit,
            outbox,
            actor,
            correlation);
    }

    private static async Task<Seed> SeedAsync(
        TestDatabase database,
        bool withConfirmationEvidence,
        string? unitOfMeasure = "UNIT")
    {
        var now = DateTimeOffset.UtcNow;
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var partyId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        await using var context = database.CreateContext();
        var organizations = new EfOrganizationRepository(context);

        await organizations.AddAsync(CompanyFiscalProfile.Create(
            "company-1",
            "214748364700",
            "Empresa Original S.A.",
            "Empresa Original"));
        await organizations.AddAsync(FiscalLocation.Create(
            "loc-1",
            "company-1",
            "Casa Central",
            "0001",
            "Av. Italia 1234",
            "Montevideo",
            "Montevideo"));

        var party = Party.Create(
            partyId,
            "company-1",
            PartyKind.Organization,
            "Cliente Original S.A.",
            "UY",
            "UY",
            new[] { PartyRole.Customer },
            new[]
            {
                PartyFiscalIdentity.Create(
                    Guid.NewGuid(),
                    "2",
                    "219999990017",
                    "UY",
                    new DateOnly(2026, 1, 1),
                    null)
            },
            new[]
            {
                PartyAddress.Create(
                    Guid.NewGuid(),
                    PartyAddressKind.Fiscal,
                    "18 de Julio 2000",
                    "Montevideo",
                    "Montevideo",
                    "UY",
                    "11200",
                    true)
            });
        await new EfPartyRepository(context).AddAsync(party);

        await new EfCommercialItemRepository(context).AddAsync(
            CommercialItem.Create(
                itemId,
                "company-1",
                "SNAPSHOT-ITEM",
                "Producto fiscal original",
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
            partyId,
            SaleCommercialIntent.TaxpayerInvoice,
            "UYU",
            new DateOnly(2026, 9, 8),
            "UY",
            false,
            new[]
            {
                SaleLine.Create(
                    lineId,
                    itemId,
                    "SNAPSHOT-ITEM",
                    "Producto fiscal original",
                    SaleLineKind.Product,
                    1m,
                    100m,
                    null,
                    unitOfMeasure: unitOfMeasure)
            });
        sale.MarkValidated("validated-content-snapshot", now, 1);
        sale.MarkConfirmed(Confirmation, Settlement, now, 2);
        await new EfSaleRepository(context).AddAsync(sale);

        FiscalConfirmationEvidence? evidence = null;
        if (withConfirmationEvidence)
        {
            evidence = FiscalConfirmationEvidence.Capture(
                CfeFamily.EFactura,
                ReceiverIdentificationRequirement.Required,
                "25.2",
                Confirmation,
                Calculation(lineId),
                new[] { RuleEvidence() });
        }

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
                now,
                evidence));

        var cae = CaeAuthorization.ImportVerified(
            "company-1",
            CfeFamily.EFactura,
            "CAE-SNAPSHOT-001",
            "A",
            1,
            100,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "integration-test",
            $"artifact-snapshot-{Guid.NewGuid():N}",
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "DGI test fixture",
            "https://example.invalid/cae-snapshot",
            now);
        cae.Activate(new DateOnly(2026, 9, 8), cae.Version, now);
        await new EfCaeRepository(context).AddAuthorizationAsync(cae);

        await new EfUnitOfWork(context).SaveChangesAsync();
        return new Seed(partyId, requestId);
    }

    private static async Task MutateMastersAsync(TestDatabase database, Guid partyId)
    {
        await using var context = database.CreateContext();
        var organizations = new EfOrganizationRepository(context);

        var company = await ((ICompanyFiscalProfileRepository)organizations).GetAsync("company-1")
            ?? throw new InvalidOperationException("Company missing before mutation.");
        company.Update("214748364711", "Empresa Cambiada S.A.", "Empresa Cambiada", company.Version);
        await organizations.SaveAsync(company);

        var location = await ((IFiscalLocationRepository)organizations).GetAsync("company-1", "loc-1")
            ?? throw new InvalidOperationException("Location missing before mutation.");
        location.Update(
            "Sucursal Cambiada",
            "0002",
            "Rambla 9999",
            "Montevideo",
            "Montevideo",
            true,
            location.Version);
        await organizations.SaveAsync(location);

        var parties = new EfPartyRepository(context);
        var party = await parties.GetAsync("company-1", partyId)
            ?? throw new InvalidOperationException("Party missing before mutation.");
        party.UpdateMasterData(
            PartyKind.Organization,
            "Cliente Cambiado S.A.",
            "UY",
            "UY",
            new[]
            {
                PartyAddress.Create(
                    Guid.NewGuid(),
                    PartyAddressKind.Fiscal,
                    "Rambla Cliente 777",
                    "Montevideo",
                    "Montevideo",
                    "UY",
                    "11300",
                    true)
            },
            Array.Empty<PartyContact>(),
            party.Version);
        await parties.SaveAsync(party);

        await new EfUnitOfWork(context).SaveChangesAsync();
    }

    private static async Task AssertOriginalSnapshotAsync(
        TestDatabase database,
        Guid fiscalDocumentId,
        Guid snapshotId)
    {
        await using var context = database.CreateContext();
        var stored = await new EfFiscalContentSnapshotRepository(context)
            .GetByFiscalDocumentAsync("company-1", fiscalDocumentId)
            ?? throw new InvalidOperationException("Fiscal content snapshot was not persisted.");

        Assert.Equal(snapshotId, stored.Id);
        Assert.Equal("214748364700", stored.Snapshot.Issuer.Ruc);
        Assert.Equal("Empresa Original S.A.", stored.Snapshot.Issuer.LegalName);
        Assert.Equal("0001", stored.Snapshot.Issuer.DgiBranchCode);
        Assert.Equal("Av. Italia 1234", stored.Snapshot.Issuer.FiscalAddress);
        Assert.Equal(1, stored.Snapshot.Issuer.CompanyVersion);
        Assert.Equal(1, stored.Snapshot.Issuer.LocationVersion);

        var receiver = Assert.IsType<FiscalReceiverContentSnapshot>(stored.Snapshot.Receiver);
        Assert.Equal("Cliente Original S.A.", receiver.Name);
        Assert.Equal(1, receiver.PartyVersion);
        Assert.Equal("219999990017", receiver.Identity?.Number);
        Assert.Equal("18 de Julio 2000", receiver.Address?.AddressLine);

        var line = Assert.Single(stored.Snapshot.Lines);
        Assert.Equal("SNAPSHOT-ITEM", line.ItemCode);
        Assert.Equal("Producto fiscal original", line.ItemName);
        Assert.Equal("UNIT", line.UnitOfMeasure);
        Assert.Equal(1m, line.Quantity);
        Assert.Equal(100m, line.UnitPrice);
        Assert.Equal(100m, line.Fiscal.ItemAmount);
        Assert.Equal(VatRateKind.Basic, line.Fiscal.VatRateKind);
        Assert.Equal(22m, line.Fiscal.AppliedRatePercent);
        Assert.Equal(122m, stored.Snapshot.FiscalEvidence.Totals.TotalAmount);
        Assert.Contains(
            stored.Snapshot.FiscalEvidence.ArithmeticRuleEvidence,
            rule => rule.RuleId == "TEST-CFE-RULE");
    }

    private static CfeArithmeticResult Calculation(Guid lineId)
    {
        var rule = RuleEvidence();
        return new CfeArithmeticResult(
            "UYU",
            "25.2",
            "UY-CFE-25.2-ARITH-R1",
            new[]
            {
                new CfeArithmeticLineResult(
                    lineId,
                    100m,
                    VatLiabilityKind.VatDue,
                    VatRateKind.Basic,
                    22m,
                    new[] { rule },
                    "UY-VAT-RATE-R1")
            },
            new CfeArithmeticTotals(100m, 0m, 100m, 0m, 0m, 22m, 22m, 122m),
            new[] { rule });
    }

    private static RegulatoryRuleEvidence RuleEvidence() =>
        new(
            "TEST-CFE-RULE",
            "DGI test evidence",
            "https://example.invalid/dgi-rule",
            "25.2-test",
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "test clause");

    private static SnapshotActorContextAccessor Actor() =>
        new(new ActorContext(
            "system-fiscalizer",
            "Fiscalization Worker",
            true,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "loc-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "term-1" }, StringComparer.Ordinal),
            null));

    private sealed record Seed(Guid PartyId, Guid FiscalizationRequestId);

    private sealed class SnapshotActorContextAccessor : IActorContextAccessor
    {
        public SnapshotActorContextAccessor(ActorContext current) => Current = current;
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
