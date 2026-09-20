using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260920040000_V1SecurityRoles")]
public sealed class V1SecurityRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_security_roles",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 200, nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                Name = table.Column<string>(maxLength: 120, nullable: false),
                NormalizedName = table.Column<string>(maxLength: 120, nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                Active = table.Column<bool>(nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_v1_security_roles", x => x.Id));

        migrationBuilder.CreateTable(
            name: "v1_security_role_permissions",
            columns: table => new
            {
                RoleId = table.Column<string>(maxLength: 200, nullable: false),
                PermissionCode = table.Column<string>(maxLength: 160, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_v1_security_role_permissions",
                    x => new { x.RoleId, x.PermissionCode });
                table.ForeignKey(
                    name: "FK_v1_security_role_permission_role",
                    column: x => x.RoleId,
                    principalTable: "v1_security_roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_security_role_org_name",
            table: "v1_security_roles",
            columns: new[] { "OrganizationId", "NormalizedName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_security_role_org_active",
            table: "v1_security_roles",
            columns: new[] { "OrganizationId", "Active" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_security_role_permission_code",
            table: "v1_security_role_permissions",
            column: "PermissionCode");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_security_role_permissions");
        migrationBuilder.DropTable(name: "v1_security_roles");
    }
}
