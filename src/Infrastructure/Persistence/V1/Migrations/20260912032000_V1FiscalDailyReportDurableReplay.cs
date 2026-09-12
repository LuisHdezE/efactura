using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260912032000_V1FiscalDailyReportDurableReplay")]
public sealed class V1FiscalDailyReportDurableReplay : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_daily_report_signing_evidence",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                SummaryDate = table.Column<DateTime>(type: "date", nullable: false),
                Sequence = table.Column<int>(nullable: false),
                FunctionalFormatVersion = table.Column<string>(maxLength: 40, nullable: false),
                ProjectionFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                UnsignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                SigningTimestamp = table.Column<DateTimeOffset>(precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fdr_signing_evidence", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_evidence_identity",
            table: "v1_fiscal_daily_report_signing_evidence",
            columns: new[] { "OrganizationId", "IssuerRuc", "SummaryDate", "Sequence" },
            unique: true);

        migrationBuilder.CreateTable(
            name: "v1_fiscal_daily_report_signed_artifacts",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                SigningEvidenceId = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                SummaryDate = table.Column<DateTime>(type: "date", nullable: false),
                Sequence = table.Column<int>(nullable: false),
                FunctionalFormatVersion = table.Column<string>(maxLength: 40, nullable: false),
                ProjectionFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                UnsignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                SignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                SigningTimestamp = table.Column<DateTimeOffset>(precision: 0, nullable: false),
                SignatureProfileId = table.Column<string>(maxLength: 120, nullable: false),
                CertificateThumbprint = table.Column<string>(maxLength: 160, nullable: false),
                CertificateSerialNumber = table.Column<string>(maxLength: 160, nullable: false),
                SchemaSetId = table.Column<string>(maxLength: 120, nullable: false),
                SchemaFunctionalFormatVersion = table.Column<string>(maxLength: 40, nullable: false),
                SchemaArchiveVersion = table.Column<string>(maxLength: 40, nullable: false),
                SchemaSetFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                SignedXml = table.Column<string>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fdr_signed_artifacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fdr_artifact_evidence",
                    column: x => x.SigningEvidenceId,
                    principalTable: "v1_fiscal_daily_report_signing_evidence",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_artifact_identity",
            table: "v1_fiscal_daily_report_signed_artifacts",
            columns: new[] { "OrganizationId", "IssuerRuc", "SummaryDate", "Sequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_artifact_evidence",
            table: "v1_fiscal_daily_report_signed_artifacts",
            column: "SigningEvidenceId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_fiscal_daily_report_signed_artifacts");
        migrationBuilder.DropTable(name: "v1_fiscal_daily_report_signing_evidence");
    }
}
