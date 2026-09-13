using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260913173000_V1FiscalCfeEnvelopeAckCertificateTrust")]
public sealed class V1FiscalCfeEnvelopeAckCertificateTrust : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_fiscal_cfe_envelope_ack_certificate_trust_validations",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                SignatureVerificationId = table.Column<Guid>(nullable: false),
                AckObservationId = table.Column<Guid>(nullable: false),
                SubmissionId = table.Column<Guid>(nullable: false),
                EnvelopeId = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                OperationId = table.Column<string>(maxLength: 120, nullable: false),
                ResponseSha256 = table.Column<string>(maxLength: 64, nullable: false),
                ValidationProfileId = table.Column<string>(maxLength: 120, nullable: false),
                CertificateSha256 = table.Column<string>(maxLength: 64, nullable: false),
                TrustedRootSha256 = table.Column<string>(maxLength: 64, nullable: false),
                ChainCertificateSha256Json = table.Column<string>(nullable: false),
                RevocationMode = table.Column<string>(maxLength: 40, nullable: false),
                PkiUruguayTrustValidated = table.Column<bool>(nullable: false),
                DgiIdentityValidated = table.Column<bool>(nullable: false),
                ValidatedAtUtc = table.Column<DateTimeOffset>(precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscal_cfe_envelope_ack_certificate_trust_validations", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fceactv_signature_verification",
                    column: x => x.SignatureVerificationId,
                    principalTable: "v1_fiscal_cfe_envelope_ack_signature_verifications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fceactv_ack_observation",
                    column: x => x.AckObservationId,
                    principalTable: "v1_fiscal_cfe_envelope_ack_observations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fceactv_submission",
                    column: x => x.SubmissionId,
                    principalTable: "v1_fiscal_cfe_envelope_submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fceactv_envelope",
                    column: x => x.EnvelopeId,
                    principalTable: "v1_fiscal_cfe_envelopes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fceactv_operation",
            table: "v1_fiscal_cfe_envelope_ack_certificate_trust_validations",
            columns: new[] { "OrganizationId", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_fceactv_signature_verification",
            table: "v1_fiscal_cfe_envelope_ack_certificate_trust_validations",
            column: "SignatureVerificationId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_fceactv_ack_observation",
            table: "v1_fiscal_cfe_envelope_ack_certificate_trust_validations",
            column: "AckObservationId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_fceactv_submission",
            table: "v1_fiscal_cfe_envelope_ack_certificate_trust_validations",
            column: "SubmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_fceactv_envelope",
            table: "v1_fiscal_cfe_envelope_ack_certificate_trust_validations",
            column: "EnvelopeId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_fiscal_cfe_envelope_ack_certificate_trust_validations");
}
