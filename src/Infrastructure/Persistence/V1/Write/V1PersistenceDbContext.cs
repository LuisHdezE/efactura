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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
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
}
