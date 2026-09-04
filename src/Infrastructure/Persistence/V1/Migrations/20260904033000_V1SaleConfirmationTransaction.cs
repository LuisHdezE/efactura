using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260904033000_V1SaleConfirmationTransaction")]
public sealed class V1SaleConfirmationTransaction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ConfirmationFingerprint",
            table: "v1_sales",
            maxLength: 64,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "SettlementFingerprint",
            table: "v1_sales",
            maxLength: 64,
            nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ConfirmedAtUtc",
            table: "v1_sales",
            precision: 6,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ConfirmedAtUtc", table: "v1_sales");
        migrationBuilder.DropColumn(name: "SettlementFingerprint", table: "v1_sales");
        migrationBuilder.DropColumn(name: "ConfirmationFingerprint", table: "v1_sales");
    }
}
