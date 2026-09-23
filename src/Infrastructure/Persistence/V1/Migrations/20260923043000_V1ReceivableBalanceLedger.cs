using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260923043000_V1ReceivableBalanceLedger")]
public sealed class V1ReceivableBalanceLedger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_receivable_balance_effects",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                ReceivableId = table.Column<Guid>(nullable: false),
                Kind = table.Column<int>(nullable: false),
                Amount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                SourceId = table.Column<string>(maxLength: 200, nullable: false),
                SourceSequence = table.Column<int>(nullable: false),
                ReversesEffectId = table.Column<Guid>(nullable: true),
                OccurredAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_receivable_balance_effects", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_ar_effect_receivable",
                    column: x => x.ReceivableId,
                    principalTable: "v1_receivables",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_ar_effect_reversal",
                    column: x => x.ReversesEffectId,
                    principalTable: "v1_receivable_balance_effects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_v1_ar_effect_org_receivable_time",
            table: "v1_receivable_balance_effects",
            columns: new[] { "OrganizationId", "ReceivableId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "UX_v1_ar_effect_source",
            table: "v1_receivable_balance_effects",
            columns: new[] { "OrganizationId", "Kind", "SourceId", "SourceSequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_v1_ar_effect_reversal",
            table: "v1_receivable_balance_effects",
            columns: new[] { "OrganizationId", "ReversesEffectId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_receivable_balance_effects");
    }
}
