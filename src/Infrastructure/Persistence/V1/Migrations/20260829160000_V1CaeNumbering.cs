using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260829160000_V1CaeNumbering")]
public sealed class V1CaeNumbering : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_cae_authorizations",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                CfeType = table.Column<int>(nullable: false),
                AuthorizationNumber = table.Column<string>(maxLength: 80, nullable: false),
                Series = table.Column<string>(maxLength: 20, nullable: false),
                RangeFrom = table.Column<long>(nullable: false),
                RangeTo = table.Column<long>(nullable: false),
                ValidFrom = table.Column<DateOnly>(nullable: false),
                ValidTo = table.Column<DateOnly>(nullable: false),
                Status = table.Column<int>(nullable: false),
                VerificationMethod = table.Column<string>(maxLength: 80, nullable: false),
                SourceArtifactId = table.Column<string>(maxLength: 200, nullable: false),
                SourceArtifactHash = table.Column<string>(maxLength: 128, nullable: false),
                SourceName = table.Column<string>(maxLength: 250, nullable: false),
                SourceReference = table.Column<string>(maxLength: 1000, nullable: false),
                NextNumber = table.Column<long>(nullable: false),
                Version = table.Column<long>(nullable: false),
                ImportedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false),
                ActivatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_v1_cae_authorizations", x => x.Id));

        migrationBuilder.CreateTable(
            name: "v1_cae_allocations",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                CaeAuthorizationId = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                LocationId = table.Column<string>(maxLength: 200, nullable: false),
                TerminalId = table.Column<string>(maxLength: 200, nullable: true),
                RangeFrom = table.Column<long>(nullable: false),
                RangeTo = table.Column<long>(nullable: false),
                NextNumber = table.Column<long>(nullable: false),
                Status = table.Column<int>(nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false),
                ClosedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_cae_allocations", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_cae_allocations_v1_cae_authorizations_CaeAuthorizationId",
                    column: x => x.CaeAuthorizationId,
                    principalTable: "v1_cae_authorizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "v1_fiscal_number_reservations",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                CaeAuthorizationId = table.Column<Guid>(nullable: false),
                AllocationId = table.Column<Guid>(nullable: true),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                CfeType = table.Column<int>(nullable: false),
                Series = table.Column<string>(maxLength: 20, nullable: false),
                Number = table.Column<long>(nullable: false),
                LocationId = table.Column<string>(maxLength: 200, nullable: true),
                TerminalId = table.Column<string>(maxLength: 200, nullable: true),
                OperationId = table.Column<string>(maxLength: 200, nullable: false),
                ReservedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscal_number_reservations", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fiscal_number_reservations_v1_cae_authorizations_CaeAuthorizationId",
                    column: x => x.CaeAuthorizationId,
                    principalTable: "v1_cae_authorizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fiscal_number_reservations_v1_cae_allocations_AllocationId",
                    column: x => x.AllocationId,
                    principalTable: "v1_cae_allocations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_v1_cae_authorizations_OrganizationId_SourceArtifactHash",
            table: "v1_cae_authorizations",
            columns: new[] { "OrganizationId", "SourceArtifactHash" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_v1_cae_authorizations_OrganizationId_CfeType_Series_RangeFrom_RangeTo",
            table: "v1_cae_authorizations",
            columns: new[] { "OrganizationId", "CfeType", "Series", "RangeFrom", "RangeTo" });
        migrationBuilder.CreateIndex(
            name: "IX_v1_cae_authorizations_OrganizationId_CfeType_Status_ValidTo",
            table: "v1_cae_authorizations",
            columns: new[] { "OrganizationId", "CfeType", "Status", "ValidTo" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_cae_allocations_CaeAuthorizationId",
            table: "v1_cae_allocations",
            column: "CaeAuthorizationId");
        migrationBuilder.CreateIndex(
            name: "IX_v1_cae_allocations_OrganizationId_LocationId_Status",
            table: "v1_cae_allocations",
            columns: new[] { "OrganizationId", "LocationId", "Status" });
        migrationBuilder.CreateIndex(
            name: "IX_v1_cae_allocations_CaeAuthorizationId_RangeFrom_RangeTo",
            table: "v1_cae_allocations",
            columns: new[] { "CaeAuthorizationId", "RangeFrom", "RangeTo" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_fiscal_number_reservations_CaeAuthorizationId",
            table: "v1_fiscal_number_reservations",
            column: "CaeAuthorizationId");
        migrationBuilder.CreateIndex(
            name: "IX_v1_fiscal_number_reservations_AllocationId",
            table: "v1_fiscal_number_reservations",
            column: "AllocationId");
        migrationBuilder.CreateIndex(
            name: "IX_v1_fiscal_number_reservations_OrganizationId_CfeType_Series_Number",
            table: "v1_fiscal_number_reservations",
            columns: new[] { "OrganizationId", "CfeType", "Series", "Number" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_v1_fiscal_number_reservations_OrganizationId_OperationId",
            table: "v1_fiscal_number_reservations",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_fiscal_number_reservations");
        migrationBuilder.DropTable(name: "v1_cae_allocations");
        migrationBuilder.DropTable(name: "v1_cae_authorizations");
    }
}
