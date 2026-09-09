using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260908233000_V1FiscalContentSnapshot")]
public sealed class V1FiscalContentSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ConfirmationEvidenceFingerprint",
            table: "v1_fiscalization_requests",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ConfirmationEvidenceJson",
            table: "v1_fiscalization_requests",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "v1_fiscal_content_snapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                FiscalDocumentId = table.Column<Guid>(nullable: false),
                FiscalizationRequestId = table.Column<Guid>(nullable: false),
                SaleId = table.Column<Guid>(nullable: false),
                ContentFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                SnapshotJson = table.Column<string>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscal_content_snapshots", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fcs_document",
                    column: x => x.FiscalDocumentId,
                    principalTable: "v1_fiscal_documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fcs_request",
                    column: x => x.FiscalizationRequestId,
                    principalTable: "v1_fiscalization_requests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fcs_sale",
                    column: x => x.SaleId,
                    principalTable: "v1_sales",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_v1_fcs_org_sale",
            table: "v1_fiscal_content_snapshots",
            columns: new[] { "OrganizationId", "SaleId" });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fcs_document",
            table: "v1_fiscal_content_snapshots",
            column: "FiscalDocumentId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_fiscal_content_snapshots");

        migrationBuilder.DropColumn(
            name: "ConfirmationEvidenceFingerprint",
            table: "v1_fiscalization_requests");

        migrationBuilder.DropColumn(
            name: "ConfirmationEvidenceJson",
            table: "v1_fiscalization_requests");
    }
}
