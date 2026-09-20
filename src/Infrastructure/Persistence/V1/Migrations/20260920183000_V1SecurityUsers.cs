using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260920183000_V1SecurityUsers")]
public sealed class V1SecurityUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_security_users",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 200, nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                IdentityProvider = table.Column<string>(maxLength: 120, nullable: false),
                NormalizedIdentityProvider = table.Column<string>(maxLength: 120, nullable: false),
                ExternalSubject = table.Column<string>(maxLength: 300, nullable: false),
                DisplayName = table.Column<string>(maxLength: 200, nullable: false),
                Email = table.Column<string>(maxLength: 320, nullable: true),
                Active = table.Column<bool>(nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(precision: 6, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_v1_security_users", x => x.Id));

        migrationBuilder.CreateTable(
            name: "v1_security_user_location_scopes",
            columns: table => new
            {
                UserId = table.Column<string>(maxLength: 200, nullable: false),
                LocationId = table.Column<string>(maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_security_user_location_scopes", x => new { x.UserId, x.LocationId });
                table.ForeignKey(
                    name: "FK_v1_security_user_location_user",
                    column: x => x.UserId,
                    principalTable: "v1_security_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "v1_security_user_terminal_scopes",
            columns: table => new
            {
                UserId = table.Column<string>(maxLength: 200, nullable: false),
                TerminalId = table.Column<string>(maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_security_user_terminal_scopes", x => new { x.UserId, x.TerminalId });
                table.ForeignKey(
                    name: "FK_v1_security_user_terminal_user",
                    column: x => x.UserId,
                    principalTable: "v1_security_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "v1_security_user_roles",
            columns: table => new
            {
                UserId = table.Column<string>(maxLength: 200, nullable: false),
                RoleId = table.Column<string>(maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_security_user_roles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_v1_security_user_role_user",
                    column: x => x.UserId,
                    principalTable: "v1_security_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_v1_security_user_role_role",
                    column: x => x.RoleId,
                    principalTable: "v1_security_roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_v1_security_user_org_identity",
            table: "v1_security_users",
            columns: new[] { "OrganizationId", "NormalizedIdentityProvider", "ExternalSubject" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_v1_security_user_org_active",
            table: "v1_security_users",
            columns: new[] { "OrganizationId", "Active" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_security_user_location_scope_location",
            table: "v1_security_user_location_scopes",
            column: "LocationId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_security_user_terminal_scope_terminal",
            table: "v1_security_user_terminal_scopes",
            column: "TerminalId");

        migrationBuilder.CreateIndex(
            name: "IX_v1_security_user_role_role",
            table: "v1_security_user_roles",
            column: "RoleId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_security_user_location_scopes");
        migrationBuilder.DropTable(name: "v1_security_user_terminal_scopes");
        migrationBuilder.DropTable(name: "v1_security_user_roles");
        migrationBuilder.DropTable(name: "v1_security_users");
    }
}
