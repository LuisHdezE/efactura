using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalDailyReportBrCorrectionPersistenceTests
{
    private static readonly DateOnly SummaryDate = new(2026, 9, 11);
    private static readonly DateTimeOffset FiscalSigningTimestamp =
        new(2026, 9, 12, 15, 30, 0, TimeSpan.FromHours(-3));

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Correction_revision_round_trips_immutable_evidence_and_Uruguay_signing_offset(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRejectedRootAsync(database);
        var revision = Revision(root.SubmissionId, root.ArtifactId, "br-provider-roundtrip", FiscalDailyReportSubmissionState.Received, "AR");

        await using (var context = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportBrCorrectionRepository(context);
            await repository.AddAsync(revision);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var storedRepository = new EfFiscalDailyReportBrCorrectionRepository(verify);
        var stored = await storedRepository.GetByRevisionAsync(
            "company-br-provider",
            "214748364700",
            SummaryDate,
            1,
            2);

        Assert.NotNull(stored);
        Assert.Equal(root.SubmissionId, stored!.RootSubmissionId);
        Assert.Equal(root.ArtifactId, stored.RootSignedArtifactId);
        Assert.Equal(2, stored.LocalRevision);
        Assert.Equal(FiscalSigningTimestamp, stored.SigningTimestamp);
        Assert.Equal(revision.SourceAckXmlHash, stored.SourceAckXmlHash);
        Assert.Equal(revision.SourceAckReasonsJson, stored.SourceAckReasonsJson);
        Assert.Equal(revision.SignedContentHash, stored.SignedContentHash);
        Assert.Equal(revision.RevisionFingerprint, stored.RevisionFingerprint);
        Assert.Equal(FiscalDailyReportSubmissionState.Received, stored.State);
        Assert.Equal("AR", stored.AckStateCode);
        Assert.True(await storedRepository.HasReceivedCorrectionAsync(
            "company-br-provider",
            "214748364700",
            SummaryDate,
            1));

        var row = await verify.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(TimeSpan.Zero, row.SigningTimestamp.Offset);
        Assert.Equal(-180, row.SigningOffsetMinutes);
        Assert.Equal(root.SubmissionId, row.RootSubmissionId);
        Assert.Equal(root.ArtifactId, row.RootSignedArtifactId);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Provider_unique_identity_rejects_two_revision_two_branches_for_same_DGI_identity(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var root = await SeedRejectedRootAsync(database);
        var first = Revision(root.SubmissionId, root.ArtifactId, "br-provider-fork-a", FiscalDailyReportSubmissionState.Prepared, null);
        var competing = Revision(root.SubmissionId, root.ArtifactId, "br-provider-fork-b", FiscalDailyReportSubmissionState.Prepared, null);

        await using (var firstContext = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportBrCorrectionRepository(firstContext);
            await repository.AddAsync(first);
            await firstContext.SaveChangesAsync();
        }

        await using (var competingContext = database.CreateContext())
        {
            var repository = new EfFiscalDailyReportBrCorrectionRepository(competingContext);
            await repository.AddAsync(competing);
            await Assert.ThrowsAsync<DbUpdateException>(() => competingContext.SaveChangesAsync());
        }

        await using var verify = database.CreateContext();
        var rows = await verify.Set<V1FiscalDailyReportBrCorrectionRevisionRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == "company-br-provider"
                && x.IssuerRuc == "214748364700"
                && x.SummaryDate == SummaryDate.ToDateTime(TimeOnly.MinValue)
                && x.Sequence == 1
                && x.LocalRevision == 2)
            .ToListAsync();
        Assert.Single(rows);
        Assert.Equal(first.Id, rows[0].Id);
    }

    private static StoredFiscalDailyReportBrCorrectionRevision Revision(
        Guid rootSubmissionId,
        Guid rootArtifactId,
        string operationId,
        FiscalDailyReportSubmissionState state,
        string? ackStateCode)
    {
        const string sourceAck = "<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Detalle><Estado>BR</Estado><MotivosRechazo><Motivo>R04</Motivo><Glosa>No cumple validaciones según Formato de Reporte</Glosa></MotivosRechazo></Detalle></ACKRepDiario>";
        const string signedXml = "<Reporte xmlns=\"http://cfe.dgi.gub.uy\"><Caratula /><Signature /></Reporte>";
        var reasonsJson = FiscalDailyReportRejectionReasonEvidence.Serialize([
            new FiscalDailyReportRejectionReason("R04", "No cumple validaciones según Formato de Reporte", null)
        ]);
        var now = new DateTimeOffset(2026, 9, 12, 18, 30, 0, TimeSpan.Zero);

        var draft = new StoredFiscalDailyReportBrCorrectionRevision(
            Guid.NewGuid(),
            rootSubmissionId,
            rootArtifactId,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "company-br-provider",
            "214748364700",
            SummaryDate,
            1,
            2,
            operationId,
            "business-data-corrected",
            Sha256(sourceAck),
            reasonsJson,
            "13.2",
            new string('a', 64),
            new string('b', 64),
            Sha256(signedXml),
            FiscalSigningTimestamp,
            "dgi-daily-report-sha256-evidence-backed-v1",
            "thumbprint-br-provider",
            "serial-br-provider",
            "dgi-daily-report-v13.2-xsd-v1.44.2-signed",
            "13.2",
            "1.44.2",
            new string('d', 64),
            signedXml,
            now,
            new string('0', 64),
            state,
            state == FiscalDailyReportSubmissionState.Prepared ? 0 : 1,
            now,
            state == FiscalDailyReportSubmissionState.Prepared ? null : now,
            state == FiscalDailyReportSubmissionState.Prepared ? null : now,
            state == FiscalDailyReportSubmissionState.Prepared ? null : "receiver-br-provider",
            ackStateCode,
            ackStateCode is null ? null : "<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Detalle><Estado>AR</Estado></Detalle></ACKRepDiario>",
            null,
            null);

        return draft with { RevisionFingerprint = draft.ComputeFingerprint() };
    }

    private static async Task<(Guid SubmissionId, Guid ArtifactId)> SeedRejectedRootAsync(TestDatabase database)
    {
        await using var context = database.CreateContext();
        var signingEvidenceId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var date = SummaryDate.ToDateTime(TimeOnly.MinValue);
        var preparedAt = new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.Zero);

        context.Set<V1FiscalDailyReportSigningEvidenceRecord>().Add(new V1FiscalDailyReportSigningEvidenceRecord
        {
            Id = signingEvidenceId,
            OrganizationId = "company-br-provider",
            IssuerRuc = "214748364700",
            SummaryDate = date,
            Sequence = 1,
            FunctionalFormatVersion = "13.2",
            ProjectionFingerprint = new string('a', 64),
            UnsignedContentHash = new string('b', 64),
            SigningTimestamp = FiscalSigningTimestamp.ToUniversalTime(),
            SigningOffsetMinutes = -180
        });
        context.Set<V1FiscalDailyReportSignedArtifactRecord>().Add(new V1FiscalDailyReportSignedArtifactRecord
        {
            Id = artifactId,
            SigningEvidenceId = signingEvidenceId,
            OrganizationId = "company-br-provider",
            IssuerRuc = "214748364700",
            SummaryDate = date,
            Sequence = 1,
            FunctionalFormatVersion = "13.2",
            ProjectionFingerprint = new string('a', 64),
            UnsignedContentHash = new string('b', 64),
            SignedContentHash = new string('c', 64),
            SigningTimestamp = FiscalSigningTimestamp.ToUniversalTime(),
            SigningOffsetMinutes = -180,
            SignatureProfileId = "dgi-daily-report-sha256-evidence-backed-v1",
            CertificateThumbprint = "root-thumbprint",
            CertificateSerialNumber = "root-serial",
            SchemaSetId = "dgi-daily-report-v13.2-xsd-v1.44.2-signed",
            SchemaFunctionalFormatVersion = "13.2",
            SchemaArchiveVersion = "1.44.2",
            SchemaSetFingerprint = new string('d', 64),
            SignedXml = "<Reporte xmlns=\"http://cfe.dgi.gub.uy\" />"
        });
        context.Set<V1FiscalDailyReportSubmissionRecord>().Add(new V1FiscalDailyReportSubmissionRecord
        {
            Id = submissionId,
            SignedArtifactId = artifactId,
            OrganizationId = "company-br-provider",
            IssuerRuc = "214748364700",
            SummaryDate = date,
            Sequence = 1,
            OperationId = "root-br-provider",
            SignedContentHash = new string('c', 64),
            State = (int)FiscalDailyReportSubmissionState.Rejected,
            AttemptCount = 1,
            PreparedAtUtc = preparedAt,
            LastAttemptAtUtc = preparedAt.AddMinutes(1),
            CompletedAtUtc = preparedAt.AddMinutes(2),
            DgiReceiverId = "root-receiver",
            AckStateCode = "BR",
            AckXml = "<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Detalle><Estado>BR</Estado><MotivosRechazo><Motivo>R04</Motivo><Glosa>No cumple validaciones según Formato de Reporte</Glosa></MotivosRechazo></Detalle></ACKRepDiario>"
        });

        await context.SaveChangesAsync();
        return (submissionId, artifactId);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
