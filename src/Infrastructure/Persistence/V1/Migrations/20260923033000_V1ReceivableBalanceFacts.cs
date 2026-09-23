using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260923033000_V1ReceivableBalanceFacts")]
public sealed class V1ReceivableBalanceFacts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_receivable_balance_facts",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                ReceivableId = table.Column<Guid>(nullable: false),
                Kind = table.Column<int>(nullable: false),
                Amount = table.Column<decimal>(precision: 18, scale: 6, nullable: false),
                EffectiveOn = table.Column<DateTime>(type: "date", nullable: false),
                ReversalOfFactId = table.Column<Guid>(nullable: true),
                RecordedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_receivable_balance_facts", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_ar_fact_receivable",
                    column: x => x.ReceivableId,
                    principalTable: "v1_receivables",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_v1_ar_fact_reversal",
                    column: x => x.ReversalOfFactId,
                    principalTable: "v1_receivable_balance_facts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_v1_ar_fact_org_receivable_effective",
            table: "v1_receivable_balance_facts",
            columns: new[] { "OrganizationId", "ReceivableId", "EffectiveOn" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_ar_fact_receivable",
            table: "v1_receivable_balance_facts",
            column: "ReceivableId");

        migrationBuilder.CreateIndex(
            name: "UX_v1_ar_fact_reversal_of",
            table: "v1_receivable_balance_facts",
            column: "ReversalOfFactId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "v1_receivable_balance_facts");
}
