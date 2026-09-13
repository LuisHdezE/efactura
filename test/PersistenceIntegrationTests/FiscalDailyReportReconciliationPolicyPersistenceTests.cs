using EFactura.Application.Common.Errors;
using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalDailyReportReconciliationPolicyPersistenceTests
{
    private static readonly DateOnly SummaryDate = new(2026, 9, 12);

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_reader_returns_latest_candidates_in_timestamp_order(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRootAsync(database, "receiver-policy-provider");
        var first = Observation(
            root.SubmissionId,
            "policy-provider-dr",
            FiscalDailyReportLaterState.Processed,
            "DR",
            new DateTimeOffset(2026, 9, 13, 2, 10, 0, TimeSpan.Zero));
        var second = Observation(
            root.SubmissionId,
            "policy-provider-fr",
            FiscalDailyReportLaterState.Reliquidated,
            "FR",
            new DateTimeOffset(2026, 9, 13, 2, 11, 0, TimeSpan.Zero));

        await using (var context = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportLaterStateObservationRepository(context);
            await repository.AddAsync(first);
            await repository.AddAsync(second);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var reader = new EfFiscalDailyReportLaterStateObservationRepository(verify);
        var candidates = await reader.GetLatestCandidatesByReceiverIdAsync(
            "company-policy-provider",
            "receiver-policy-provider");

        Assert.Equal(2, candidates.Count);
        Assert.Equal(second.Id, candidates[0].Id);
        Assert.Equal(first.Id, candidates[1].Id);

        var assessment = await new AssessFiscalDailyReportReconciliationUseCase(reader)
            .ExecuteAsync(new("company-policy-provider", "receiver-policy-provider"));
        Assert.Equal(FiscalDailyReportReconciliationDisposition.ReliquidatedExternally, assessment.Disposition);
        Assert.False(assessment.AutomaticReliquidationAuthorized);
        Assert.False(assessment.AutomaticLocalMutationAuthorized);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_equal_latest_timestamps_fail_closed(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRootAsync(database, "receiver-policy-ambiguous");
        var timestamp = new DateTimeOffset(2026, 9, 13, 2, 20, 0, TimeSpan.Zero);
        var first = Observation(
            root.SubmissionId,
            "policy-ambiguous-er",
            FiscalDailyReportLaterState.InManagement,
            "ER",
            timestamp,
            "receiver-policy-ambiguous");
        var second = Observation(
            root.SubmissionId,
            "policy-ambiguous-fr",
            FiscalDailyReportLaterState.Reliquidated,
            "FR",
            timestamp,
            "receiver-policy-ambiguous");

        await using (var context = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportLaterStateObservationRepository(context);
            await repository.AddAsync(first);
            await repository.AddAsync(second);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var reader = new EfFiscalDailyReportLaterStateObservationRepository(verify);
        var candidates = await reader.GetLatestCandidatesByReceiverIdAsync(
            "company-policy-provider",
            "receiver-policy-ambiguous");
        Assert.Equal(2, candidates.Count);
        Assert.Equal(candidates[0].ObservedAtUtc, candidates[1].ObservedAtUtc);

        var useCase = new AssessFiscalDailyReportReconciliationUseCase(reader);
        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new("company-policy-provider", "receiver-policy-ambiguous")));

        Assert.Equal("fiscal.daily_report.reconciliation.latest_observation_ambiguous", error.Code);
    }

    private static StoredFiscalDailyReportLaterStateObservation Observation(
        Guid rootSubmissionId,
        string operationId,
        FiscalDailyReportLaterState state,
        string stateCode,
        DateTimeOffset observedAtUtc,
        string receiverId = "receiver-policy-provider") =>
        new(
            Guid.NewGuid(),
            rootSubmissionId,
            null,
            "company-policy-provider",
            "214748364700",
            SummaryDate,
            1,
            null,
            operationId,
            "emitter-policy-provider",
            receiverId,
            state,
            stateCode,
            "2026-09-13T02:00:00-03:00",
            "<Ackconsultaenviosreporte><ColeccionDatosReporte /></Ackconsultaenviosreporte>",
            new string('e', 64),
            observedAtUtc);

    private static async Task<(Guid SubmissionId, Guid ArtifactId)> SeedRootAsync(
        TestDatabase database,
        string receiverId)
    {
        await using var context = database.CreateContext();
        var evidenceId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var date = SummaryDate.ToDateTime(TimeOnly.MinValue);
        var signingUtc = new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.Zero);

        context.Set<V1FiscalDailyReportSigningEvidenceRecord>().Add(new V1FiscalDailyReportSigningEvidenceRecord
        {
            Id = evidenceId,
            OrganizationId = "company-policy-provider",
            IssuerRuc = "214748364700",
            SummaryDate = date,
            Sequence = 1,
            FunctionalFormatVersion = "13.2",
            ProjectionFingerprint = new string('a', 64),
            UnsignedContentHash = new string('b', 64),
            SigningTimestamp = signingUtc,
            SigningOffsetMinutes = -180
        });
        context.Set<V1FiscalDailyReportSignedArtifactRecord>().Add(new V1FiscalDailyReportSignedArtifactRecord
        {
            Id = artifactId,
            SigningEvidenceId = evidenceId,
            OrganizationId = "company-policy-provider",
            IssuerRuc = "214748364700",
            SummaryDate = date,
            Sequence = 1,
            FunctionalFormatVersion = "13.2",
            ProjectionFingerprint = new string('a', 64),
            UnsignedContentHash = new string('b', 64),
            SignedContentHash = new string('c', 64),
            SigningTimestamp = signingUtc,
            SigningOffsetMinutes = -180,
            SignatureProfileId = "report-sha256",
            CertificateThumbprint = "thumb-policy",
            CertificateSerialNumber = "serial-policy",
            SchemaSetId = "daily-report-v13.2",
            SchemaFunctionalFormatVersion = "13.2",
            SchemaArchiveVersion = "1.44.2",
            SchemaSetFingerprint = new string('d', 64),
            SignedXml = "<Reporte xmlns=\"http://cfe.dgi.gub.uy\" />"
        });
        context.Set<V1FiscalDailyReportSubmissionRecord>().Add(new V1FiscalDailyReportSubmissionRecord
        {
            Id = submissionId,
            SignedArtifactId = artifactId,
            OrganizationId = "company-policy-provider",
            IssuerRuc = "214748364700",
            SummaryDate = date,
            Sequence = 1,
            OperationId = "root-policy-provider-" + receiverId,
            SignedContentHash = new string('c', 64),
            State = (int)FiscalDailyReportSubmissionState.Received,
            AttemptCount = 1,
            PreparedAtUtc = signingUtc,
            LastAttemptAtUtc = signingUtc.AddMinutes(1),
            CompletedAtUtc = signingUtc.AddMinutes(2),
            DgiReceiverId = receiverId,
            AckStateCode = "AR",
            AckXml = $"<ACKRepDiario><IDReceptor>{receiverId}</IDReceptor><Estado>AR</Estado></ACKRepDiario>"
        });
        await context.SaveChangesAsync();
        return (submissionId, artifactId);
    }
}
