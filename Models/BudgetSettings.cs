using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.Models;

// A single-row settings table — we always use Id = 1.
// Add more app-wide settings here as new properties in the future.
public class BudgetSettings
{
    public int Id { get; set; }

    // The user's expected take-home income per month. Null means not set yet.
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ExpectedMonthlyIncome { get; set; }
}
