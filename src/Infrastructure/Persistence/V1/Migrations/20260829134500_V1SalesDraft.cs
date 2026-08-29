using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260829134500_V1SalesDraft")]
public sealed class V1SalesDraft : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_sales",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                LocationId = table.Column<string>(maxLength: 200, nullable: true),
                TerminalId = table.Column<string>(maxLength: 200, nullable: true),
                CustomerPartyId = table.Column<Guid>(nullable: true),
                Intent = table.Column<int>(nullable: false),
                CurrencyCode = table.Column<string>(maxLength: 3, nullable: false),
                EffectiveOnUtc = table.Column<DateTime>(precision: 6, nullable: false),
                DeliveryCountry = table.Column<string>(maxLength: 2, nullable: true),
                GoodsExportConfirmed = table.Column<bool>(nullable: false),
                Status = table.Column<int>(nullable: false),
                ValidationFingerprint = table.Column<string>(maxLength: 64, nullable: true),
                ValidatedAtUtc = table.Column<DateTime>(precision: 6, nullable: true),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_sales", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_sales_v1_parties_CustomerPartyId",
                    column: x => x.CustomerPartyId,
                    principalTable: "v1_parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "v1_sale_lines",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                SaleId = table.Column<Guid>(nullable: false),
                ItemId = table.Column<Guid>(nullable: false),
                ItemCode = table.Column<string>(maxLength: 80, nullable: false),
                ItemName = table.Column<string>(maxLength: 250, nullable: false),
                Kind = table.Column<int>(nullable: false),
                Quantity = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                UnitPrice = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                TaxProfileId = table.Column<Guid>(nullable: true),
                ServicePerformanceScope = table.Column<int>(nullable: false),
                ServiceUseCountry = table.Column<string>(maxLength: 2, nullable: true),
                ExportServiceKind = table.Column<int>(nullable: false),
                RecipientIsPersonAbroad = table.Column<int>(nullable: false),
                ExclusiveUseAbroad = table.Column<int>(nullable: false),
                ForeignEconomicRelation = table.Column<int>(nullable: false),
                RecipientInstalledInFreeZone = table.Column<int>(nullable: false),
                ProviderFromNonFreeNationalTerritory = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_sale_lines", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_sale_lines_v1_sales_SaleId",
                    column: x => x.SaleId,
                    principalTable: "v1_sales",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_v1_sale_lines_v1_commercial_items_ItemId",
                    column: x => x.ItemId,
                    principalTable: "v1_commercial_items",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_sale_lines_v1_tax_profiles_TaxProfileId",
                    column: x => x.TaxProfileId,
                    principalTable: "v1_tax_profiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_v1_sales_OrganizationId_EffectiveOnUtc",
            table: "v1_sales",
            columns: new[] { "OrganizationId", "EffectiveOnUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_v1_sales_OrganizationId_Status",
            table: "v1_sales",
            columns: new[] { "OrganizationId", "Status" });
        migrationBuilder.CreateIndex(
            name: "IX_v1_sales_OrganizationId_CustomerPartyId",
            table: "v1_sales",
            columns: new[] { "OrganizationId", "CustomerPartyId" });
        migrationBuilder.CreateIndex(
            name: "IX_v1_sales_CustomerPartyId",
            table: "v1_sales",
            column: "CustomerPartyId");
        migrationBuilder.CreateIndex(
            name: "IX_v1_sale_lines_SaleId",
            table: "v1_sale_lines",
            column: "SaleId");
        migrationBuilder.CreateIndex(
            name: "IX_v1_sale_lines_ItemId",
            table: "v1_sale_lines",
            column: "ItemId");
        migrationBuilder.CreateIndex(
            name: "IX_v1_sale_lines_TaxProfileId",
            table: "v1_sale_lines",
            column: "TaxProfileId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_sale_lines");
        migrationBuilder.DropTable(name: "v1_sales");
    }
}
