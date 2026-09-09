using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260909195000_V1FiscalSignedArtifact")]
public sealed class V1FiscalSignedArtifact : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_signed_artifacts",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                FiscalDocumentId = table.Column<Guid>(nullable: false),
                SigningEvidenceId = table.Column<Guid>(nullable: false),
                FiscalContentFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                UnsignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                SigningPayloadHash = table.Column<string>(maxLength: 64, nullable: false),
                SignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                SigningTimestamp = table.Column<DateTimeOffset>(nullable: false),
                SignatureProfileId = table.Column<string>(maxLength: 120, nullable: false),
                CertificateThumbprint = table.Column<string>(maxLength: 160, nullable: false),
                CertificateSerialNumber = table.Column<string>(maxLength: 160, nullable: false),
                SignedXml = table.Column<string>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscal_signed_artifacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fsa_document",
                    column: x => x.FiscalDocumentId,
                    principalTable: "v1_fiscal_documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fsa_evidence",
                    column: x => x.SigningEvidenceId,
                    principalTable: "v1_fiscal_signing_evidence",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fsa_org_document",
            table: "v1_fiscal_signed_artifacts",
            columns: new[] { "OrganizationId", "FiscalDocumentId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fsa_evidence",
            table: "v1_fiscal_signed_artifacts",
            column: "SigningEvidenceId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_fiscal_signed_artifacts");
    }
}
