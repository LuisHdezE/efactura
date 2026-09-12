using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260912143000_V1FiscalDailyReportBrSameSequence")]
public sealed class V1FiscalDailyReportBrSameSequence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_daily_report_br_revisions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                RootSubmissionId = table.Column<Guid>(nullable: false),
                RootSignedArtifactId = table.Column<Guid>(nullable: false),
                PreviousRevisionId = table.Column<Guid>(nullable: true),
                SigningEvidenceId = table.Column<Guid>(nullable: false),
                SignedArtifactId = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                SummaryDate = table.Column<DateTime>(type: "date", nullable: false),
                Sequence = table.Column<int>(nullable: false),
                LocalRevision = table.Column<int>(nullable: false),
                OperationId = table.Column<string>(maxLength: 120, nullable: false),
                CorrectionReasonCode = table.Column<string>(maxLength: 120, nullable: false),
                SourceAckXmlHash = table.Column<string>(maxLength: 64, nullable: false),
                SourceAckReasonsJson = table.Column<string>(nullable: false),
                FunctionalFormatVersion = table.Column<string>(maxLength: 40, nullable: false),
                ProjectionFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                UnsignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                SignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                SigningTimestamp = table.Column<DateTimeOffset>(precision: 0, nullable: false),
                SigningOffsetMinutes = table.Column<int>(nullable: false),
                SignatureProfileId = table.Column<string>(maxLength: 120, nullable: false),
                CertificateThumbprint = table.Column<string>(maxLength: 160, nullable: false),
                CertificateSerialNumber = table.Column<string>(maxLength: 160, nullable: false),
                SchemaSetId = table.Column<string>(maxLength: 120, nullable: false),
                SchemaFunctionalFormatVersion = table.Column<string>(maxLength: 40, nullable: false),
                SchemaArchiveVersion = table.Column<string>(maxLength: 40, nullable: false),
                SchemaSetFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                SignedXml = table.Column<string>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false),
                RevisionFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                State = table.Column<int>(nullable: false),
                AttemptCount = table.Column<int>(nullable: false),
                PreparedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false),
                LastAttemptAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: true),
                CompletedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: true),
                DgiReceiverId = table.Column<string>(maxLength: 120, nullable: true),
                AckStateCode = table.Column<string>(maxLength: 8, nullable: true),
                AckXml = table.Column<string>(nullable: true),
                AckReasonsJson = table.Column<string>(nullable: true),
                FailureCode = table.Column<string>(maxLength: 160, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fdr_br_revisions", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fdr_br_revision_root_submission",
                    column: x => x.RootSubmissionId,
                    principalTable: "v1_fiscal_daily_report_submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fdr_br_revision_root_artifact",
                    column: x => x.RootSignedArtifactId,
                    principalTable: "v1_fiscal_daily_report_signed_artifacts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fdr_br_revision_previous",
                    column: x => x.PreviousRevisionId,
                    principalTable: "v1_fiscal_daily_report_br_revisions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_br_revision_operation",
            table: "v1_fiscal_daily_report_br_revisions",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_br_revision_identity",
            table: "v1_fiscal_daily_report_br_revisions",
            columns: new[] { "OrganizationId", "IssuerRuc", "SummaryDate", "Sequence", "LocalRevision" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_br_revision_previous",
            table: "v1_fiscal_daily_report_br_revisions",
            column: "PreviousRevisionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_fdr_br_revision_root_submission",
            table: "v1_fiscal_daily_report_br_revisions",
            column: "RootSubmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_fdr_br_revision_root_artifact",
            table: "v1_fiscal_daily_report_br_revisions",
            column: "RootSignedArtifactId");

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_br_revision_signed_artifact",
            table: "v1_fiscal_daily_report_br_revisions",
            column: "SignedArtifactId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_br_revision_signing_evidence",
            table: "v1_fiscal_daily_report_br_revisions",
            column: "SigningEvidenceId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fiscal_daily_report_br_revisions");
}
