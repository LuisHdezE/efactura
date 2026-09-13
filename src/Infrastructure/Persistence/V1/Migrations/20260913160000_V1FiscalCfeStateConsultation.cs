using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260913160000_V1FiscalCfeStateConsultation")]
public sealed class V1FiscalCfeStateConsultation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_cfe_state_consultations",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                FiscalDocumentId = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                CfeType = table.Column<int>(nullable: false),
                Series = table.Column<string>(maxLength: 20, nullable: false),
                Number = table.Column<long>(nullable: false),
                OperationId = table.Column<string>(maxLength: 120, nullable: false),
                StateCode = table.Column<string>(maxLength: 40, nullable: false),
                DgiSenderId = table.Column<string>(maxLength: 120, nullable: false),
                DgiReceiverId = table.Column<string>(maxLength: 120, nullable: false),
                ConsultationToken = table.Column<string>(nullable: true),
                ConsultationAvailableAtText = table.Column<string>(maxLength: 80, nullable: true),
                ResponseXml = table.Column<string>(nullable: false),
                ResponseSha256 = table.Column<string>(maxLength: 64, nullable: false),
                ConsultedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscal_cfe_state_consultations", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fcsc_document",
                    column: x => x.FiscalDocumentId,
                    principalTable: "v1_fiscal_documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fcsc_operation",
            table: "v1_fiscal_cfe_state_consultations",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_fcsc_document",
            table: "v1_fiscal_cfe_state_consultations",
            column: "FiscalDocumentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fiscal_cfe_state_consultations");
}
