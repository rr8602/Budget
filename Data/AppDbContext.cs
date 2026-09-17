using HouseholdBudget.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Payer> Payers => Set<Payer>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<FixedItem> FixedItems => Set<FixedItem>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Installment>        Installments        => Set<Installment>();
    public DbSet<MonthlyFixedActual> MonthlyFixedActuals => Set<MonthlyFixedActual>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("TEXT");  // SQLite decimal 호환
            e.HasOne(x => x.Category).WithMany(c => c.Entries).HasForeignKey(x => x.CategoryId);
            e.HasOne(x => x.Payer).WithMany(p => p.Entries).HasForeignKey(x => x.PayerId);
            e.HasOne(x => x.PaymentMethod).WithMany(pm => pm.Entries).HasForeignKey(x => x.PaymentMethodId);
        });

        modelBuilder.Entity<FixedItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("TEXT");
            e.HasOne(x => x.Category).WithMany(c => c.FixedItems).HasForeignKey(x => x.CategoryId);
        });

        modelBuilder.Entity<Budget>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MonthlyExtra).HasColumnType("TEXT");
        });

        modelBuilder.Entity<MonthlyFixedActual>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ActualAmount).HasColumnType("TEXT");
            e.HasOne(x => x.FixedItem).WithMany().HasForeignKey(x => x.FixedItemId);
            e.HasIndex(x => new { x.FixedItemId, x.Year, x.Month }).IsUnique();
        });

        modelBuilder.Entity<Installment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalAmount).HasColumnType("TEXT");
            e.Ignore(x => x.MonthlyAmount);    // 계산 프로퍼티는 DB에 저장 안 함
            e.Ignore(x => x.RemainingMonths);
            e.Ignore(x => x.IsCompleted);
        });
    }
}
