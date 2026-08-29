using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write;

public sealed class V1PersistenceDbContext : DbContext
{
    public V1PersistenceDbContext(DbContextOptions<V1PersistenceDbContext> options)
        : base(options)
    {
    }

    public DbSet<V1AuditEventRecord> AuditEvents => Set<V1AuditEventRecord>();
    public DbSet<V1IdempotencyRecord> IdempotencyRecords => Set<V1IdempotencyRecord>();
    public DbSet<V1OutboxMessageRecord> OutboxMessages => Set<V1OutboxMessageRecord>();
    public DbSet<V1InboxMessageRecord> InboxMessages => Set<V1InboxMessageRecord>();

    public DbSet<V1PartyRecord> Parties => Set<V1PartyRecord>();
    public DbSet<V1PartyRoleRecord> PartyRoles => Set<V1PartyRoleRecord>();
    public DbSet<V1PartyFiscalIdentityRecord> PartyFiscalIdentities => Set<V1PartyFiscalIdentityRecord>();
    public DbSet<V1CommercialItemRecord> CommercialItems => Set<V1CommercialItemRecord>();
    public DbSet<V1ItemCategoryRecord> ItemCategories => Set<V1ItemCategoryRecord>();
    public DbSet<V1TaxProfileRecord> TaxProfiles => Set<V1TaxProfileRecord>();
    public DbSet<V1SaleRecord> Sales => Set<V1SaleRecord>();
    public DbSet<V1SaleLineRecord> SaleLines => Set<V1SaleLineRecord>();
    public DbSet<V1InventoryPositionRecord> InventoryPositions => Set<V1InventoryPositionRecord>();
    public DbSet<V1StockMovementRecord> StockMovements => Set<V1StockMovementRecord>();
    public DbSet<V1CaeAuthorizationRecord> CaeAuthorizations => Set<V1CaeAuthorizationRecord>();
    public DbSet<V1CaeAllocationRecord> CaeAllocations => Set<V1CaeAllocationRecord>();
    public DbSet<V1FiscalNumberReservationRecord> FiscalNumberReservations => Set<V1FiscalNumberReservationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureCrossCutting(modelBuilder);
        ConfigureParties(modelBuilder);
        ConfigureTaxation(modelBuilder);
        ConfigureCatalog(modelBuilder);
        ConfigureSales(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureCae(modelBuilder);
    }

