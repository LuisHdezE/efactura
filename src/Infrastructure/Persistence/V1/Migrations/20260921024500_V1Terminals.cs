using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260921024500_V1Terminals")]
public sealed class V1Terminals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_terminals",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 200, nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                Code = table.Column<string>(maxLength: 64, nullable: false),
                NormalizedCode = table.Column<string>(maxLength: 64, nullable: false),
                Name = table.Column<string>(maxLength: 120, nullable: false),
                LocationId = table.Column<string>(maxLength: 200, nullable: false),
                Active = table.Column<bool>(nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_terminals", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_terminal_location",
                    column: x => x.LocationId,
                    principalTable: "v1_fiscal_locations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_terminal_org_code",
            table: "v1_terminals",
            columns: new[] { "OrganizationId", "NormalizedCode" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_terminal_org_active",
            table: "v1_terminals",
            columns: new[] { "OrganizationId", "Active" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_terminal_org_location",
            table: "v1_terminals",
            columns: new[] { "OrganizationId", "LocationId" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_terminal_location_fk",
            table: "v1_terminals",
            column: "LocationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_terminals");
    }
}
