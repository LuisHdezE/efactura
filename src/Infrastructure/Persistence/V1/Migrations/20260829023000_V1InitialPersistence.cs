using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.V1.Migrations;

[DbContext(typeof(V1PersistenceDbContext))]
[Migration("20260829023000_V1InitialPersistence")]
public sealed class V1InitialPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "v1_audit_events",
            columns: table => new
            {
                EventId = table.Column<Guid>(nullable: false),
                OccurredAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                EventName = table.Column<string>(maxLength: 200, nullable: false),
                ActorId = table.Column<string>(maxLength: 200, nullable: true),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: true),
                LocationId = table.Column<string>(maxLength: 200, nullable: true),
                TerminalId = table.Column<string>(maxLength: 200, nullable: true),
                TargetType = table.Column<string>(maxLength: 200, nullable: true),
                TargetId = table.Column<string>(maxLength: 200, nullable: true),
                Outcome = table.Column<int>(nullable: false),
                CorrelationId = table.Column<string>(maxLength: 128, nullable: false),
                CausationId = table.Column<string>(maxLength: 128, nullable: true),
                MetadataJson = table.Column<string>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_v1_audit_events", x => x.EventId));

        migrationBuilder.CreateTable(
            name: "v1_idempotency_records",
            columns: table => new
            {
                Scope = table.Column<string>(maxLength: 160, nullable: false),
                KeyHash = table.Column<string>(maxLength: 64, nullable: false),
                RequestHash = table.Column<string>(maxLength: 128, nullable: false),
                ActorId = table.Column<string>(maxLength: 200, nullable: true),
                CorrelationId = table.Column<string>(maxLength: 128, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                State = table.Column<int>(nullable: false),
                OutcomeCode = table.Column<string>(maxLength: 120, nullable: true),
                ResourceType = table.Column<string>(maxLength: 120, nullable: true),
                ResourceId = table.Column<string>(maxLength: 200, nullable: true),
                CompletedAtUtc = table.Column<DateTime>(precision: 6, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_v1_idempotency_records", x => new { x.Scope, x.KeyHash }));

        migrationBuilder.CreateTable(
            name: "v1_inbox_messages",
            columns: table => new
            {
                Consumer = table.Column<string>(maxLength: 200, nullable: false),
                MessageIdHash = table.Column<string>(maxLength: 64, nullable: false),
                PayloadHash = table.Column<string>(maxLength: 128, nullable: false),
                CorrelationId = table.Column<string>(maxLength: 128, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                State = table.Column<int>(nullable: false),
                OutcomeCode = table.Column<string>(maxLength: 120, nullable: true),
                CompletedAtUtc = table.Column<DateTime>(precision: 6, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_v1_inbox_messages", x => new { x.Consumer, x.MessageIdHash }));

        migrationBuilder.CreateTable(
            name: "v1_outbox_messages",
            columns: table => new
            {
                EventId = table.Column<Guid>(nullable: false),
                OccurredAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                EventType = table.Column<string>(maxLength: 500, nullable: false),
                PayloadJson = table.Column<string>(nullable: false),
                CorrelationId = table.Column<string>(maxLength: 128, nullable: false),
                CausationId = table.Column<string>(maxLength: 128, nullable: true),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: true),
                ActorId = table.Column<string>(maxLength: 200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                State = table.Column<int>(nullable: false),
                AttemptCount = table.Column<int>(nullable: false),
                NextAttemptAtUtc = table.Column<DateTime>(precision: 6, nullable: true),
                ProcessedAtUtc = table.Column<DateTime>(precision: 6, nullable: true),
                LastErrorCode = table.Column<string>(maxLength: 120, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_v1_outbox_messages", x => x.EventId));

        migrationBuilder.CreateTable(
            name: "v1_parties",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                Kind = table.Column<int>(nullable: false),
                Name = table.Column<string>(maxLength: 250, nullable: false),
                ResidenceCountry = table.Column<string>(maxLength: 2, nullable: false),
                TaxResidenceCountry = table.Column<string>(maxLength: 2, nullable: false),
                Active = table.Column<bool>(nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_v1_parties", x => x.Id));

        migrationBuilder.CreateTable(
            name: "v1_commercial_items",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                Code = table.Column<string>(maxLength: 80, nullable: false),
                Name = table.Column<string>(maxLength: 250, nullable: false),
                Description = table.Column<string>(maxLength: 1000, nullable: true),
                Kind = table.Column<int>(nullable: false),
                Unit = table.Column<string>(maxLength: 40, nullable: false),
                TrackInventory = table.Column<bool>(nullable: false),
                TaxProfileId = table.Column<Guid>(nullable: true),
                CategoryId = table.Column<Guid>(nullable: true),
                Active = table.Column<bool>(nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_v1_commercial_items", x => x.Id));

        migrationBuilder.CreateTable(
            name: "v1_item_categories",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                Code = table.Column<string>(maxLength: 80, nullable: false),
                Name = table.Column<string>(maxLength: 250, nullable: false),
                Active = table.Column<bool>(nullable: false),
                Version = table.Column<long>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(precision: 6, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_v1_item_categories", x => x.Id));

        migrationBuilder.CreateTable(
            name: "v1_party_roles",
            columns: table => new
            {
                PartyId = table.Column<Guid>(nullable: false),
                Role = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_party_roles", x => new { x.PartyId, x.Role });
                table.ForeignKey(
                    name: "FK_v1_party_roles_v1_parties_PartyId",
                    column: x => x.PartyId,
                    principalTable: "v1_parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "v1_party_fiscal_identities",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                PartyId = table.Column<Guid>(nullable: false),
                TypeCode = table.Column<string>(maxLength: 32, nullable: false),
                Number = table.Column<string>(maxLength: 80, nullable: false),
                IssuingCountry = table.Column<string>(maxLength: 2, nullable: false),
                ValidFromUtc = table.Column<DateTime>(precision: 6, nullable: true),
                ValidToUtc = table.Column<DateTime>(precision: 6, nullable: true),
                Active = table.Column<bool>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_v1_party_fiscal_identities", x => x.Id);
                table.ForeignKey(
                    name: "FK_v1_party_fiscal_identities_v1_parties_PartyId",
                    column: x => x.PartyId,
                    principalTable: "v1_parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_v1_audit_events_OrganizationId_OccurredAtUtc", "v1_audit_events", new[] { "OrganizationId", "OccurredAtUtc" });
        migrationBuilder.CreateIndex("IX_v1_audit_events_CorrelationId", "v1_audit_events", "CorrelationId");
        migrationBuilder.CreateIndex("IX_v1_audit_events_EventName", "v1_audit_events", "EventName");
        migrationBuilder.CreateIndex("IX_v1_idempotency_records_ExpiresAtUtc", "v1_idempotency_records", "ExpiresAtUtc");
        migrationBuilder.CreateIndex("IX_v1_idempotency_records_CorrelationId", "v1_idempotency_records", "CorrelationId");
        migrationBuilder.CreateIndex("IX_v1_inbox_messages_ExpiresAtUtc", "v1_inbox_messages", "ExpiresAtUtc");
        migrationBuilder.CreateIndex("IX_v1_inbox_messages_CorrelationId", "v1_inbox_messages", "CorrelationId");
        migrationBuilder.CreateIndex("IX_v1_outbox_messages_State_NextAttemptAtUtc", "v1_outbox_messages", new[] { "State", "NextAttemptAtUtc" });
        migrationBuilder.CreateIndex("IX_v1_outbox_messages_CorrelationId", "v1_outbox_messages", "CorrelationId");
        migrationBuilder.CreateIndex("IX_v1_parties_OrganizationId_Name", "v1_parties", new[] { "OrganizationId", "Name" });
        migrationBuilder.CreateIndex("IX_v1_parties_OrganizationId_Active", "v1_parties", new[] { "OrganizationId", "Active" });
        migrationBuilder.CreateIndex("IX_v1_party_fiscal_identities_PartyId_Active", "v1_party_fiscal_identities", new[] { "PartyId", "Active" });
        migrationBuilder.CreateIndex("IX_v1_party_fiscal_identities_TypeCode_Number_IssuingCountry", "v1_party_fiscal_identities", new[] { "TypeCode", "Number", "IssuingCountry" });
        migrationBuilder.CreateIndex("IX_v1_commercial_items_OrganizationId_Code", "v1_commercial_items", new[] { "OrganizationId", "Code" }, unique: true);
        migrationBuilder.CreateIndex("IX_v1_commercial_items_OrganizationId_Active", "v1_commercial_items", new[] { "OrganizationId", "Active" });
        migrationBuilder.CreateIndex("IX_v1_item_categories_OrganizationId_Code", "v1_item_categories", new[] { "OrganizationId", "Code" }, unique: true);
        migrationBuilder.CreateIndex("IX_v1_item_categories_OrganizationId_Active", "v1_item_categories", new[] { "OrganizationId", "Active" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("v1_party_fiscal_identities");
        migrationBuilder.DropTable("v1_party_roles");
        migrationBuilder.DropTable("v1_commercial_items");
        migrationBuilder.DropTable("v1_item_categories");
        migrationBuilder.DropTable("v1_parties");
        migrationBuilder.DropTable("v1_outbox_messages");
        migrationBuilder.DropTable("v1_inbox_messages");
        migrationBuilder.DropTable("v1_idempotency_records");
        migrationBuilder.DropTable("v1_audit_events");
    }
}
