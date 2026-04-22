using BudgetTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Subcategory> Subcategories { get; set; }
    public DbSet<BankAccount> BankAccounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<BudgetAssignment> BudgetAssignments { get; set; }
    public DbSet<BudgetSettings> BudgetSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ensure only one BudgetAssignment per subcategory per month/year
        modelBuilder.Entity<BudgetAssignment>()
            .HasIndex(b => new { b.SubcategoryId, b.Month, b.Year })
            .IsUnique();
    }
}
