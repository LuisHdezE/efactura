using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalDailyReportLaterStateObservationPersistenceTests
{
    private static readonly DateOnly SummaryDate = new(2026, 9, 11);

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Root_later_states_are_append_only_and_do_not_rewrite_transport_state(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRootAsync(database, "receiver-later-root", FiscalDailyReportSubmissionState.Received, "AR");
        var first = Observation(
            root.SubmissionId,
            null,
            null,
            "later-root-dr",
            "receiver-later-root",
            FiscalDailyReportLaterState.Processed,
            "DR",
            new DateTimeOffset(2026, 9, 13, 1, 20, 0, TimeSpan.Zero));
        var second = Observation(
            root.SubmissionId,
            null,
            null,
            "later-root-fr",
            "receiver-later-root",
            FiscalDailyReportLaterState.Reliquidated,
            "FR",
            new DateTimeOffset(2026, 9, 13, 1, 30, 0, TimeSpan.Zero));

        await using (var context = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportLaterStateObservationRepository(context);
            await repository.AddAsync(first);
            await repository.AddAsync(second);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var storedRepository = new EfFiscalDailyReportLaterStateObservationRepository(verify);
        var storedFirst = await storedRepository.GetByOperationIdAsync("company-later-provider", "later-root-dr");
        var storedSecond = await storedRepository.GetByOperationIdAsync("company-later-provider", "later-root-fr");

        Assert.NotNull(storedFirst);
        Assert.NotNull(storedSecond);
        Assert.Equal(FiscalDailyReportLaterState.Processed, storedFirst!.State);
        Assert.Equal("DR", storedFirst.DgiStateCode);
        Assert.Equal(FiscalDailyReportLaterState.Reliquidated, storedSecond!.State);
        Assert.Equal("FR", storedSecond.DgiStateCode);
        Assert.Equal(root.SubmissionId, storedFirst.RootSubmissionId);
        Assert.Equal(root.SubmissionId, storedSecond.RootSubmissionId);
        Assert.Equal(64, storedSecond.EvidenceXmlHash.Length);

        var rows = await verify.Set<V1FiscalDailyReportLaterStateObservationRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == "company-later-provider" && x.DgiReceiverId == "receiver-later-root")
            .OrderBy(x => x.ObservedAtUtc)
            .ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal("DR", rows[0].DgiStateCode);
        Assert.Equal("FR", rows[1].DgiStateCode);

        var rootRow = await verify.Set<V1FiscalDailyReportSubmissionRecord>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == root.SubmissionId);
        Assert.Equal((int)FiscalDailyReportSubmissionState.Received, rootRow.State);
        Assert.Equal("AR", rootRow.AckStateCode);
        Assert.Equal("receiver-later-root", rootRow.DgiReceiverId);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task BR_revision_later_state_round_trips_without_rewriting_revision(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRootAsync(database, "receiver-root-for-later-br", FiscalDailyReportSubmissionState.Rejected, "BR");
        var revisionId = await SeedCorrectionAsync(database, root.SubmissionId, root.ArtifactId, "receiver-later-br");
        var observation = Observation(
            null,
            revisionId,
            2,
            "later-br-er",
            "receiver-later-br",
            FiscalDailyReportLaterState.InManagement,
            "ER",
            new DateTimeOffset(2026, 9, 13, 1, 40, 0, TimeSpan.Zero));

        await using (var context = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportLaterStateObservationRepository(context);
            await repository.AddAsync(observation);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var repositoryVerify = new EfFiscalDailyReportLaterStateObservationRepository(verify);
        var stored = await repositoryVerify.GetByOperationIdAsync("company-later-provider", "later-br-er");

        Assert.NotNull(stored);
        Assert.Null(stored!.RootSubmissionId);
        Assert.Equal(revisionId, stored.BrCorrectionRevisionId);
        Assert.Equal(2, stored.LocalRevision);
        Assert.Equal(FiscalDailyReportLaterState.InManagement, stored.State);
        Assert.Equal("ER", stored.DgiStateCode);

        var revision = await verify.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == revisionId);
        Assert.Equal((int)FiscalDailyReportSubmissionState.Received, revision.State);
        Assert.Equal("AR", revision.AckStateCode);
        Assert.Equal("receiver-later-br", revision.DgiReceiverId);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_unique_operation_rejects_competing_later_state_evidence(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRootAsync(database, "receiver-later-unique", FiscalDailyReportSubmissionState.Received, "AR");
        var first = Observation(
            root.SubmissionId,
            null,
            null,
            "later-same-operation",
            "receiver-later-unique",
            FiscalDailyReportLaterState.Processed,
            "DR",
            new DateTimeOffset(2026, 9, 13, 1, 50, 0, TimeSpan.Zero));
        var competing = first with
        {
            Id = Guid.NewGuid(),
            State = FiscalDailyReportLaterState.InManagement,
            DgiStateCode = "ER",
            EvidenceXmlHash = new string('f', 64),
            ObservedAtUtc = first.ObservedAtUtc.AddMinutes(1)
        };

        await using (var firstContext = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportLaterStateObservationRepository(firstContext);
            await repository.AddAsync(first);
            await firstContext.SaveChangesAsync();
        }

        await using (var competingContext = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportLaterStateObservationRepository(competingContext);
            await repository.AddAsync(competing);
            await Assert.ThrowsAsync<DbUpdateException>(() => competingContext.SaveChangesAsync());
        }

        await using var verify = database.CreateContext();
        var verifyRepository = new EfFiscalDailyReportLaterStateObservationRepository(verify);
        var stored = await verifyRepository.GetByOperationIdAsync("company-later-provider", "later-same-operation");
        Assert.NotNull(stored);
        Assert.Equal(first.Id, stored!.Id);
        Assert.Equal("DR", stored.DgiStateCode);
    }

    private static StoredFiscalDailyReportLaterStateObservation Observation(
        Guid? rootSubmissionId,
        Guid? brRevisionId,
        int? localRevision,
        string operationId,
        string receiverId,
        FiscalDailyReportLaterState state,
        string stateCode,
        DateTimeOffset observedAtUtc) =>
        new(
            Guid.NewGuid(),
            rootSubmissionId,
            brRevisionId,
            "company-later-provider",
            "214748364700",
            SummaryDate,
            1,
            localRevision,
            operationId,
            "emitter-later",
            receiverId,
            state,
            stateCode,
            "2026-09-13T01:00:00-03:00",
            "<Ackconsultaenviosreporte><ColeccionDatosReporte /></Ackconsultaenviosreporte>",
            new string('e', 64),
            observedAtUtc);

    private static async Task<(Guid SubmissionId, Guid ArtifactId)> SeedRootAsync(
        TestDatabase database,
        string receiverId,
        FiscalDailyReportSubmissionState state,
        string ackState)
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
            OrganizationId = "company-later-provider",
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
            OrganizationId = "company-later-provider",
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
            CertificateThumbprint = "thumb-later",
            CertificateSerialNumber = "serial-later",
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
            OrganizationId = "company-later-provider",
            IssuerRuc = "214748364700",
            SummaryDate = date,
            Sequence = 1,
            OperationId = "root-later-provider-" + receiverId,
            SignedContentHash = new string('c', 64),
            State = (int)state,
            AttemptCount = 1,
            PreparedAtUtc = signingUtc,
            LastAttemptAtUtc = signingUtc.AddMinutes(1),
            CompletedAtUtc = signingUtc.AddMinutes(2),
            DgiReceiverId = receiverId,
            AckStateCode = ackState,
            AckXml = $"<ACKRepDiario><IDReceptor>{receiverId}</IDReceptor><Estado>{ackState}</Estado></ACKRepDiario>"
        });
        await context.SaveChangesAsync();
        return (submissionId, artifactId);
    }

    private static async Task<Guid> SeedCorrectionAsync(
        TestDatabase database,
        Guid rootSubmissionId,
        Guid rootArtifactId,
        string receiverId)
    {
        await using var context = database.CreateContext();
        var id = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 12, 21, 0, 0, TimeSpan.Zero);
        context.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>().Add(new V1FiscalDailyReportBrCorrectionRevisionRecord
        {
            Id = id,
            RootSubmissionId = rootSubmissionId,
            RootSignedArtifactId = rootArtifactId,
            PreviousRevisionId = null,
            SigningEvidenceId = Guid.NewGuid(),
            SignedArtifactId = Guid.NewGuid(),
            OrganizationId = "company-later-provider",
            IssuerRuc = "214748364700",
            SummaryDate = SummaryDate.ToDateTime(TimeOnly.MinValue),
            Sequence = 1,
            LocalRevision = 2,
            OperationId = "br-later-provider",
            CorrectionReasonCode = "business-data-corrected",
            SourceAckXmlHash = new string('1', 64),
            SourceAckReasonsJson = "[]",
            FunctionalFormatVersion = "13.2",
            ProjectionFingerprint = new string('2', 64),
            UnsignedContentHash = new string('3', 64),
            SignedContentHash = new string('4', 64),
            SigningTimestamp = now,
            SigningOffsetMinutes = -180,
            SignatureProfileId = "report-sha256",
            CertificateThumbprint = "thumb-br-later",
            CertificateSerialNumber = "serial-br-later",
            SchemaSetId = "daily-report-v13.2",
            SchemaFunctionalFormatVersion = "13.2",
            SchemaArchiveVersion = "1.44.2",
            SchemaSetFingerprint = new string('5', 64),
            SignedXml = "<Reporte xmlns=\"http://cfe.dgi.gub.uy\" />",
            CreatedAtUtc = now,
            RevisionFingerprint = new string('6', 64),
            State = (int)FiscalDailyReportSubmissionState.Received,
            AttemptCount = 1,
            PreparedAtUtc = now,
            LastAttemptAtUtc = now.AddMinutes(1),
            CompletedAtUtc = now.AddMinutes(2),
            DgiReceiverId = receiverId,
            AckStateCode = "AR",
            AckXml = $"<ACKRepDiario><IDReceptor>{receiverId}</IDReceptor><Estado>AR</Estado></ACKRepDiario>",
            AckReasonsJson = null,
            FailureCode = null
        });
        await context.SaveChangesAsync();
        return id;
    }
}
