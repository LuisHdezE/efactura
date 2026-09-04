using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.V1;

public sealed class V1PersistenceModelCustomizer : ModelCustomizer
{
    public V1PersistenceModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        ConfigureFinance(modelBuilder);
    }

    private static void ConfigureFinance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1PaymentMethodRecord>(entity =>
        {
            entity.ToTable("v1_payment_methods");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(6);
            entity.HasIndex(x => new { x.OrganizationId, x.Enabled })
                .HasDatabaseName("IX_v1_pm_org_enabled");
        });

        modelBuilder.Entity<V1PaymentRecord>(entity =>
        {
            entity.ToTable("v1_payments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 6);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.ExternalReference).HasMaxLength(200);
            entity.Property(x => x.ConfirmationFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SettlementFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RecordedAtUtc).HasPrecision(6);
            entity.HasOne<V1SaleRecord>()
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_pay_sale");
            entity.HasOne<V1PaymentMethodRecord>()
                .WithMany()
                .HasForeignKey(x => x.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_pay_method");
            entity.HasIndex(x => new { x.OrganizationId, x.SaleId })
                .HasDatabaseName("IX_v1_pay_org_sale");
            entity.HasIndex(x => x.PaymentMethodId)
                .HasDatabaseName("IX_v1_pay_method");
            entity.HasIndex(x => new { x.OrganizationId, x.SaleId, x.SettlementFingerprint, x.Sequence })
                .IsUnique()
                .HasDatabaseName("UX_v1_pay_sale_plan_seq");
        });

        modelBuilder.Entity<V1ReceivableRecord>(entity =>
        {
            entity.ToTable("v1_receivables");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OriginalAmount).HasPrecision(18, 6);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.DueDate).HasColumnType("date");
            entity.Property(x => x.ConfirmationFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SettlementFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(6);
            entity.HasOne<V1SaleRecord>()
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_ar_sale");
            entity.HasOne<V1PartyRecord>()
                .WithMany()
                .HasForeignKey(x => x.CustomerPartyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_ar_customer");
            entity.HasIndex(x => new { x.OrganizationId, x.SaleId })
                .IsUnique()
                .HasDatabaseName("UX_v1_ar_org_sale");
            entity.HasIndex(x => new { x.OrganizationId, x.CustomerPartyId, x.DueDate })
                .HasDatabaseName("IX_v1_ar_org_customer_due");
        });
    }
}
