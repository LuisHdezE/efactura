using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260909222000_V1FiscalSignedArtifactSchemaEvidence")]
public sealed class V1FiscalSignedArtifactSchemaEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SchemaSetId",
            table: "v1_fiscal_signed_artifacts",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SchemaVersion",
            table: "v1_fiscal_signed_artifacts",
            maxLength: 40,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SchemaSetFingerprint",
            table: "v1_fiscal_signed_artifacts",
            maxLength: 64,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SchemaSetFingerprint", table: "v1_fiscal_signed_artifacts");
        migrationBuilder.DropColumn(name: "SchemaVersion", table: "v1_fiscal_signed_artifacts");
        migrationBuilder.DropColumn(name: "SchemaSetId", table: "v1_fiscal_signed_artifacts");
    }
}
