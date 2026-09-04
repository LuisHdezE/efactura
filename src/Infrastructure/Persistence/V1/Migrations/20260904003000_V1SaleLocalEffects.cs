using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260904003000_V1SaleLocalEffects")]
public sealed class V1SaleLocalEffects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "SourceSaleId",
            table: "v1_stock_movements",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ConfirmationFingerprint",
            table: "v1_stock_movements",
            maxLength: 64,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "SettlementFingerprint",
            table: "v1_stock_movements",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddForeignKey(
            name: "FK_v1_stock_sale",
            table: "v1_stock_movements",
            column: "SourceSaleId",
            principalTable: "v1_sales",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateIndex(
            name: "IX_v1_stock_sale",
            table: "v1_stock_movements",
            column: "SourceSaleId");
        migrationBuilder.CreateIndex(
            name: "UX_v1_stock_sale_position",
            table: "v1_stock_movements",
            columns: new[] { "OrganizationId", "SourceSaleId", "PositionId" },
            unique: true);

        migrationBuilder.CreateTable(
            name: "v1_fiscalization_requests",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                SaleId = table.Column<Guid>(nullable: false),
                LocationId = table.Column<string>(maxLength: 200, nullable: true),
                TerminalId = table.Column<string>(maxLength: 200, nullable: true),
                CfeFamily = table.Column<int>(nullable: false),
                ReceiverIdentification = table.Column<int>(nullable: true),
                FormatVersion = table.Column<string>(maxLength: 40, nullable: false),
                ConfirmationFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                SettlementFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                CurrencyCode = table.Column<string>(maxLength: 3, nullable: false),
                NetAmount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                VatAmount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                TotalAmount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                Status = table.Column<int>(nullable: false),
                Version = table.Column<long>(nullable: false),
                RequestedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscalization_requests", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fiscal_req_sale",
                    column: x => x.SaleId,
                    principalTable: "v1_sales",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fiscal_req_org_sale",
            table: "v1_fiscalization_requests",
            columns: new[] { "OrganizationId", "SaleId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_v1_fiscal_req_work",
            table: "v1_fiscalization_requests",
            columns: new[] { "OrganizationId", "Status", "RequestedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_fiscalization_requests");

        migrationBuilder.DropForeignKey(
            name: "FK_v1_stock_sale",
            table: "v1_stock_movements");
        migrationBuilder.DropIndex(
            name: "IX_v1_stock_sale",
            table: "v1_stock_movements");
        migrationBuilder.DropIndex(
            name: "UX_v1_stock_sale_position",
            table: "v1_stock_movements");
        migrationBuilder.DropColumn(
            name: "SourceSaleId",
            table: "v1_stock_movements");
        migrationBuilder.DropColumn(
            name: "ConfirmationFingerprint",
            table: "v1_stock_movements");
        migrationBuilder.DropColumn(
            name: "SettlementFingerprint",
            table: "v1_stock_movements");
    }
}
