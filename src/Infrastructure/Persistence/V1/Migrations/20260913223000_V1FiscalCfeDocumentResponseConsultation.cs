using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260913223000_V1FiscalCfeDocumentResponseConsultation")]
public sealed class V1FiscalCfeDocumentResponseConsultation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_cfe_document_response_consultations",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                AckObservationId = table.Column<Guid>(nullable: false),
                SubmissionId = table.Column<Guid>(nullable: false),
                EnvelopeId = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                OperationId = table.Column<string>(maxLength: 120, nullable: false),
                SourceAckResponseSha256 = table.Column<string>(maxLength: 64, nullable: false),
                DgiReceiverId = table.Column<long>(nullable: false),
                ConsultationTokenSha256 = table.Column<string>(maxLength: 64, nullable: false),
                DgiResponseId = table.Column<long>(nullable: false),
                IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                ReceiverRut = table.Column<string>(maxLength: 12, nullable: false),
                SenderEnvelopeId = table.Column<long>(nullable: false),
                EnvelopeCfeCount = table.Column<int>(nullable: false),
                RespondedCount = table.Column<int>(nullable: false),
                AcceptedCount = table.Column<int>(nullable: false),
                RejectedCount = table.Column<int>(nullable: false),
                ObservedCount = table.Column<int>(nullable: false),
                OtherRejectedCount = table.Column<int>(nullable: false),
                DetailsJson = table.Column<string>(nullable: false),
                ResponseXml = table.Column<string>(nullable: false),
                ResponseSha256 = table.Column<string>(maxLength: 64, nullable: false),
                ConsultedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscal_cfe_document_response_consultations", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fcdrc_ack_observation",
                    column: x => x.AckObservationId,
                    principalTable: "v1_fiscal_cfe_envelope_ack_observations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fcdrc_submission",
                    column: x => x.SubmissionId,
                    principalTable: "v1_fiscal_cfe_envelope_submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fcdrc_envelope",
                    column: x => x.EnvelopeId,
                    principalTable: "v1_fiscal_cfe_envelopes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fcdrc_operation",
            table: "v1_fiscal_cfe_document_response_consultations",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_fcdrc_ack_observation",
            table: "v1_fiscal_cfe_document_response_consultations",
            column: "AckObservationId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_fcdrc_submission",
            table: "v1_fiscal_cfe_document_response_consultations",
            column: "SubmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_fcdrc_envelope",
            table: "v1_fiscal_cfe_document_response_consultations",
            column: "EnvelopeId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fiscal_cfe_document_response_consultations");
}
