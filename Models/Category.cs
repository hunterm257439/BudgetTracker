using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    // Controls display order on the budget and categories pages.
    // Lower numbers appear first. Set via drag-and-drop.
    public int SortOrder { get; set; }

    public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
}
