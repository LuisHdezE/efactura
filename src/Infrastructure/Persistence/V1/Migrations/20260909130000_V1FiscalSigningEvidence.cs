using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260909130000_V1FiscalSigningEvidence")]
public sealed class V1FiscalSigningEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_signing_evidence",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                FiscalDocumentId = table.Column<Guid>(nullable: false),
                FiscalContentFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                UnsignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                SigningTimestamp = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscal_signing_evidence", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fse_document",
                    column: x => x.FiscalDocumentId,
                    principalTable: "v1_fiscal_documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fse_org_document",
            table: "v1_fiscal_signing_evidence",
            columns: new[] { "OrganizationId", "FiscalDocumentId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_fiscal_signing_evidence");
    }
}
