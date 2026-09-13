using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260913012000_V1FiscalDailyReportLaterStateObservation")]
public sealed class V1FiscalDailyReportLaterStateObservation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fdr_later_state_observations",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                RootSubmissionId = table.Column<Guid>(nullable: true),
                BrCorrectionRevisionId = table.Column<Guid>(nullable: true),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                SummaryDate = table.Column<DateTime>(type: "date", nullable: false),
                Sequence = table.Column<int>(nullable: false),
                LocalRevision = table.Column<int>(nullable: true),
                OperationId = table.Column<string>(maxLength: 120, nullable: false),
                DgiEmitterId = table.Column<string>(maxLength: 120, nullable: false),
                DgiReceiverId = table.Column<string>(maxLength: 120, nullable: false),
                State = table.Column<int>(nullable: false),
                DgiStateCode = table.Column<string>(maxLength: 8, nullable: false),
                DgiReceptionTimestampText = table.Column<string>(maxLength: 80, nullable: false),
                EvidenceXml = table.Column<string>(nullable: false),
                EvidenceXmlHash = table.Column<string>(maxLength: 64, nullable: false),
                ObservedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fdr_later_state_observations", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fdr_later_root",
                    column: x => x.RootSubmissionId,
                    principalTable: "v1_fiscal_daily_report_submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fdr_later_br",
                    column: x => x.BrCorrectionRevisionId,
                    principalTable: "v1_fdr_br_revisions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_later_operation",
            table: "v1_fdr_later_state_observations",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_fdr_later_receiver",
            table: "v1_fdr_later_state_observations",
            columns: new[] { "OrganizationId", "DgiReceiverId", "ObservedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_fdr_later_root",
            table: "v1_fdr_later_state_observations",
            column: "RootSubmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_fdr_later_br",
            table: "v1_fdr_later_state_observations",
            column: "BrCorrectionRevisionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fdr_later_state_observations");
}
