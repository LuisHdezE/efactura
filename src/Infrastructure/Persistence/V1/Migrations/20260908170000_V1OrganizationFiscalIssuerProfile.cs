using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260908170000_V1OrganizationFiscalIssuerProfile")]
public sealed class V1OrganizationFiscalIssuerProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_company_fiscal_profiles",
            columns: table => new
            {
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                Ruc = table.Column<string>(maxLength: 12, nullable: false),
                LegalName = table.Column<string>(maxLength: 150, nullable: false),
                CommercialName = table.Column<string>(maxLength: 30, nullable: true),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_v1_company_fiscal_profiles", x => x.OrganizationId));

        migrationBuilder.CreateTable(
            name: "v1_fiscal_locations",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 200, nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                Name = table.Column<string>(maxLength: 120, nullable: false),
                DgiBranchCode = table.Column<string>(maxLength: 4, nullable: false),
                FiscalAddress = table.Column<string>(maxLength: 70, nullable: false),
                City = table.Column<string>(maxLength: 30, nullable: false),
                Department = table.Column<string>(maxLength: 30, nullable: false),
                Active = table.Column<bool>(nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_v1_fiscal_locations", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_v1_company_ruc", table: "v1_company_fiscal_profiles", column: "Ruc");
        migrationBuilder.CreateIndex(name: "UX_v1_location_org_branch", table: "v1_fiscal_locations", columns: new[] { "OrganizationId", "DgiBranchCode" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_v1_location_org_active", table: "v1_fiscal_locations", columns: new[] { "OrganizationId", "Active" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_fiscal_locations");
        migrationBuilder.DropTable(name: "v1_company_fiscal_profiles");
    }
}
