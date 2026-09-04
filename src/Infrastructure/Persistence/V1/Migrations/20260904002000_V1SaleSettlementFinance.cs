using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260904002000_V1SaleSettlementFinance")]
public sealed class V1SaleSettlementFinance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_payment_methods",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                Name = table.Column<string>(maxLength: 120, nullable: false),
                Enabled = table.Column<bool>(nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_v1_payment_methods", x => x.Id));

        migrationBuilder.CreateTable(
            name: "v1_payments",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                SaleId = table.Column<Guid>(nullable: false),
                Sequence = table.Column<int>(nullable: false),
                PaymentMethodId = table.Column<Guid>(nullable: false),
                PaymentMethodVersion = table.Column<long>(nullable: false),
                Amount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                CurrencyCode = table.Column<string>(maxLength: 3, nullable: false),
                ExternalReference = table.Column<string>(maxLength: 200, nullable: true),
                ConfirmationFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                SettlementFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                RecordedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_pay_sale",
                    column: x => x.SaleId,
                    principalTable: "v1_sales",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_pay_method",
                    column: x => x.PaymentMethodId,
                    principalTable: "v1_payment_methods",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "v1_receivables",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                CustomerPartyId = table.Column<Guid>(nullable: false),
                SaleId = table.Column<Guid>(nullable: false),
                OriginalAmount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                CurrencyCode = table.Column<string>(maxLength: 3, nullable: false),
                DueDate = table.Column<DateTime>(type: "date", nullable: false),
                ConfirmationFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                SettlementFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_receivables", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_ar_sale",
                    column: x => x.SaleId,
                    principalTable: "v1_sales",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_ar_customer",
                    column: x => x.CustomerPartyId,
                    principalTable: "v1_parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_v1_pm_org_enabled",
            table: "v1_payment_methods",
            columns: new[] { "OrganizationId", "Enabled" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_pay_org_sale",
            table: "v1_payments",
            columns: new[] { "OrganizationId", "SaleId" });
        migrationBuilder.CreateIndex(
            name: "IX_v1_pay_method",
            table: "v1_payments",
            column: "PaymentMethodId");
        migrationBuilder.CreateIndex(
            name: "UX_v1_pay_sale_plan_seq",
            table: "v1_payments",
            columns: new[] { "OrganizationId", "SaleId", "SettlementFingerprint", "Sequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_ar_org_sale",
            table: "v1_receivables",
            columns: new[] { "OrganizationId", "SaleId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_v1_ar_org_customer_due",
            table: "v1_receivables",
            columns: new[] { "OrganizationId", "CustomerPartyId", "DueDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_receivables");
        migrationBuilder.DropTable(name: "v1_payments");
        migrationBuilder.DropTable(name: "v1_payment_methods");
    }
}
