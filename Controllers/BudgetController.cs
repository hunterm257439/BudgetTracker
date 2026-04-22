using BudgetTracker.Data;
using BudgetTracker.Models;
using BudgetTracker.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Controllers;

public class BudgetController : Controller
{
    private readonly AppDbContext _db;

    public BudgetController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /Budget  or  /Budget?month=3&year=2026
    public async Task<IActionResult> Index(int? month, int? year)
    {
        var today = DateTime.Today;
        var selectedMonth = month ?? today.Month;
        var selectedYear = year ?? today.Year;

        // ── Account balance (raw money in non-investment accounts) ───────────
        var accounts = await _db.BankAccounts
            .Where(a => a.AccountType != AccountType.Investment)
            .Include(a => a.Transactions)
            .ToListAsync();

        // This is just the real money sitting in your accounts.
        // We'll subtract assignments below once we've loaded them.
        var accountBalance = accounts.Sum(a =>
            a.StartingBalance
            + a.Transactions.Sum(t => t.Inflow ?? 0)
            - a.Transactions.Sum(t => t.Outflow ?? 0));

        // ── Budget rows ──────────────────────────────────────────────────────
        // Load all categories with their subcategories, in custom sort order
        var categories = await _db.Categories
            .Include(c => c.Subcategories)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        // Load all budget assignments and transactions up to the selected month
        // so we can compute cumulative Available to Spend.
        var allAssignments = await _db.BudgetAssignments.ToListAsync();
        var allTransactions = await _db.Transactions.ToListAsync();

        var categoryRows = new List<CategoryBudgetRow>();

        foreach (var category in categories)
        {
            var subRows = new List<SubcategoryBudgetRow>();

            foreach (var sub in category.Subcategories.OrderBy(s => s.SortOrder).ThenBy(s => s.Name))
            {
                // Assigned for the selected month only
                var assigned = allAssignments
                    .Where(a => a.SubcategoryId == sub.Id
                                && a.Month == selectedMonth
                                && a.Year == selectedYear)
                    .Sum(a => a.Amount);

                // Activity for the selected month: sum(Outflow) - sum(Inflow)
                var activity = allTransactions
                    .Where(t => t.SubcategoryId == sub.Id
                                && t.Date.Month == selectedMonth
                                && t.Date.Year == selectedYear)
                    .Sum(t => (t.Outflow ?? 0) - (t.Inflow ?? 0));

                // Available = cumulative (Assigned - Activity) from all months up to selected month
                // This is: sum of all past Assigned - sum of all past Activity
                var cumulativeAssigned = allAssignments
                    .Where(a => a.SubcategoryId == sub.Id
                                && (a.Year < selectedYear
                                    || (a.Year == selectedYear && a.Month <= selectedMonth)))
                    .Sum(a => a.Amount);

                var cumulativeActivity = allTransactions
                    .Where(t => t.SubcategoryId == sub.Id
                                && (t.Date.Year < selectedYear
                                    || (t.Date.Year == selectedYear && t.Date.Month <= selectedMonth)))
                    .Sum(t => (t.Outflow ?? 0) - (t.Inflow ?? 0));

                var available = cumulativeAssigned - cumulativeActivity;

                subRows.Add(new SubcategoryBudgetRow
                {
                    SubcategoryId = sub.Id,
                    SubcategoryName = sub.Name,
                    Assigned = assigned,
                    Activity = activity,
                    Available = available,
                    TargetAmount = sub.TargetAmount,
                    Period = sub.TargetPeriod,
                    TargetDay = sub.TargetDay
                });
            }

            categoryRows.Add(new CategoryBudgetRow
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                Subcategories = subRows
            });
        }

        // Available to Budget = total money in non-investment accounts
        // minus the sum of every subcategory's Available balance.
        // The subcategory Available values already account for all assignments
        // and all activity (both inflows and outflows) cumulatively.
        var availableToBudget = accountBalance - categoryRows.Sum(c => c.TotalAvailable);

        // Load the single app-wide settings row (always Id=1).
        // If it doesn't exist yet (first run before the seed), default to empty settings.
        var settings = await _db.BudgetSettings.FindAsync(1) ?? new BudgetSettings { Id = 1 };

        var viewModel = new BudgetViewModel
        {
            Month = selectedMonth,
            Year = selectedYear,
            AvailableToBudget = availableToBudget,
            Categories = categoryRows,
            ExpectedMonthlyIncome = settings.ExpectedMonthlyIncome
        };

