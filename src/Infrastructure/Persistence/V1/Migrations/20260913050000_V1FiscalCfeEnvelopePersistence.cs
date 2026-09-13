using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260913050000_V1FiscalCfeEnvelopePersistence")]
public sealed class V1FiscalCfeEnvelopePersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_cfe_envelopes",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                ReceiverRut = table.Column<string>(maxLength: 12, nullable: false),
                IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                SenderEnvelopeId = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false),
                CreatedAtOffsetMinutes = table.Column<int>(nullable: false),
                FiscalDocumentIdsJson = table.Column<string>(nullable: false),
                OperationId = table.Column<string>(maxLength: 120, nullable: false),
                CfeCount = table.Column<int>(nullable: false),
                CertificateThumbprint = table.Column<string>(maxLength: 160, nullable: false),
                CertificateSerialNumber = table.Column<string>(maxLength: 160, nullable: false),
                EnvelopeXml = table.Column<string>(nullable: false),
                EnvelopeSha256 = table.Column<string>(maxLength: 64, nullable: false),
                SchemaSetId = table.Column<string>(maxLength: 120, nullable: false),
                SchemaVersion = table.Column<string>(maxLength: 40, nullable: false),
                SchemaSetFingerprint = table.Column<string>(maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscal_cfe_envelopes", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fce_operation",
            table: "v1_fiscal_cfe_envelopes",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fce_identity",
            table: "v1_fiscal_cfe_envelopes",
            columns: new[] { "OrganizationId", "IssuerRuc", "ReceiverRut", "SenderEnvelopeId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fiscal_cfe_envelopes");
}
