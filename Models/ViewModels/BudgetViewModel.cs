namespace BudgetTracker.Models.ViewModels;

// The top-level view model passed to the Budget/Index view
public class BudgetViewModel
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

    // Sum of all non-Investment account balances
    public decimal AvailableToBudget { get; set; }

    public List<CategoryBudgetRow> Categories { get; set; } = new();

    // Grand totals across all categories
    public decimal TotalAssigned => Categories.Sum(c => c.TotalAssigned);
    public decimal TotalActivity => Categories.Sum(c => c.TotalActivity);
    public decimal TotalAvailable => Categories.Sum(c => c.TotalAvailable);
}

// One row per Category (header row in the table)
public class CategoryBudgetRow
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public List<SubcategoryBudgetRow> Subcategories { get; set; } = new();

    // Category totals are the sum of its subcategory rows
    public decimal TotalAssigned => Subcategories.Sum(s => s.Assigned);
    public decimal TotalActivity => Subcategories.Sum(s => s.Activity);
    public decimal TotalAvailable => Subcategories.Sum(s => s.Available);
}

// One row per Subcategory (detail row in the table)
public class SubcategoryBudgetRow
{
    public int SubcategoryId { get; set; }
    public string SubcategoryName { get; set; } = string.Empty;

    // Amount the user allocated to this subcategory this month
    public decimal Assigned { get; set; }

    // Net transaction activity this month: sum(Outflow) - sum(Inflow)
    // Positive = net spending, Negative = net income
    public decimal Activity { get; set; }

    // Rolling available balance: cumulative (Assigned - Activity) from all time up to this month
    public decimal Available { get; set; }
}
