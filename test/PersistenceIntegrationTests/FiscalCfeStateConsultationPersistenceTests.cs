using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
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

public sealed class FiscalCfeStateConsultationPersistenceTests
{
    private const string OrganizationId = "company-cfe-state";
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_round_trips_append_only_CFE_state_evidence_without_mutating_document(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var document = await SeedFiscalDocumentAsync(database);
        var evidence = Consultation(document, "state-consult-provider", "AE", "2020", "129");

        await using (var context = database.CreateContext())
        {
            var repository = new EfFiscalCfeStateConsultationRepository(context);
            await repository.AddAsync(evidence);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var stored = await new EfFiscalCfeStateConsultationRepository(verify)
            .GetByOperationIdAsync(OrganizationId, "state-consult-provider");

        Assert.NotNull(stored);
        Assert.Equal(document.Id, stored!.FiscalDocumentId);
        Assert.Equal(document.CfeType, stored.CfeType);
        Assert.Equal(document.Series, stored.Series);
        Assert.Equal(document.Number, stored.Number);
        Assert.Equal("AE", stored.StateCode);
        Assert.Equal("2020", stored.DgiSenderId);
        Assert.Equal("129", stored.DgiReceiverId);
        Assert.Equal("token-provider", stored.ConsultationToken);
        Assert.Equal("2026-09-13T17:00:00", stored.ConsultationAvailableAtText);
        Assert.Equal(64, stored.ResponseSha256.Length);

        var documentRow = await verify.Set<V1FiscalDocumentRecord>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == document.Id);
        Assert.Equal((int)FiscalDocumentStatus.IdentityCreated, documentRow.Status);
        Assert.Equal(document.Series, documentRow.Series);
        Assert.Equal(document.Number, documentRow.Number);
        Assert.Equal(1, await verify.Set<V1FiscalCfeStateConsultationRecord>().CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_unique_operation_rejects_competing_CFE_state_evidence(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var document = await SeedFiscalDocumentAsync(database);
        var first = Consultation(document, "same-state-operation", "AE", "2020", "129");
        var competing = first with
        {
            Id = Guid.NewGuid(),
            StateCode = "DIFFERENT-EXTERNAL-EVIDENCE",
            ResponseSha256 = new string('f', 64)
        };

        await using (var firstContext = database.CreateContext())
        {
            await new EfFiscalCfeStateConsultationRepository(firstContext).AddAsync(first);
            await firstContext.SaveChangesAsync();
        }

        await using (var competingContext = database.CreateContext())
        {
            await new EfFiscalCfeStateConsultationRepository(competingContext).AddAsync(competing);
            await Assert.ThrowsAsync<DbUpdateException>(() => competingContext.SaveChangesAsync());
        }

        await using var verify = database.CreateContext();
        var rows = await verify.Set<V1FiscalCfeStateConsultationRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == OrganizationId && x.OperationId == "same-state-operation")
            .ToListAsync();
        Assert.Single(rows);
        Assert.Equal(first.Id, rows[0].Id);
        Assert.Equal("AE", rows[0].StateCode);
    }

    private static StoredFiscalCfeStateConsultation Consultation(
        FiscalDocument document,
        string operationId,
        string state,
        string senderId,
        string receiverId)
    {
        const string response = "<Ackconsultaestadocfe xmlns=\"http://cfe.dgi.gub.uy\"><EstadoCFE>AE</EstadoCFE><IdEmisor>2020</IdEmisor><IdReceptor>129</IdReceptor><ParamConsulta><Token>token-provider</Token><Fechahora>2026-09-13T17:00:00</Fechahora></ParamConsulta></Ackconsultaestadocfe>";
        return new StoredFiscalCfeStateConsultation(
            Guid.NewGuid(),
            document.Id,
            document.OrganizationId,
            document.CfeType,
            document.Series,
            document.Number,
            operationId,
            state,
            senderId,
            receiverId,
            "token-provider",
            "2026-09-13T17:00:00",
            response,
            new string('e', 64),
            new DateTimeOffset(2026, 9, 13, 16, 45, 0, TimeSpan.Zero));
    }

    private static async Task<FiscalDocument> SeedFiscalDocumentAsync(TestDatabase database)
    {
        var now = new DateTimeOffset(2026, 9, 13, 16, 0, 0, TimeSpan.Zero);
        var saleId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        await using (var context = database.CreateContext())
        {
            await new EfCommercialItemRepository(context).AddAsync(
                CommercialItem.Create(
                    itemId,
                    OrganizationId,
                    "STATE-ITEM",
                    "CFE state consultation item",
                    null,
                    CommercialItemKind.Product,
                    "UNIT",
                    false,
                    null,
                    null));

            var sale = Sale.Create(
                saleId,
                OrganizationId,
                "loc-state",
                "term-state",
                null,
                SaleCommercialIntent.ConsumerSale,
                "UYU",
                new DateOnly(2026, 9, 13),
                "UY",
                false,
                new[]
                {
                    SaleLine.Create(
                        Guid.NewGuid(),
                        itemId,
                        "STATE-ITEM",
                        "CFE state consultation item",
                        SaleLineKind.Product,
                        1m,
                        100m,
                        null)
                });
            sale.MarkValidated("validated-cfe-state", now, 1);
            sale.MarkConfirmed(Confirmation, Settlement, now, 2);
            await new EfSaleRepository(context).AddAsync(sale);

            await new EfFiscalizationRequestRepository(context).AddAsync(
                FiscalizationRequest.CreateFromSale(
                    requestId,
                    OrganizationId,
                    saleId,
                    "loc-state",
                    "term-state",
                    CfeFamily.ETicket,
                    null,
                    "25.2",
                    Confirmation,
                    Settlement,
                    "UYU",
                    100m,
                    22m,
                    122m,
                    now));

            var cae = CaeAuthorization.ImportVerified(
                OrganizationId,
                CfeFamily.ETicket,
                "CAE-STATE-001",
                "A",
                123,
                200,
                new DateOnly(2026, 1, 1),
                new DateOnly(2027, 12, 31),
                "integration-test",
                "artifact-state-001",
                new string('c', 64),
                "DGI test fixture",
                "https://example.invalid/cae-state",
                now);
            cae.Activate(new DateOnly(2026, 9, 13), cae.Version, now);
            await new EfCaeRepository(context).AddAuthorizationAsync(cae);
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var identity = IdentityUseCase(context);
            await identity.ExecuteAsync(new PrepareFiscalDocumentIdentityCommand(OrganizationId, requestId));
        }

        await using var verify = database.CreateContext();
        return (await new EfFiscalDocumentRepository(verify).GetByFiscalizationRequestAsync(
            OrganizationId,
            requestId))!;
    }

    private static PrepareFiscalDocumentIdentityUseCase IdentityUseCase(V1PersistenceDbContext context)
    {
        var actor = new FixedActorContextAccessor(new ActorContext(
            "system-cfe-state",
            "CFE State Test",
            true,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(new[] { OrganizationId }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "loc-state" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "term-state" }, StringComparer.Ordinal),
            null));
        var correlation = new FixedCorrelationContextAccessor(
            new CorrelationContext("corr-cfe-state", "trace-cfe-state"));
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

    private sealed class FixedActorContextAccessor(ActorContext current) : IActorContextAccessor
    {
        public ActorContext Current { get; } = current;
    }
}
