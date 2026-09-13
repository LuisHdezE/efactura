using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260913003500_V1FiscalDailyReportReceiverDiscovery")]
public sealed class V1FiscalDailyReportReceiverDiscovery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fdr_receiver_discoveries",
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
                DgiStateCode = table.Column<string>(maxLength: 8, nullable: false),
                DgiReceptionTimestampText = table.Column<string>(maxLength: 80, nullable: false),
                EvidenceXml = table.Column<string>(nullable: false),
                EvidenceXmlHash = table.Column<string>(maxLength: 64, nullable: false),
                DiscoveredAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fdr_receiver_discoveries", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fdr_disc_root",
                    column: x => x.RootSubmissionId,
                    principalTable: "v1_fiscal_daily_report_submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fdr_disc_br",
                    column: x => x.BrCorrectionRevisionId,
                    principalTable: "v1_fdr_br_revisions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_disc_operation",
            table: "v1_fdr_receiver_discoveries",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_disc_receiver",
            table: "v1_fdr_receiver_discoveries",
            columns: new[] { "OrganizationId", "DgiReceiverId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_disc_root",
            table: "v1_fdr_receiver_discoveries",
            column: "RootSubmissionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_disc_br",
            table: "v1_fdr_receiver_discoveries",
            column: "BrCorrectionRevisionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_fdr_disc_identity",
            table: "v1_fdr_receiver_discoveries",
            columns: new[] { "OrganizationId", "IssuerRuc", "SummaryDate", "Sequence" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fdr_receiver_discoveries");
}
