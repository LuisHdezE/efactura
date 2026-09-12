using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260912120500_V1FiscalDailyReportSigningOffset")]
public sealed class V1FiscalDailyReportSigningOffset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SigningOffsetMinutes",
            table: "v1_fiscal_daily_report_signing_evidence",
            nullable: false,
            defaultValue: -180);

        migrationBuilder.AddColumn<int>(
            name: "SigningOffsetMinutes",
            table: "v1_fiscal_daily_report_signed_artifacts",
            nullable: false,
            defaultValue: -180);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SigningOffsetMinutes",
            table: "v1_fiscal_daily_report_signed_artifacts");

        migrationBuilder.DropColumn(
            name: "SigningOffsetMinutes",
            table: "v1_fiscal_daily_report_signing_evidence");
    }
}
