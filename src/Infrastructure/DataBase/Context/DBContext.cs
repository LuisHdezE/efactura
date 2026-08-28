using System;
using System.Collections.Generic;
using ApplicationCore.Entites;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataBase.Context;

public partial class DBContext : DbContext
{
    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CashTransactions> CashTransactions { get; set; }

    public virtual DbSet<CustomerTypes> CustomerTypes { get; set; }

    public virtual DbSet<Customers> Customers { get; set; }

    public virtual DbSet<Invoices> Invoices { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Payments> Payments { get; set; }

    public virtual DbSet<ProductCategories> ProductCategories { get; set; }

    public virtual DbSet<Products> Products { get; set; }

    public virtual DbSet<PurchaseOrders> PurchaseOrders { get; set; }

    public virtual DbSet<SupplierTypes> SupplierTypes { get; set; }

    public virtual DbSet<Suppliers> Suppliers { get; set; }

    public virtual DbSet<TaxTypes> TaxTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CashTransactions>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CashTransactions_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"CashTransactionsIdSeq\"'::regclass)");
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.TransactionType)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });

        modelBuilder.Entity<CustomerTypes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CustomerTypes_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"CustomerTypesIdSeq\"'::regclass)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });

        modelBuilder.Entity<Customers>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Customers_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"CustomersIdSeq\"'::regclass)");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });

        modelBuilder.Entity<Invoices>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Invoices_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"InvoicesIdSeq\"'::regclass)");
            entity.Property(e => e.AmountDue).HasPrecision(10, 2);
            entity.Property(e => e.AmountPaid).HasPrecision(10, 2);
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.InvoiceDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PaymentMethods_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"PaymentMethodsIdSeq\"'::regclass)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });

        modelBuilder.Entity<Payments>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Payments_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"PaymentsIdSeq\"'::regclass)");
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });

        modelBuilder.Entity<ProductCategories>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ProductCategories_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"ProductCategoriesIdSeq\"'::regclass)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });

        modelBuilder.Entity<Products>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Products_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"ProductsIdSeq\"'::regclass)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });

        modelBuilder.Entity<PurchaseOrders>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PurchaseOrders_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"PurchaseOrdersIdSeq\"'::regclass)");
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });

        modelBuilder.Entity<SupplierTypes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SupplierTypes_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"SupplierTypesIdSeq\"'::regclass)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });

        modelBuilder.Entity<Suppliers>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Suppliers_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"SuppliersIdSeq\"'::regclass)");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.ContactName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });

        modelBuilder.Entity<TaxTypes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TaxTypes_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"TaxTypesIdSeq\"'::regclass)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp(6) without time zone");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Rate).HasPrecision(5, 2);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(6) without time zone");
        });
        modelBuilder.HasSequence("cashtransactionsidseq");
        modelBuilder.HasSequence("CashTransactionsIdSeq");
        modelBuilder.HasSequence("customersidseq");
        modelBuilder.HasSequence("CustomersIdSeq");
        modelBuilder.HasSequence("customertypesidseq");
        modelBuilder.HasSequence("CustomerTypesIdSeq");
        modelBuilder.HasSequence("invoicesidseq");
        modelBuilder.HasSequence("InvoicesIdSeq");
        modelBuilder.HasSequence("paymentmethodsidseq");
        modelBuilder.HasSequence("PaymentMethodsIdSeq");
        modelBuilder.HasSequence("paymentsidseq");
        modelBuilder.HasSequence("PaymentsIdSeq");
        modelBuilder.HasSequence("productcategoriesidseq");
        modelBuilder.HasSequence("ProductCategoriesIdSeq");
        modelBuilder.HasSequence("productsidseq");
        modelBuilder.HasSequence("ProductsIdSeq");
        modelBuilder.HasSequence("purchaseordersidseq");
        modelBuilder.HasSequence("PurchaseOrdersIdSeq");
        modelBuilder.HasSequence("suppliersidseq");
        modelBuilder.HasSequence("SuppliersIdSeq");
        modelBuilder.HasSequence("suppliertypesidseq");
        modelBuilder.HasSequence("SupplierTypesIdSeq");
        modelBuilder.HasSequence("taxtypesidseq");
        modelBuilder.HasSequence("TaxTypesIdSeq");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
