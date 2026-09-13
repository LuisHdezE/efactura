using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalDailyReportReceiverDiscoveryPersistenceTests
{
    private static readonly DateOnly SummaryDate = new(2026, 9, 11);

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Root_discovery_round_trips_and_becomes_available_to_original_ACK_consultation(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRootAsync(database, 1, FiscalDailyReportSubmissionState.Unknown, null, null);
        var discovery = Discovery(root.SubmissionId, null, null, "discover-root-provider", "receiver-discovered");

        await using (var context = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportReceiverDiscoveryRepository(context);
            await repository.AddAsync(discovery);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var repositoryVerify = new EfFiscalDailyReportReceiverDiscoveryRepository(verify);
        var stored = await repositoryVerify.GetByOperationIdAsync("company-disc-provider", "discover-root-provider");
        var target = await repositoryVerify.GetTargetAsync(
            "company-disc-provider",
            FiscalDailyReportConsultationTargetKind.RootSubmission,
            root.SubmissionId);
        var known = await repositoryVerify.GetKnownReceiverIdsAsync(
            "company-disc-provider",
            "214748364700",
            SummaryDate,
            1);
        var consultationTargets = await new EfFiscalDailyReportResponseConsultationRepository(verify)
            .FindByReceiverIdAsync("company-disc-provider", "receiver-discovered");

        Assert.NotNull(stored);
        Assert.Equal(root.SubmissionId, stored!.RootSubmissionId);
        Assert.Equal("receiver-discovered", stored.DgiReceiverId);
        Assert.Equal(64, stored.EvidenceXmlHash.Length);
        Assert.NotNull(target);
        Assert.Equal(FiscalDailyReportSubmissionState.Unknown, target!.State);
        Assert.Null(target.DgiReceiverId);
        Assert.Contains("receiver-discovered", known);

        var consultationTarget = Assert.Single(consultationTargets);
        Assert.Equal(FiscalDailyReportConsultationTargetKind.RootSubmission, consultationTarget.Kind);
        Assert.Equal(root.SubmissionId, consultationTarget.TargetId);
        Assert.Equal("receiver-discovered", consultationTarget.DgiReceiverId);
        Assert.Null(consultationTarget.ImmediateAckStateCode);

        var rootRow = await verify.Set<V1FiscalDailyReportSubmissionRecord>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == root.SubmissionId);
        Assert.Equal((int)FiscalDailyReportSubmissionState.Unknown, rootRow.State);
        Assert.Null(rootRow.DgiReceiverId);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task BR_revision_discovery_round_trips_without_rewriting_revision(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRootAsync(database, 1, FiscalDailyReportSubmissionState.Rejected, "receiver-root-br", "BR");
        var revisionId = await SeedCorrectionAsync(database, root.SubmissionId, root.ArtifactId);
        var discovery = Discovery(null, revisionId, 2, "discover-br-provider", "receiver-br-discovered");

        await using (var context = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportReceiverDiscoveryRepository(context);
            await repository.AddAsync(discovery);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var repositoryVerify = new EfFiscalDailyReportReceiverDiscoveryRepository(verify);
        var stored = await repositoryVerify.GetByTargetAsync(
            "company-disc-provider",
            FiscalDailyReportConsultationTargetKind.BrCorrectionRevision,
            revisionId);
        var consultationTargets = await new EfFiscalDailyReportResponseConsultationRepository(verify)
            .FindByReceiverIdAsync("company-disc-provider", "receiver-br-discovered");

        Assert.NotNull(stored);
        Assert.Equal(revisionId, stored!.BrCorrectionRevisionId);
        Assert.Equal(2, stored.LocalRevision);
        var consultationTarget = Assert.Single(consultationTargets);
        Assert.Equal(revisionId, consultationTarget.TargetId);
        Assert.Equal(2, consultationTarget.LocalRevision);

        var revision = await verify.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == revisionId);
        Assert.Equal((int)FiscalDailyReportSubmissionState.Unknown, revision.State);
        Assert.Null(revision.DgiReceiverId);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_constraints_reject_second_discovery_for_same_target(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRootAsync(database, 1, FiscalDailyReportSubmissionState.Unknown, null, null);
        var first = Discovery(root.SubmissionId, null, null, "discover-first", "receiver-first");
        var competing = Discovery(root.SubmissionId, null, null, "discover-second", "receiver-second");

        await using (var firstContext = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportReceiverDiscoveryRepository(firstContext);
            await repository.AddAsync(first);
            await firstContext.SaveChangesAsync();
        }

        await using (var competingContext = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportReceiverDiscoveryRepository(competingContext);
            await repository.AddAsync(competing);
            await Assert.ThrowsAsync<DbUpdateException>(() => competingContext.SaveChangesAsync());
        }

        await using var verify = database.CreateContext();
        var rows = await verify.Set<V1FiscalDailyReportReceiverDiscoveryRecord>()
            .AsNoTracking()
            .Where(x => x.RootSubmissionId == root.SubmissionId)
            .ToListAsync();
        Assert.Single(rows);
        Assert.Equal(first.Id, rows[0].Id);
    }

    private static StoredFiscalDailyReportReceiverDiscovery Discovery(
        Guid? rootSubmissionId,
        Guid? brRevisionId,
        int? localRevision,
        string operationId,
        string receiverId) =>
        new(
            Guid.NewGuid(),
            rootSubmissionId,
            brRevisionId,
            "company-disc-provider",
            "214748364700",
            SummaryDate,
            1,
            localRevision,
            operationId,
            "emitter-provider",
            receiverId,
            "AR",
            "2026-09-12T20:00:00",
            $"<Ackconsultaenviosreporte><ColeccionDatosReporte><DatosReporte><IdEmisor>emitter-provider</IdEmisor><IdReceptor>{receiverId}</IdReceptor><Estado>AR</Estado><FechaHoraRecepcion>2026-09-12T20:00:00</FechaHoraRecepcion></DatosReporte></ColeccionDatosReporte></Ackconsultaenviosreporte>",
            new string('e', 64),
            new DateTimeOffset(2026, 9, 13, 0, 30, 0, TimeSpan.Zero));

    private static async Task<(Guid SubmissionId, Guid ArtifactId)> SeedRootAsync(
        TestDatabase database,
        int sequence,
        FiscalDailyReportSubmissionState state,
        string? receiverId,
        string? ackState)
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
            OrganizationId = "company-disc-provider",
            IssuerRuc = "214748364700",
            SummaryDate = date,
            Sequence = sequence,
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
            OrganizationId = "company-disc-provider",
            IssuerRuc = "214748364700",
            SummaryDate = date,
            Sequence = sequence,
            FunctionalFormatVersion = "13.2",
            ProjectionFingerprint = new string('a', 64),
            UnsignedContentHash = new string('b', 64),
            SignedContentHash = new string('c', 64),
            SigningTimestamp = signingUtc,
            SigningOffsetMinutes = -180,
            SignatureProfileId = "report-sha256",
            CertificateThumbprint = "thumb-disc",
            CertificateSerialNumber = "serial-disc",
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
            OrganizationId = "company-disc-provider",
            IssuerRuc = "214748364700",
            SummaryDate = date,
            Sequence = sequence,
            OperationId = "root-disc-provider-" + sequence,
            SignedContentHash = new string('c', 64),
            State = (int)state,
            AttemptCount = 1,
            PreparedAtUtc = signingUtc,
            LastAttemptAtUtc = signingUtc.AddMinutes(1),
            CompletedAtUtc = state is FiscalDailyReportSubmissionState.Received or FiscalDailyReportSubmissionState.Rejected
                ? signingUtc.AddMinutes(2)
                : null,
            DgiReceiverId = receiverId,
            AckStateCode = ackState,
            AckXml = receiverId is null || ackState is null ? null : Ack(receiverId, ackState),
            FailureCode = state == FiscalDailyReportSubmissionState.Unknown ? "transport_ambiguous" : null
        });
        await context.SaveChangesAsync();
        return (submissionId, artifactId);
    }

    private static async Task<Guid> SeedCorrectionAsync(
        TestDatabase database,
        Guid rootSubmissionId,
        Guid rootArtifactId)
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
            OrganizationId = "company-disc-provider",
            IssuerRuc = "214748364700",
            SummaryDate = SummaryDate.ToDateTime(TimeOnly.MinValue),
            Sequence = 1,
            LocalRevision = 2,
            OperationId = "br-disc-provider",
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
            CertificateThumbprint = "thumb-br-disc",
            CertificateSerialNumber = "serial-br-disc",
            SchemaSetId = "daily-report-v13.2",
            SchemaFunctionalFormatVersion = "13.2",
            SchemaArchiveVersion = "1.44.2",
            SchemaSetFingerprint = new string('5', 64),
            SignedXml = "<Reporte xmlns=\"http://cfe.dgi.gub.uy\" />",
            CreatedAtUtc = now,
            RevisionFingerprint = new string('6', 64),
            State = (int)FiscalDailyReportSubmissionState.Unknown,
            AttemptCount = 1,
            PreparedAtUtc = now,
            LastAttemptAtUtc = now.AddMinutes(1),
            CompletedAtUtc = null,
            DgiReceiverId = null,
            AckStateCode = null,
            AckXml = null,
            AckReasonsJson = null,
            FailureCode = "transport_ambiguous"
        });
        await context.SaveChangesAsync();
        return id;
    }

    private static string Ack(string receiver, string state) =>
        $"<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDReceptor>{receiver}</IDReceptor></Caratula><Detalle><Estado>{state}</Estado></Detalle></ACKRepDiario>";
}
