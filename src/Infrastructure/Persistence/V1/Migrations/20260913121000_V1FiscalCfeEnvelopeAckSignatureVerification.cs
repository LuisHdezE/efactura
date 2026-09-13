using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260913121000_V1FiscalCfeEnvelopeAckSignatureVerification")]
public sealed class V1FiscalCfeEnvelopeAckSignatureVerification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_cfe_envelope_ack_signature_verifications",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                AckObservationId = table.Column<Guid>(nullable: false),
                SubmissionId = table.Column<Guid>(nullable: false),
                EnvelopeId = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                ResponseSha256 = table.Column<string>(maxLength: 64, nullable: false),
                VerificationProfileId = table.Column<string>(maxLength: 120, nullable: false),
                CertificateSha256 = table.Column<string>(maxLength: 64, nullable: false),
                CertificateThumbprint = table.Column<string>(maxLength: 160, nullable: false),
                CertificateSerialNumber = table.Column<string>(maxLength: 160, nullable: false),
                CertificateSubject = table.Column<string>(maxLength: 1024, nullable: false),
                CertificateIssuer = table.Column<string>(maxLength: 1024, nullable: false),
                CanonicalizationMethod = table.Column<string>(maxLength: 300, nullable: false),
                SignatureMethod = table.Column<string>(maxLength: 300, nullable: false),
                DigestMethod = table.Column<string>(maxLength: 300, nullable: false),
                ReferenceUri = table.Column<string>(maxLength: 512, nullable: false),
                ReferenceTransformsJson = table.Column<string>(nullable: false),
                CertificateTrustValidated = table.Column<bool>(nullable: false),
                VerifiedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscal_cfe_envelope_ack_signature_verifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fceasv_ack_observation",
                    column: x => x.AckObservationId,
                    principalTable: "v1_fiscal_cfe_envelope_ack_observations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fceasv_submission",
                    column: x => x.SubmissionId,
                    principalTable: "v1_fiscal_cfe_envelope_submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fceasv_envelope",
                    column: x => x.EnvelopeId,
                    principalTable: "v1_fiscal_cfe_envelopes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fceasv_ack_observation",
            table: "v1_fiscal_cfe_envelope_ack_signature_verifications",
            column: "AckObservationId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_fceasv_submission",
            table: "v1_fiscal_cfe_envelope_ack_signature_verifications",
            column: "SubmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_fceasv_envelope",
            table: "v1_fiscal_cfe_envelope_ack_signature_verifications",
            column: "EnvelopeId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fiscal_cfe_envelope_ack_signature_verifications");
}
