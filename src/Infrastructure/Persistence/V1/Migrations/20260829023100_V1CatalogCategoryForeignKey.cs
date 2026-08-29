using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260829023100_V1CatalogCategoryForeignKey")]
public sealed class V1CatalogCategoryForeignKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_v1_commercial_items_CategoryId",
            table: "v1_commercial_items",
            column: "CategoryId");

        migrationBuilder.AddForeignKey(
            name: "FK_v1_commercial_items_v1_item_categories_CategoryId",
            table: "v1_commercial_items",
            column: "CategoryId",
            principalTable: "v1_item_categories",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_v1_commercial_items_v1_item_categories_CategoryId",
            table: "v1_commercial_items");

        migrationBuilder.DropIndex(
            name: "IX_v1_commercial_items_CategoryId",
            table: "v1_commercial_items");
    }
}