        return View(viewModel);
    }

    // GET: /Budget/SetAssignment?subcategoryId=5&month=3&year=2026
    public async Task<IActionResult> SetAssignment(int subcategoryId, int month, int year)
    {
        var subcategory = await _db.Subcategories
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == subcategoryId);

        if (subcategory == null) return NotFound();

        var existing = await _db.BudgetAssignments
            .FirstOrDefaultAsync(a => a.SubcategoryId == subcategoryId
                                      && a.Month == month
                                      && a.Year == year);

        var assignment = existing ?? new BudgetAssignment
        {
            SubcategoryId = subcategoryId,
            Month = month,
            Year = year,
            Amount = 0
        };

        ViewBag.SubcategoryName = $"{subcategory.Category?.Name} > {subcategory.Name}";
        ViewBag.MonthName = new DateTime(year, month, 1).ToString("MMMM yyyy");
        return View(assignment);
    }

    // POST: /Budget/SetAssignment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAssignment(BudgetAssignment assignment)
    {
        if (!ModelState.IsValid)
        {
            var sub = await _db.Subcategories
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == assignment.SubcategoryId);
            ViewBag.SubcategoryName = $"{sub?.Category?.Name} > {sub?.Name}";
            ViewBag.MonthName = new DateTime(assignment.Year, assignment.Month, 1).ToString("MMMM yyyy");
            return View(assignment);
        }

        var existing = await _db.BudgetAssignments
            .FirstOrDefaultAsync(a => a.SubcategoryId == assignment.SubcategoryId
                                      && a.Month == assignment.Month
                                      && a.Year == assignment.Year);

        if (existing == null)
        {
            _db.BudgetAssignments.Add(assignment);
        }
        else
        {
            existing.Amount = assignment.Amount;
            _db.BudgetAssignments.Update(existing);
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { month = assignment.Month, year = assignment.Year });
    }

    // POST: /Budget/UpdateAssignment
    // This is a JSON API endpoint called by JavaScript when you click-to-edit the Assigned cell.
    // It does the same upsert logic as SetAssignment, but returns Ok() instead of redirecting.
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateAssignment([FromBody] AssignmentUpdate update)
    {
        var existing = await _db.BudgetAssignments
            .FirstOrDefaultAsync(a => a.SubcategoryId == update.SubcategoryId
                                      && a.Month == update.Month
                                      && a.Year == update.Year);
        if (existing == null)
        {
            _db.BudgetAssignments.Add(new BudgetAssignment
            {
                SubcategoryId = update.SubcategoryId,
                Month = update.Month,
                Year = update.Year,
                Amount = update.Amount
            });
        }
        else
        {
            existing.Amount = update.Amount;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    // POST: /Budget/SaveTarget
    // Saves (or clears) the spending target for a subcategory. Called by the modal on the budget page.
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveTarget([FromBody] TargetUpdate update)
    {
        var sub = await _db.Subcategories.FindAsync(update.SubcategoryId);
        if (sub == null) return NotFound();

        sub.TargetAmount = update.TargetAmount;
        sub.TargetPeriod = update.Period;
        sub.TargetDay = update.TargetDay;

        await _db.SaveChangesAsync();
        return Ok();
    }

    // POST: /Budget/SaveSettings
    // Saves the user's expected monthly income. Called by JavaScript on the budget page.
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveSettings([FromBody] SettingsUpdate update)
    {
        var settings = await _db.BudgetSettings.FindAsync(1);
        if (settings == null)
        {
            _db.BudgetSettings.Add(new BudgetSettings { Id = 1, ExpectedMonthlyIncome = update.ExpectedMonthlyIncome });
        }
        else
        {
            settings.ExpectedMonthlyIncome = update.ExpectedMonthlyIncome;
            _db.BudgetSettings.Update(settings);
        }
        await _db.SaveChangesAsync();
        return Ok();
    }
}

// A simple data container for the UpdateAssignment endpoint.
// 'record' is a modern C# shorthand for a class whose only purpose is holding data.
// The properties are defined right in the constructor-like syntax below.
public record AssignmentUpdate(int SubcategoryId, int Month, int Year, decimal Amount);
public record TargetUpdate(int SubcategoryId, decimal? TargetAmount, TargetPeriod? Period, int? TargetDay);
public record SettingsUpdate(decimal? ExpectedMonthlyIncome);
