using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalCfeEnvelopeDocumentResponseConsultationPersistenceTests
{
    private const string OrganizationId = "company-ackcfe";

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_round_trips_append_only_ACKCFE_evidence_without_rewriting_sources(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var source = await SeedSourceAsync(database);
        var evidence = Evidence(source, "ackcfe-provider-op");

        await using (var context = database.CreateContext())
        {
            await new EfFiscalCfeEnvelopeDocumentResponseConsultationRepository(context).AddAsync(evidence);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var stored = await new EfFiscalCfeEnvelopeDocumentResponseConsultationRepository(verify)
            .GetByOperationAsync(OrganizationId, "ackcfe-provider-op");

        Assert.NotNull(stored);
        Assert.Equal(source.AckObservationId, stored!.AckObservationId);
        Assert.Equal(source.SubmissionId, stored.SubmissionId);
        Assert.Equal(source.EnvelopeId, stored.EnvelopeId);
        Assert.Equal(1516, stored.DgiReceiverId);
        Assert.Equal(3009, stored.SenderEnvelopeId);
        Assert.Equal(2, stored.EnvelopeCfeCount);
        Assert.Equal(1, stored.RespondedCount);
        Assert.Contains("\"StateCode\":\"A\"", stored.DetailsJson, StringComparison.Ordinal);
        Assert.Equal(64, stored.ResponseSha256.Length);

        var envelope = await verify.Set<V1FiscalCfeEnvelopeRecord>().AsNoTracking().SingleAsync(x => x.Id == source.EnvelopeId);
        var submission = await verify.Set<V1FiscalCfeEnvelopeSubmissionRecord>().AsNoTracking().SingleAsync(x => x.Id == source.SubmissionId);
        var observation = await verify.Set<V1FiscalCfeEnvelopeAckObservationRecord>().AsNoTracking().SingleAsync(x => x.Id == source.AckObservationId);
        Assert.Equal("envelope-source-op", envelope.OperationId);
        Assert.Equal((int)FiscalCfeEnvelopeSubmissionState.ResponseReceived, submission.State);
        Assert.Equal((int)FiscalCfeEnvelopeAckState.Received, observation.State);
        Assert.Equal("token-provider", observation.ConsultationToken);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_unique_operation_rejects_competing_ACKCFE_evidence(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var source = await SeedSourceAsync(database);
        var first = Evidence(source, "same-ackcfe-op");
        var competing = first with
        {
            Id = Guid.NewGuid(),
            DgiResponseId = 1517,
            ResponseXml = "<ACKCFE xmlns=\"http://cfe.dgi.gub.uy\"><different /></ACKCFE>",
            ResponseSha256 = new string('f', 64)
        };

        await using (var firstContext = database.CreateContext())
        {
            await new EfFiscalCfeEnvelopeDocumentResponseConsultationRepository(firstContext).AddAsync(first);
            await firstContext.SaveChangesAsync();
        }

        await using (var competingContext = database.CreateContext())
        {
            await new EfFiscalCfeEnvelopeDocumentResponseConsultationRepository(competingContext).AddAsync(competing);
            await Assert.ThrowsAsync<DbUpdateException>(() => competingContext.SaveChangesAsync());
        }

        await using var verify = database.CreateContext();
        var rows = await verify.Set<V1FiscalCfeDocumentResponseConsultationRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == OrganizationId && x.OperationId == "same-ackcfe-op")
            .ToListAsync();
        Assert.Single(rows);
        Assert.Equal(first.Id, rows[0].Id);
        Assert.Equal(1516, rows[0].DgiResponseId);
    }

    private static StoredFiscalCfeEnvelopeDocumentResponseConsultation Evidence(SourceIds source, string operationId)
    {
        const string response = "<ACKCFE xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDRespuesta>1516</IDRespuesta></Caratula><ACKCFE_det ordinal=\"1\"><TipoCFE>101</TipoCFE><Serie>A</Serie><NroCFE>123</NroCFE><Estado>A</Estado></ACKCFE_det></ACKCFE>";
        return new StoredFiscalCfeEnvelopeDocumentResponseConsultation(
            Guid.NewGuid(),
            source.AckObservationId,
            source.SubmissionId,
            source.EnvelopeId,
            OrganizationId,
            operationId,
            new string('b', 64),
            1516,
            new string('c', 64),
            1516,
            "219999820013",
            "214844360018",
            3009,
            2,
            1,
            1,
            0,
            0,
            0,
            "[{\"Ordinal\":1,\"CfeType\":101,\"Series\":\"A\",\"Number\":123,\"StateCode\":\"A\"}]",
            response,
            new string('d', 64),
            new DateTimeOffset(2026, 9, 13, 22, 30, 0, TimeSpan.Zero));
    }

    private static async Task<SourceIds> SeedSourceAsync(TestDatabase database)
    {
        var envelopeId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var observationId = Guid.NewGuid();

        await using var context = database.CreateContext();
        context.Set<V1FiscalCfeEnvelopeRecord>().Add(new V1FiscalCfeEnvelopeRecord
        {
            Id = envelopeId,
            OrganizationId = OrganizationId,
            ReceiverRut = "214844360018",
            IssuerRuc = "219999820013",
            SenderEnvelopeId = 3009,
            CreatedAtUtc = new DateTimeOffset(2026, 9, 13, 22, 0, 0, TimeSpan.Zero),
            CreatedAtOffsetMinutes = -180,
            FiscalDocumentIdsJson = "[\"11111111-1111-1111-1111-111111111111\",\"22222222-2222-2222-2222-222222222222\"]",
            OperationId = "envelope-source-op",
            CfeCount = 2,
            CertificateThumbprint = "thumbprint-provider",
            CertificateSerialNumber = "serial-provider",
            EnvelopeXml = "<EnvioCFE />",
            EnvelopeSha256 = new string('a', 64),
            SchemaSetId = "dgi-fe-v1.44.2",
            SchemaVersion = "05",
            SchemaSetFingerprint = new string('1', 64)
        });
        context.Set<V1FiscalCfeEnvelopeSubmissionRecord>().Add(new V1FiscalCfeEnvelopeSubmissionRecord
        {
            Id = submissionId,
            EnvelopeId = envelopeId,
            OrganizationId = OrganizationId,
            IssuerRuc = "219999820013",
            ReceiverRut = "214844360018",
            SenderEnvelopeId = 3009,
            OperationId = "submission-source-op",
            EnvelopeSha256 = new string('a', 64),
            State = (int)FiscalCfeEnvelopeSubmissionState.ResponseReceived,
            AttemptCount = 1,
            PreparedAtUtc = new DateTimeOffset(2026, 9, 13, 22, 1, 0, TimeSpan.Zero),
            LastAttemptAtUtc = new DateTimeOffset(2026, 9, 13, 22, 2, 0, TimeSpan.Zero),
            CompletedAtUtc = new DateTimeOffset(2026, 9, 13, 22, 2, 1, TimeSpan.Zero),
            ResponseXml = "<ACKSobre />",
            ResponseSha256 = new string('b', 64)
        });
        context.Set<V1FiscalCfeEnvelopeAckObservationRecord>().Add(new V1FiscalCfeEnvelopeAckObservationRecord
        {
            Id = observationId,
            SubmissionId = submissionId,
            EnvelopeId = envelopeId,
            OrganizationId = OrganizationId,
            IssuerRuc = "219999820013",
            ReceiverRut = "214844360018",
            SenderEnvelopeId = 3009,
            ResponseSha256 = new string('b', 64),
            DgiResponseId = 1516,
            DgiReceiverId = 1516,
            CfeCount = 2,
            State = (int)FiscalCfeEnvelopeAckState.Received,
            ReceptionTimestampText = "2026-09-13T19:02:00-03:00",
            SigningTimestampText = "2026-09-13T19:02:01-03:00",
            ConsultationToken = "token-provider",
            ConsultationAvailableAtText = "2026-09-13T19:02:02-03:00",
            RejectionReasonsJson = "[]",
            ObservedAtUtc = new DateTimeOffset(2026, 9, 13, 22, 3, 0, TimeSpan.Zero)
        });
        await context.SaveChangesAsync();
        return new(envelopeId, submissionId, observationId);
    }

    private sealed record SourceIds(Guid EnvelopeId, Guid SubmissionId, Guid AckObservationId);
}
