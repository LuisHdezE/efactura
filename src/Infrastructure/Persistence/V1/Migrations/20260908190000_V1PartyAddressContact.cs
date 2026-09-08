using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260908190000_V1PartyAddressContact")]
public sealed class V1PartyAddressContact : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_party_addresses",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                PartyId = table.Column<Guid>(nullable: false),
                Kind = table.Column<int>(nullable: false),
                AddressLine = table.Column<string>(maxLength: 255, nullable: false),
                City = table.Column<string>(maxLength: 80, nullable: false),
                Region = table.Column<string>(maxLength: 100, nullable: true),
                CountryCode = table.Column<string>(maxLength: 2, nullable: false),
                PostalCode = table.Column<string>(maxLength: 20, nullable: true),
                Primary = table.Column<bool>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_party_addresses", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_party_addresses_v1_parties_PartyId",
                    column: x => x.PartyId,
                    principalTable: "v1_parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "v1_party_contacts",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                PartyId = table.Column<Guid>(nullable: false),
                TypeCode = table.Column<string>(maxLength: 80, nullable: false),
                Value = table.Column<string>(maxLength: 100, nullable: false),
                Primary = table.Column<bool>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_party_contacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_party_contacts_v1_parties_PartyId",
                    column: x => x.PartyId,
                    principalTable: "v1_parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_v1_party_address_party_kind_primary",
            table: "v1_party_addresses",
            columns: new[] { "PartyId", "Kind", "Primary" });

        migrationBuilder.CreateIndex(
            name: "IX_v1_party_contact_party_type_primary",
            table: "v1_party_contacts",
            columns: new[] { "PartyId", "TypeCode", "Primary" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "v1_party_addresses");
        migrationBuilder.DropTable(name: "v1_party_contacts");
    }
}
