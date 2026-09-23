using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260923143000_V1PayableBalanceFoundation")]
public sealed class V1PayableBalanceFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_payables",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                SupplierPartyId = table.Column<Guid>(nullable: false),
                SourceKind = table.Column<int>(nullable: false),
                SourceId = table.Column<string>(maxLength: 200, nullable: false),
                OriginalAmount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                CurrencyCode = table.Column<string>(maxLength: 3, nullable: false),
                SourceEffectiveOn = table.Column<DateTime>(type: "date", nullable: false),
                DueDate = table.Column<DateTime>(type: "date", nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_payables", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_ap_supplier",
                    column: x => x.SupplierPartyId,
                    principalTable: "v1_parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "v1_payable_balance_effects",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                PayableId = table.Column<Guid>(nullable: false),
                Kind = table.Column<int>(nullable: false),
                Amount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                SourceId = table.Column<string>(maxLength: 200, nullable: false),
                SourceSequence = table.Column<int>(nullable: false),
                ReversesEffectId = table.Column<Guid>(nullable: true),
                OccurredAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_payable_balance_effects", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_ap_effect_payable",
                    column: x => x.PayableId,
                    principalTable: "v1_payables",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_ap_effect_reversal",
                    column: x => x.ReversesEffectId,
                    principalTable: "v1_payable_balance_effects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_ap_org_source",
            table: "v1_payables",
            columns: new[] { "OrganizationId", "SourceKind", "SourceId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_ap_org_supplier_due",
            table: "v1_payables",
            columns: new[] { "OrganizationId", "SupplierPartyId", "DueDate" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_ap_supplier",
            table: "v1_payables",
            column: "SupplierPartyId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_ap_effect_org_payable_time",
            table: "v1_payable_balance_effects",
            columns: new[] { "OrganizationId", "PayableId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "UX_v1_ap_effect_source",
            table: "v1_payable_balance_effects",
            columns: new[] { "OrganizationId", "Kind", "SourceId", "SourceSequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_ap_effect_reversal",
            table: "v1_payable_balance_effects",
            columns: new[] { "OrganizationId", "ReversesEffectId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_ap_effect_payable",
            table: "v1_payable_balance_effects",
            column: "PayableId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_payable_balance_effects");
        migrationBuilder.DropTable(name: "v1_payables");
    }
}
