using Microsoft.EntityFrameworkCore;
using MilkHisab.Api.Models;

namespace MilkHisab.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<DailyEntry> DailyEntries => Set<DailyEntry>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<LedgerUser> LedgerUsers => Set<LedgerUser>();
    public DbSet<LedgerProduct> LedgerProducts => Set<LedgerProduct>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpensePayment> ExpensePayments => Set<ExpensePayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<LedgerUser>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<LedgerProduct>().HasOne<LedgerUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Expense>().HasOne<LedgerUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Expense>().HasOne<LedgerProduct>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Expense>().HasIndex(x => new { x.OwnerId, x.Date });
        modelBuilder.Entity<ExpensePayment>().HasOne<LedgerUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ExpensePayment>().HasOne<Expense>().WithMany().HasForeignKey(x => x.ExpenseId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyEntry>(e =>
        {
            e.HasIndex(x => new { x.CustomerId, x.Date }).IsUnique();
            e.HasOne(x => x.Customer)
                .WithMany(c => c.DailyEntries)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.QuantityLitres).HasPrecision(10, 3);
            e.Property(x => x.Rate).HasPrecision(10, 2);
            e.Ignore(x => x.Amount);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasIndex(x => x.ReceiptNumber).IsUnique();
            e.HasOne(x => x.Customer)
                .WithMany(c => c.Payments)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.Property(x => x.Mode).HasMaxLength(32);
            e.Property(x => x.ReceiptNumber).HasMaxLength(32);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.DefaultRate).HasPrecision(10, 2);
        });
    }
}

