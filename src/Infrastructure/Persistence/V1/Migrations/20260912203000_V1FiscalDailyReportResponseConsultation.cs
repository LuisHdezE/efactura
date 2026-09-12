using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260912203000_V1FiscalDailyReportResponseConsultation")]
public sealed class V1FiscalDailyReportResponseConsultation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fdr_response_consultations",
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
                DgiReceiverId = table.Column<string>(maxLength: 120, nullable: false),
                AckStateCode = table.Column<string>(maxLength: 8, nullable: false),
                AckXml = table.Column<string>(nullable: false),
                AckXmlHash = table.Column<string>(maxLength: 64, nullable: false),
                Consistency = table.Column<int>(nullable: false),
                ConsultedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fdr_response_consultations", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fdr_cons_root",
                    column: x => x.RootSubmissionId,
                    principalTable: "v1_fiscal_daily_report_submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fdr_cons_br",
                    column: x => x.BrCorrectionRevisionId,
                    principalTable: "v1_fdr_br_revisions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_cons_operation",
            table: "v1_fdr_response_consultations",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_fdr_cons_receiver",
            table: "v1_fdr_response_consultations",
            columns: new[] { "OrganizationId", "DgiReceiverId" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_fdr_cons_root",
            table: "v1_fdr_response_consultations",
            column: "RootSubmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_fdr_cons_br",
            table: "v1_fdr_response_consultations",
            column: "BrCorrectionRevisionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fdr_response_consultations");
}
