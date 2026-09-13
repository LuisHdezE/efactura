using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260913065000_V1FiscalCfeEnvelopeAckObservation")]
public sealed class V1FiscalCfeEnvelopeAckObservation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_cfe_envelope_ack_observations",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                SubmissionId = table.Column<Guid>(nullable: false),
                EnvelopeId = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                ReceiverRut = table.Column<string>(maxLength: 12, nullable: false),
                SenderEnvelopeId = table.Column<long>(nullable: false),
                ResponseSha256 = table.Column<string>(maxLength: 64, nullable: false),
                DgiResponseId = table.Column<long>(nullable: false),
                DgiReceiverId = table.Column<long>(nullable: false),
                CfeCount = table.Column<int>(nullable: false),
                State = table.Column<int>(nullable: false),
                ReceptionTimestampText = table.Column<string>(maxLength: 80, nullable: false),
                SigningTimestampText = table.Column<string>(maxLength: 80, nullable: false),
                ConsultationToken = table.Column<string>(nullable: true),
                ConsultationAvailableAtText = table.Column<string>(maxLength: 80, nullable: true),
                RejectionReasonsJson = table.Column<string>(nullable: false),
                ObservedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscal_cfe_envelope_ack_observations", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fceao_submission",
                    column: x => x.SubmissionId,
                    principalTable: "v1_fiscal_cfe_envelope_submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fceao_envelope",
                    column: x => x.EnvelopeId,
                    principalTable: "v1_fiscal_cfe_envelopes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fceao_submission",
            table: "v1_fiscal_cfe_envelope_ack_observations",
            column: "SubmissionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_fceao_envelope",
            table: "v1_fiscal_cfe_envelope_ack_observations",
            column: "EnvelopeId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fiscal_cfe_envelope_ack_observations");
}
