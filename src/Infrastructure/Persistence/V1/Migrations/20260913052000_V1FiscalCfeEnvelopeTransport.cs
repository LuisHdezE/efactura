using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260913052000_V1FiscalCfeEnvelopeTransport")]
public sealed class V1FiscalCfeEnvelopeTransport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_cfe_envelope_submissions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                EnvelopeId = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                ReceiverRut = table.Column<string>(maxLength: 12, nullable: false),
                SenderEnvelopeId = table.Column<long>(nullable: false),
                OperationId = table.Column<string>(maxLength: 120, nullable: false),
                EnvelopeSha256 = table.Column<string>(maxLength: 64, nullable: false),
                State = table.Column<int>(nullable: false),
                AttemptCount = table.Column<int>(nullable: false),
                PreparedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false),
                LastAttemptAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: true),
                CompletedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: true),
                ResponseXml = table.Column<string>(nullable: true),
                ResponseSha256 = table.Column<string>(maxLength: 64, nullable: true),
                FailureCode = table.Column<string>(maxLength: 160, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_v1_fiscal_cfe_envelope_submissions", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "UX_v1_fces_operation",
            table: "v1_fiscal_cfe_envelope_submissions",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_fces_envelope",
            table: "v1_fiscal_cfe_envelope_submissions",
            column: "EnvelopeId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fiscal_cfe_envelope_submissions");
}