    private static void ConfigureCrossCutting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1AuditEventRecord>(entity =>
        {
            entity.ToTable("v1_audit_events");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).ValueGeneratedNever();
            entity.Property(x => x.OccurredAtUtc).HasPrecision(6);
            entity.Property(x => x.EventName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ActorId).HasMaxLength(200);
            entity.Property(x => x.OrganizationId).HasMaxLength(200);
            entity.Property(x => x.LocationId).HasMaxLength(200);
            entity.Property(x => x.TerminalId).HasMaxLength(200);
            entity.Property(x => x.TargetType).HasMaxLength(200);
            entity.Property(x => x.TargetId).HasMaxLength(200);
            entity.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CausationId).HasMaxLength(128);
            entity.Property(x => x.MetadataJson).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.OccurredAtUtc });
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.EventName);
        });

        modelBuilder.Entity<V1IdempotencyRecord>(entity =>
        {
            entity.ToTable("v1_idempotency_records");
            entity.HasKey(x => new { x.Scope, x.KeyHash });
            entity.Property(x => x.Scope).HasMaxLength(160).IsRequired();
            entity.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RequestHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ActorId).HasMaxLength(200);
            entity.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.ExpiresAtUtc).HasPrecision(6);
            entity.Property(x => x.OutcomeCode).HasMaxLength(120);
            entity.Property(x => x.ResourceType).HasMaxLength(120);
            entity.Property(x => x.ResourceId).HasMaxLength(200);
            entity.Property(x => x.CompletedAtUtc).HasPrecision(6);
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<V1OutboxMessageRecord>(entity =>
        {
            entity.ToTable("v1_outbox_messages");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).ValueGeneratedNever();
            entity.Property(x => x.OccurredAtUtc).HasPrecision(6);
            entity.Property(x => x.EventType).HasMaxLength(500).IsRequired();
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CausationId).HasMaxLength(128);
            entity.Property(x => x.OrganizationId).HasMaxLength(200);
            entity.Property(x => x.ActorId).HasMaxLength(200);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.NextAttemptAtUtc).HasPrecision(6);
            entity.Property(x => x.ProcessedAtUtc).HasPrecision(6);
            entity.Property(x => x.LastErrorCode).HasMaxLength(120);
            entity.HasIndex(x => new { x.State, x.NextAttemptAtUtc });
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<V1InboxMessageRecord>(entity =>
        {
            entity.ToTable("v1_inbox_messages");
            entity.HasKey(x => new { x.Consumer, x.MessageIdHash });
            entity.Property(x => x.Consumer).HasMaxLength(200).IsRequired();
            entity.Property(x => x.MessageIdHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PayloadHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.ExpiresAtUtc).HasPrecision(6);
            entity.Property(x => x.OutcomeCode).HasMaxLength(120);
            entity.Property(x => x.CompletedAtUtc).HasPrecision(6);
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasIndex(x => x.CorrelationId);
        });
    }

    private static void ConfigureParties(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1PartyRecord>(entity =>
        {
            entity.ToTable("v1_parties");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
            entity.Property(x => x.ResidenceCountry).HasMaxLength(2).IsRequired();
            entity.Property(x => x.TaxResidenceCountry).HasMaxLength(2).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.Name });
            entity.HasIndex(x => new { x.OrganizationId, x.Active });
        });

        modelBuilder.Entity<V1PartyRoleRecord>(entity =>
        {
            entity.ToTable("v1_party_roles");
            entity.HasKey(x => new { x.PartyId, x.Role });
            entity.HasOne(x => x.Party)
                .WithMany(x => x.Roles)
                .HasForeignKey(x => x.PartyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<V1PartyFiscalIdentityRecord>(entity =>
        {
            entity.ToTable("v1_party_fiscal_identities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.TypeCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Number).HasMaxLength(80).IsRequired();
            entity.Property(x => x.IssuingCountry).HasMaxLength(2).IsRequired();
            entity.Property(x => x.ValidFromUtc).HasPrecision(6);
            entity.Property(x => x.ValidToUtc).HasPrecision(6);
            entity.HasOne(x => x.Party)
                .WithMany(x => x.FiscalIdentities)
                .HasForeignKey(x => x.PartyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.PartyId, x.Active });
            entity.HasIndex(x => new { x.TypeCode, x.Number, x.IssuingCountry });
        });
    }

    private static void ConfigureTaxation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1TaxProfileRecord>(entity =>
        {
            entity.ToTable("v1_tax_profiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
            entity.Property(x => x.TreatmentCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.RatePercent).HasPrecision(9, 4);
            entity.Property(x => x.EffectiveFromUtc).HasPrecision(6);
            entity.Property(x => x.EffectiveToUtc).HasPrecision(6);
            entity.Property(x => x.SourceName).HasMaxLength(250).IsRequired();
            entity.Property(x => x.SourceReference).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.SourceVersion).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.Code, x.EffectiveFromUtc }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.Active, x.EffectiveFromUtc });
        });
    }

    private static void ConfigureCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1CommercialItemRecord>(entity =>
        {
            entity.ToTable("v1_commercial_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Unit).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.Active });
            entity.HasOne<V1TaxProfileRecord>()
                .WithMany()
                .HasForeignKey(x => x.TaxProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<V1ItemCategoryRecord>(entity =>
        {
            entity.ToTable("v1_item_categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.Active });
        });
    }

    private static void ConfigureSales(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1SaleRecord>(entity =>
        {
            entity.ToTable("v1_sales");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LocationId).HasMaxLength(200);
            entity.Property(x => x.TerminalId).HasMaxLength(200);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.EffectiveOnUtc).HasPrecision(6);
            entity.Property(x => x.DeliveryCountry).HasMaxLength(2);
            entity.Property(x => x.ValidationFingerprint).HasMaxLength(64);
            entity.Property(x => x.ValidatedAtUtc).HasPrecision(6);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.EffectiveOnUtc });
            entity.HasIndex(x => new { x.OrganizationId, x.Status });
            entity.HasIndex(x => new { x.OrganizationId, x.CustomerPartyId });
            entity.HasOne<V1PartyRecord>()
                .WithMany()
                .HasForeignKey(x => x.CustomerPartyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<V1SaleLineRecord>(entity =>
        {
            entity.ToTable("v1_sale_lines");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.ItemCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ItemName).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(18, 6);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 6);
            entity.Property(x => x.ServiceUseCountry).HasMaxLength(2);
            entity.HasOne(x => x.Sale)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<V1CommercialItemRecord>()
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<V1TaxProfileRecord>()
                .WithMany()
                .HasForeignKey(x => x.TaxProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.SaleId);
            entity.HasIndex(x => x.ItemId);
        });
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1InventoryPositionRecord>(entity =>
        {
            entity.ToTable("v1_inventory_positions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LocationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(18, 6);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.ItemId, x.LocationId }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.LocationId });
            entity.HasOne<V1CommercialItemRecord>()
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<V1StockMovementRecord>(entity =>
        {
            entity.ToTable("v1_stock_movements");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LocationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.QuantityBefore).HasPrecision(18, 6);
            entity.Property(x => x.QuantityDelta).HasPrecision(18, 6);
            entity.Property(x => x.QuantityAfter).HasPrecision(18, 6);
            entity.Property(x => x.ReasonCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Explanation).HasMaxLength(1000);
            entity.Property(x => x.OccurredAtUtc).HasPrecision(6);
            entity.HasOne(x => x.Position)
                .WithMany(x => x.Movements)
                .HasForeignKey(x => x.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<V1CommercialItemRecord>()
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.PositionId);
            entity.HasIndex(x => x.ItemId);
            entity.HasIndex(x => new { x.OrganizationId, x.LocationId, x.OccurredAtUtc });
        });
    }

    private static void ConfigureCae(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1CaeAuthorizationRecord>(entity =>
        {
            entity.ToTable("v1_cae_authorizations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.AuthorizationNumber).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Series).HasMaxLength(20).IsRequired();
            entity.Property(x => x.VerificationMethod).HasMaxLength(80).IsRequired();
            entity.Property(x => x.SourceArtifactId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SourceArtifactHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.SourceName).HasMaxLength(250).IsRequired();
            entity.Property(x => x.SourceReference).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.ImportedAtUtc).HasPrecision(6);
            entity.Property(x => x.ActivatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.SourceArtifactHash }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.CfeType, x.Series, x.RangeFrom, x.RangeTo });
            entity.HasIndex(x => new { x.OrganizationId, x.CfeType, x.Status, x.ValidTo });
        });

        modelBuilder.Entity<V1CaeAllocationRecord>(entity =>
        {
            entity.ToTable("v1_cae_allocations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LocationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TerminalId).HasMaxLength(200);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.ClosedAtUtc).HasPrecision(6);
            entity.HasOne(x => x.Authorization)
                .WithMany(x => x.Allocations)
                .HasForeignKey(x => x.CaeAuthorizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.CaeAuthorizationId);
            entity.HasIndex(x => new { x.OrganizationId, x.LocationId, x.Status });
            entity.HasIndex(x => new { x.CaeAuthorizationId, x.RangeFrom, x.RangeTo });
        });

        modelBuilder.Entity<V1FiscalNumberReservationRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_number_reservations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Series).HasMaxLength(20).IsRequired();
            entity.Property(x => x.LocationId).HasMaxLength(200);
            entity.Property(x => x.TerminalId).HasMaxLength(200);
            entity.Property(x => x.OperationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ReservedAtUtc).HasPrecision(6);
            entity.HasOne(x => x.Authorization)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.CaeAuthorizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.CaeAuthorizationId);
            entity.HasIndex(x => x.AllocationId);
            entity.HasIndex(x => new { x.OrganizationId, x.CfeType, x.Series, x.Number }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.OperationId }).IsUnique();
        });
    }
}
