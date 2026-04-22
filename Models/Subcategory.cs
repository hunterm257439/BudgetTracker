using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.Models;

public class Subcategory
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Controls display order within its category. Set via drag-and-drop.
    public int SortOrder { get; set; }

    // Optional spending target. All three fields are null when no target is set.
    [Range(0.01, double.MaxValue, ErrorMessage = "Target amount must be greater than zero.")]
    public decimal? TargetAmount { get; set; }
    public TargetPeriod? TargetPeriod { get; set; }

    // Day the target amount is needed: day of month (1–31) for Monthly, day of year (1–366) for Yearly.
    // Maps to the existing TargetCustomDays column in the database.
    [Column("TargetCustomDays")]
    [Range(1, 366, ErrorMessage = "Day must be between 1 and 366.")]
    public int? TargetDay { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<BudgetAssignment> BudgetAssignments { get; set; } = new List<BudgetAssignment>();
}
