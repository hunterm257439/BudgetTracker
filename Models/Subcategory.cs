using System.ComponentModel.DataAnnotations;

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

    // Only used when TargetPeriod == Custom; stores the number of days in the custom period.
    [Range(1, int.MaxValue, ErrorMessage = "Custom period must be at least 1 day.")]
    public int? TargetCustomDays { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<BudgetAssignment> BudgetAssignments { get; set; } = new List<BudgetAssignment>();
}
