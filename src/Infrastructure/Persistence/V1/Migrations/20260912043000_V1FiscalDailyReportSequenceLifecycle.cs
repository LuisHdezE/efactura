using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260912043000_V1FiscalDailyReportSequenceLifecycle")]
public sealed class V1FiscalDailyReportSequenceLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_daily_report_versions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                SummaryDate = table.Column<DateTime>(type: "date", nullable: false),
                Sequence = table.Column<int>(nullable: false),
                PreviousVersionId = table.Column<Guid>(nullable: true),
                OperationId = table.Column<string>(maxLength: 120, nullable: false),
                RevisionKind = table.Column<int>(nullable: false),
                ReasonCode = table.Column<string>(maxLength: 120, nullable: false),
                ReconciliationFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                RequiresFxReliquidation = table.Column<bool>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false),
                VersionFingerprint = table.Column<string>(maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fdr_versions", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fdr_version_previous",
                    column: x => x.PreviousVersionId,
                    principalTable: "v1_fiscal_daily_report_versions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_version_identity",
            table: "v1_fiscal_daily_report_versions",
            columns: new[] { "OrganizationId", "IssuerRuc", "SummaryDate", "Sequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fdr_version_operation",
            table: "v1_fiscal_daily_report_versions",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_fdr_version_previous",
            table: "v1_fiscal_daily_report_versions",
            column: "PreviousVersionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fiscal_daily_report_versions");
}
