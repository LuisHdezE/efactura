using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260912061500_V1FiscalDailyReportTransport")]
public sealed class V1FiscalDailyReportTransport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_daily_report_submissions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                SignedArtifactId = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                SummaryDate = table.Column<DateTime>(type: "date", nullable: false),
                Sequence = table.Column<int>(nullable: false),
                OperationId = table.Column<string>(maxLength: 120, nullable: false),
                SignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                State = table.Column<int>(nullable: false),
                AttemptCount = table.Column<int>(nullable: false),
                PreparedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false),
                LastAttemptAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: true),
                CompletedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: true),
                DgiReceiverId = table.Column<string>(maxLength: 120, nullable: true),
                AckStateCode = table.Column<string>(maxLength: 8, nullable: true),
                AckXml = table.Column<string>(nullable: true),
                FailureCode = table.Column<string>(maxLength: 160, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fdr_submissions", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fdr_submission_artifact",
                    column: x => x.SignedArtifactId,
                    principalTable: "v1_fiscal_daily_report_signed_artifacts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_submission_identity",
            table: "v1_fiscal_daily_report_submissions",
            columns: new[] { "OrganizationId", "IssuerRuc", "SummaryDate", "Sequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_submission_operation",
            table: "v1_fiscal_daily_report_submissions",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_submission_artifact",
            table: "v1_fiscal_daily_report_submissions",
            column: "SignedArtifactId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fiscal_daily_report_submissions");
}
