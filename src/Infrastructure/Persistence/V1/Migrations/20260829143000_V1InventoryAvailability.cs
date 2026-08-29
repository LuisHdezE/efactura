using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260829143000_V1InventoryAvailability")]
public sealed class V1InventoryAvailability : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_inventory_positions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                ItemId = table.Column<Guid>(nullable: false),
                LocationId = table.Column<string>(maxLength: 200, nullable: false),
                Quantity = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_inventory_positions", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_inventory_positions_v1_commercial_items_ItemId",
                    column: x => x.ItemId,
                    principalTable: "v1_commercial_items",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "v1_stock_movements",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                PositionId = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                ItemId = table.Column<Guid>(nullable: false),
                LocationId = table.Column<string>(maxLength: 200, nullable: false),
                Kind = table.Column<int>(nullable: false),
                QuantityBefore = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                QuantityDelta = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                QuantityAfter = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                ReasonCode = table.Column<string>(maxLength: 80, nullable: false),
                Explanation = table.Column<string>(maxLength: 1000, nullable: true),
                OccurredAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_stock_movements", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_stock_movements_v1_inventory_positions_PositionId",
                    column: x => x.PositionId,
                    principalTable: "v1_inventory_positions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_stock_movements_v1_commercial_items_ItemId",
                    column: x => x.ItemId,
                    principalTable: "v1_commercial_items",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_v1_inventory_positions_OrganizationId_ItemId_LocationId",
            table: "v1_inventory_positions",
            columns: new[] { "OrganizationId", "ItemId", "LocationId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_v1_inventory_positions_ItemId",
            table: "v1_inventory_positions",
            column: "ItemId");
        migrationBuilder.CreateIndex(
            name: "IX_v1_inventory_positions_OrganizationId_LocationId",
            table: "v1_inventory_positions",
            columns: new[] { "OrganizationId", "LocationId" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_stock_movements_PositionId",
            table: "v1_stock_movements",
            column: "PositionId");
        migrationBuilder.CreateIndex(
            name: "IX_v1_stock_movements_ItemId",
            table: "v1_stock_movements",
            column: "ItemId");
        migrationBuilder.CreateIndex(
            name: "IX_v1_stock_movements_OrganizationId_LocationId_OccurredAtUtc",
            table: "v1_stock_movements",
            columns: new[] { "OrganizationId", "LocationId", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_stock_movements");
        migrationBuilder.DropTable(name: "v1_inventory_positions");
    }
}
