using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalDailyReportResponseConsultationPersistenceTests
{
    private static readonly DateOnly SummaryDate = new(2026, 9, 11);

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Root_consultation_round_trips_append_only_evidence_and_target_lookup(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRootAsync(database, "receiver-root-cons", FiscalDailyReportSubmissionState.Received, "AR");
        var consultation = Consultation(
            root.SubmissionId,
            null,
            null,
            "consult-root-provider",
            "receiver-root-cons",
            "AR",
            FiscalDailyReportConsultationConsistency.MatchesImmediateAck);

        await using (var context = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportResponseConsultationRepository(context);
            await repository.AddAsync(consultation);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var storedRepository = new EfFiscalDailyReportResponseConsultationRepository(verify);
        var stored = await storedRepository.GetByOperationIdAsync("company-cons-provider", "consult-root-provider");
        var targets = await storedRepository.FindByReceiverIdAsync("company-cons-provider", "receiver-root-cons");

        Assert.NotNull(stored);
        Assert.Equal(root.SubmissionId, stored!.RootSubmissionId);
        Assert.Null(stored.BrCorrectionRevisionId);
        Assert.Equal("AR", stored.AckStateCode);
        Assert.Equal(64, stored.AckXmlHash.Length);
        Assert.Equal(FiscalDailyReportConsultationConsistency.MatchesImmediateAck, stored.Consistency);

        var target = Assert.Single(targets);
        Assert.Equal(FiscalDailyReportConsultationTargetKind.RootSubmission, target.Kind);
        Assert.Equal(root.SubmissionId, target.TargetId);
        Assert.Equal("AR", target.ImmediateAckStateCode);

        var rootRow = await verify.Set<V1FiscalDailyReportSubmissionRecord>().AsNoTracking().SingleAsync();
        Assert.Equal((int)FiscalDailyReportSubmissionState.Received, rootRow.State);
        Assert.Equal("AR", rootRow.AckStateCode);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task BR_revision_consultation_round_trips_without_rewriting_revision(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRootAsync(database, "receiver-root-br-cons", FiscalDailyReportSubmissionState.Rejected, "BR");
        var revisionId = await SeedCorrectionAsync(database, root.SubmissionId, root.ArtifactId, "receiver-revision-cons");
        var consultation = Consultation(
            null,
            revisionId,
            2,
            "consult-br-provider",
            "receiver-revision-cons",
            "BR",
            FiscalDailyReportConsultationConsistency.MatchesImmediateAck);

        await using (var context = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportResponseConsultationRepository(context);
            await repository.AddAsync(consultation);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var repositoryVerify = new EfFiscalDailyReportResponseConsultationRepository(verify);
        var stored = await repositoryVerify.GetByOperationIdAsync("company-cons-provider", "consult-br-provider");
        var targets = await repositoryVerify.FindByReceiverIdAsync("company-cons-provider", "receiver-revision-cons");

        Assert.NotNull(stored);
        Assert.Null(stored!.RootSubmissionId);
        Assert.Equal(revisionId, stored.BrCorrectionRevisionId);
        Assert.Equal(2, stored.LocalRevision);
        Assert.Equal("BR", stored.AckStateCode);

        var target = Assert.Single(targets);
        Assert.Equal(FiscalDailyReportConsultationTargetKind.BrCorrectionRevision, target.Kind);
        Assert.Equal(revisionId, target.TargetId);
        Assert.Equal(2, target.LocalRevision);

        var revision = await verify.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == revisionId);
        Assert.Equal((int)FiscalDailyReportSubmissionState.Rejected, revision.State);
        Assert.Equal("BR", revision.AckStateCode);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_unique_operation_rejects_competing_consultation_evidence(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRootAsync(database, "receiver-unique-cons", FiscalDailyReportSubmissionState.Received, "AR");
        var first = Consultation(
            root.SubmissionId, null, null, "consult-same-operation", "receiver-unique-cons", "AR",
            FiscalDailyReportConsultationConsistency.MatchesImmediateAck);
        var competing = first with { Id = Guid.NewGuid(), AckXmlHash = new string('f', 64) };

        await using (var firstContext = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportResponseConsultationRepository(firstContext);
            await repository.AddAsync(first);
            await firstContext.SaveChangesAsync();
        }

        await using (var competingContext = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportResponseConsultationRepository(competingContext);
            await repository.AddAsync(competing);
            await Assert.ThrowsAsync<DbUpdateException>(() => competingContext.SaveChangesAsync());
        }

        await using var verify = database.CreateContext();
        var rows = await verify.Set<V1FiscalDailyReportResponseConsultationRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == "company-cons-provider" && x.OperationId == "consult-same-operation")
            .ToListAsync();
        Assert.Single(rows);
        Assert.Equal(first.Id, rows[0].Id);
    }

    private static StoredFiscalDailyReportResponseConsultation Consultation(
        Guid? rootSubmissionId,
        Guid? brRevisionId,
        int? localRevision,
        string operationId,
        string receiverId,
        string state,
        FiscalDailyReportConsultationConsistency consistency)
    {
        var ack = Ack(receiverId, state);
        return new StoredFiscalDailyReportResponseConsultation(
            Guid.NewGuid(),
            rootSubmissionId,
            brRevisionId,
            "company-cons-provider",
            "214748364700",
            SummaryDate,
            1,
            localRevision,
            operationId,
            receiverId,
            state,
            ack,
            new string('e', 64),
            consistency,
            new DateTimeOffset(2026, 9, 12, 22, 30, 0, TimeSpan.Zero));
    }

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
            OrganizationId = "company-cons-provider",
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
            OrganizationId = "company-cons-provider",
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
            CertificateThumbprint = "thumb-cons",
            CertificateSerialNumber = "serial-cons",
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
            OrganizationId = "company-cons-provider",
            IssuerRuc = "214748364700",
            SummaryDate = date,
            Sequence = 1,
            OperationId = "root-cons-provider-" + receiverId,
            SignedContentHash = new string('c', 64),
            State = (int)state,
            AttemptCount = 1,
            PreparedAtUtc = signingUtc,
            LastAttemptAtUtc = signingUtc.AddMinutes(1),
            CompletedAtUtc = signingUtc.AddMinutes(2),
            DgiReceiverId = receiverId,
            AckStateCode = ackState,
            AckXml = Ack(receiverId, ackState)
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
            OrganizationId = "company-cons-provider",
            IssuerRuc = "214748364700",
            SummaryDate = SummaryDate.ToDateTime(TimeOnly.MinValue),
            Sequence = 1,
            LocalRevision = 2,
            OperationId = "br-cons-provider",
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
            CertificateThumbprint = "thumb-br-cons",
            CertificateSerialNumber = "serial-br-cons",
            SchemaSetId = "daily-report-v13.2",
            SchemaFunctionalFormatVersion = "13.2",
            SchemaArchiveVersion = "1.44.2",
            SchemaSetFingerprint = new string('5', 64),
            SignedXml = "<Reporte xmlns=\"http://cfe.dgi.gub.uy\" />",
            CreatedAtUtc = now,
            RevisionFingerprint = new string('6', 64),
            State = (int)FiscalDailyReportSubmissionState.Rejected,
            AttemptCount = 1,
            PreparedAtUtc = now,
            LastAttemptAtUtc = now.AddMinutes(1),
            CompletedAtUtc = now.AddMinutes(2),
            DgiReceiverId = receiverId,
            AckStateCode = "BR",
            AckXml = Ack(receiverId, "BR"),
            AckReasonsJson = "[]",
            FailureCode = null
        });
        await context.SaveChangesAsync();
        return id;
    }

    private static string Ack(string receiver, string state) =>
        $"<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDReceptor>{receiver}</IDReceptor></Caratula><Detalle><Estado>{state}</Estado></Detalle></ACKRepDiario>";
}
