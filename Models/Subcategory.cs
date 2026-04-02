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

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<BudgetAssignment> BudgetAssignments { get; set; } = new List<BudgetAssignment>();
}
