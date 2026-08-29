using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260829031000_V1TaxProfiles")]
public sealed class V1TaxProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_tax_profiles",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                Code = table.Column<string>(maxLength: 80, nullable: false),
                Name = table.Column<string>(maxLength: 250, nullable: false),
                TreatmentCode = table.Column<string>(maxLength: 80, nullable: false),
                RatePercent = table.Column<decimal>(precision: 9, scale: 4, nullable: false),
                EffectiveFromUtc = table.Column<DateTime>(precision: 6, nullable: false),
                EffectiveToUtc = table.Column<DateTime>(precision: 6, nullable: true),
                SourceName = table.Column<string>(maxLength: 250, nullable: false),
                SourceReference = table.Column<string>(maxLength: 1000, nullable: false),
                SourceVersion = table.Column<string>(maxLength: 120, nullable: false),
                Active = table.Column<bool>(nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_v1_tax_profiles", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_v1_tax_profiles_OrganizationId_Code_EffectiveFromUtc",
            table: "v1_tax_profiles",
            columns: new[] { "OrganizationId", "Code", "EffectiveFromUtc" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_tax_profiles_OrganizationId_Active_EffectiveFromUtc",
            table: "v1_tax_profiles",
            columns: new[] { "OrganizationId", "Active", "EffectiveFromUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_commercial_items_TaxProfileId",
            table: "v1_commercial_items",
            column: "TaxProfileId");

        migrationBuilder.AddForeignKey(
            name: "FK_v1_commercial_items_v1_tax_profiles_TaxProfileId",
            table: "v1_commercial_items",
            column: "TaxProfileId",
            principalTable: "v1_tax_profiles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_v1_commercial_items_v1_tax_profiles_TaxProfileId",
            table: "v1_commercial_items");

        migrationBuilder.DropIndex(
            name: "IX_v1_commercial_items_TaxProfileId",
            table: "v1_commercial_items");

        migrationBuilder.DropTable(name: "v1_tax_profiles");
    }
}
