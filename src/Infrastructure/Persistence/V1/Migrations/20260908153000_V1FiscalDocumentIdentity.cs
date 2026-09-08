using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260908153000_V1FiscalDocumentIdentity")]
public sealed class V1FiscalDocumentIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "FiscalDocumentId",
            table: "v1_fiscalization_requests",
            nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "IdentityCreatedAtUtc",
            table: "v1_fiscalization_requests",
            precision: 6,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "v1_fiscal_documents",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                FiscalizationRequestId = table.Column<Guid>(nullable: false),
                SaleId = table.Column<Guid>(nullable: false),
                FiscalNumberReservationId = table.Column<Guid>(nullable: false),
                CaeAuthorizationId = table.Column<Guid>(nullable: false),
                CaeAllocationId = table.Column<Guid>(nullable: true),
                CfeType = table.Column<int>(nullable: false),
                Series = table.Column<string>(maxLength: 20, nullable: false),
                Number = table.Column<long>(nullable: false),
                CaeAuthorizationNumber = table.Column<string>(maxLength: 80, nullable: false),
                CaeRangeFrom = table.Column<long>(nullable: false),
                CaeRangeTo = table.Column<long>(nullable: false),
                CaeValidFrom = table.Column<DateTime>(type: "date", nullable: false),
                CaeValidTo = table.Column<DateTime>(type: "date", nullable: false),
                FiscalDate = table.Column<DateTime>(type: "date", nullable: false),
                LocationId = table.Column<string>(maxLength: 200, nullable: true),
                TerminalId = table.Column<string>(maxLength: 200, nullable: true),
                ReceiverIdentification = table.Column<int>(nullable: true),
                FormatVersion = table.Column<string>(maxLength: 40, nullable: false),
                ConfirmationFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                SettlementFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                CurrencyCode = table.Column<string>(maxLength: 3, nullable: false),
                NetAmount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                VatAmount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                TotalAmount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                Status = table.Column<int>(nullable: false),
                IdentityCreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_fiscal_documents", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_fd_req",
                    column: x => x.FiscalizationRequestId,
                    principalTable: "v1_fiscalization_requests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fd_sale",
                    column: x => x.SaleId,
                    principalTable: "v1_sales",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fd_res",
                    column: x => x.FiscalNumberReservationId,
                    principalTable: "v1_fiscal_number_reservations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_fd_cae",
                    column: x => x.CaeAuthorizationId,
                    principalTable: "v1_cae_authorizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_fd_org_req",
            table: "v1_fiscal_documents",
            columns: new[] { "OrganizationId", "FiscalizationRequestId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "UX_v1_fd_identity",
            table: "v1_fiscal_documents",
            columns: new[] { "OrganizationId", "CfeType", "Series", "Number" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "UX_v1_fd_res",
            table: "v1_fiscal_documents",
            column: "FiscalNumberReservationId",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_v1_fd_org_sale",
            table: "v1_fiscal_documents",
            columns: new[] { "OrganizationId", "SaleId" });
        migrationBuilder.CreateIndex(
            name: "IX_v1_fd_org_status",
            table: "v1_fiscal_documents",
            columns: new[] { "OrganizationId", "Status", "IdentityCreatedAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_v1_fd_req",
            table: "v1_fiscal_documents",
            column: "FiscalizationRequestId");
        migrationBuilder.CreateIndex(
            name: "IX_v1_fd_sale",
            table: "v1_fiscal_documents",
            column: "SaleId");
        migrationBuilder.CreateIndex(
            name: "IX_v1_fd_cae",
            table: "v1_fiscal_documents",
            column: "CaeAuthorizationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_fiscal_documents");
        migrationBuilder.DropColumn(name: "IdentityCreatedAtUtc", table: "v1_fiscalization_requests");
        migrationBuilder.DropColumn(name: "FiscalDocumentId", table: "v1_fiscalization_requests");
    }
}
