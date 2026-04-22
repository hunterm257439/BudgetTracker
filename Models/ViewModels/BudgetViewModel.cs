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

    // ── Feature 3: Income vs. Targets ───────────────────────────────────────

    // Expected monthly take-home income loaded from BudgetSettings (Id=1).
    // Null when the user hasn't set it yet.
    public decimal? ExpectedMonthlyIncome { get; set; }

    // Sum of MonthlyTargetAmount across all subcategories that have a target set.
    // Yearly targets are already converted to monthly (÷12) on the row itself.
    public decimal TotalMonthlyTargets =>
        Categories.SelectMany(c => c.Subcategories)
                  .Where(s => s.HasTarget)
                  .Sum(s => s.MonthlyTargetAmount);

    // Positive = surplus (income exceeds targets), negative = deficit.
    // Null when income has not been set.
    public decimal? IncomeSurplusOrDeficit =>
        ExpectedMonthlyIncome.HasValue ? ExpectedMonthlyIncome.Value - TotalMonthlyTargets : null;

    // ── Feature 2: Rollover hint ─────────────────────────────────────────────

    // True when no assignments have been made this month but some subcategories
    // have Available balances carried forward from prior months.
    // Used to show a "New month?" hint to the user.
    public bool IsUnassignedNewMonth =>
        TotalAssigned == 0 && Categories.SelectMany(c => c.Subcategories).Any(s => s.Available > 0);
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

    // Optional target set on the subcategory
    public decimal? TargetAmount { get; set; }
    public TargetPeriod? Period { get; set; }
    public int? TargetDay { get; set; }

    // True when a target is fully configured
    public bool HasTarget => TargetAmount.HasValue && Period.HasValue;

    // How much should be available per month to stay on track.
    // Monthly targets apply as-is; yearly targets are divided across 12 months.
    public decimal MonthlyTargetAmount => HasTarget
        ? (Period == TargetPeriod.Yearly ? TargetAmount!.Value / 12m : TargetAmount!.Value)
        : 0m;

    // True when the available balance meets or exceeds the monthly target
    public bool TargetMet => HasTarget && Available >= MonthlyTargetAmount;

    // ── Feature 1: Progress bar ──────────────────────────────────────────────

    // Percentage of MonthlyTargetAmount covered by Available, capped 0–100.
    // Returns 0 when no target is set or when Available is negative (overspent).
    public int TargetProgressPercent
    {
        get
        {
            if (!HasTarget || MonthlyTargetAmount <= 0) return 0;
            var pct = (Available / MonthlyTargetAmount) * 100m;
            return (int)Math.Clamp(pct, 0m, 100m);
        }
    }

    // ── Feature 2: Rollover hint ─────────────────────────────────────────────

    // True when nothing was assigned this month but there is positive Available
    // carried over from prior months. Used to show a "carried" badge on the row.
    public bool HasCarryForward => Assigned == 0 && Available > 0;
}
