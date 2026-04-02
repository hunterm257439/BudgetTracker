using BudgetTracker.Data;
using BudgetTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Controllers;

public class TransactionsController : Controller
{
    private readonly AppDbContext _db;

    public TransactionsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /Transactions
    public async Task<IActionResult> Index()
    {
        var transactions = await _db.Transactions
            .Include(t => t.Subcategory)
                .ThenInclude(s => s!.Category)
            .Include(t => t.BankAccount)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        return View(transactions);
    }

    // GET: /Transactions/Create?bankAccountId=5
    public async Task<IActionResult> Create(int? bankAccountId)
    {
        await PopulateDropdowns(selectedBankAccountId: bankAccountId);
        return View(new Transaction { Date = DateTime.Today });
    }

    // POST: /Transactions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Transaction transaction)
    {
        // At least one of Inflow or Outflow must be provided
        if (!transaction.Inflow.HasValue && !transaction.Outflow.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Please enter either an Inflow or Outflow amount.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(transaction.SubcategoryId, transaction.BankAccountId);
            return View(transaction);
        }

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Transactions/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var transaction = await _db.Transactions.FindAsync(id);
        if (transaction == null) return NotFound();

        await PopulateDropdowns(transaction.SubcategoryId, transaction.BankAccountId);
        return View(transaction);
    }

    // POST: /Transactions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Transaction transaction)
    {
        if (id != transaction.Id) return BadRequest();

        if (!transaction.Inflow.HasValue && !transaction.Outflow.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Please enter either an Inflow or Outflow amount.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(transaction.SubcategoryId, transaction.BankAccountId);
            return View(transaction);
        }

        _db.Transactions.Update(transaction);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Transactions/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var transaction = await _db.Transactions
            .Include(t => t.Subcategory)
                .ThenInclude(s => s!.Category)
            .Include(t => t.BankAccount)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null) return NotFound();
        return View(transaction);
    }

    // POST: /Transactions/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var transaction = await _db.Transactions.FindAsync(id);
        if (transaction != null)
        {
            _db.Transactions.Remove(transaction);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: /Transactions/ImportCsv
    // Shows the upload form and instructions for the expected CSV format.
    public async Task<IActionResult> ImportCsv()
    {
        // Load accounts and subcategories so we can show the user valid names in the instructions
        var accounts = await _db.BankAccounts.OrderBy(a => a.Name).ToListAsync();
        var subcategories = await _db.Subcategories
            .Include(s => s.Category)
            .OrderBy(s => s.Category!.Name).ThenBy(s => s.Name)
            .ToListAsync();

        ViewBag.AccountNames = accounts.Select(a => a.Name).ToList();
        ViewBag.SubcategoryNames = subcategories.Select(s => $"{s.Category?.Name} > {s.Name}").ToList();
        return View();
    }

    // POST: /Transactions/ImportCsv
    // Receives the uploaded file, parses each row, and bulk-inserts valid transactions.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportCsv(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please select a CSV file to upload.");
            return View();
        }

        var subcategories = await _db.Subcategories.Include(s => s.Category).ToListAsync();
        var accounts = await _db.BankAccounts.ToListAsync();

        var errors = new List<string>();
        var toImport = new List<Transaction>();

        // StreamReader reads the file as text, line by line.
        // 'using' ensures the file handle is closed when we're done — important for cleanup.
        using var reader = new StreamReader(file.OpenReadStream());

        // Skip the header row (Date,Payee,Account,Subcategory,Outflow,Inflow)
        await reader.ReadLineAsync();

        int lineNumber = 1;
        string? line;

        // ReadLineAsync returns null when there are no more lines
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Split the CSV row into columns. This handles simple CSVs without quoted commas.
            var cols = line.Split(',');

            if (cols.Length < 4)
            {
                errors.Add($"Row {lineNumber}: needs at least 4 columns (Date, Payee, Account, Subcategory).");
                continue; // 'continue' skips to the next loop iteration
            }

            // --- Parse Date ---
            if (!DateTime.TryParse(cols[0].Trim(), out var date))
            {
                errors.Add($"Row {lineNumber}: invalid date '{cols[0].Trim()}'. Use YYYY-MM-DD or MM/DD/YYYY.");
                continue;
            }

            // --- Parse Payee ---
            var payee = cols[1].Trim();
            if (string.IsNullOrWhiteSpace(payee))
            {
                errors.Add($"Row {lineNumber}: Payee is required.");
                continue;
            }

            // --- Match Account by name (case-insensitive) ---
            var accountName = cols[2].Trim();
            var account = accounts.FirstOrDefault(a =>
                a.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase));
            if (account == null)
            {
                errors.Add($"Row {lineNumber}: account '{accountName}' not found. Check the name matches exactly.");
                continue;
            }

            // --- Match Subcategory by name, supporting both "Subcategory" and "Category > Subcategory" ---
            var subSearch = cols[3].Trim();
            Subcategory? subcategory;

            if (subSearch.Contains('>'))
            {
                var parts = subSearch.Split('>', 2); // split into at most 2 pieces
                var catName = parts[0].Trim();
                var subName = parts[1].Trim();
                subcategory = subcategories.FirstOrDefault(s =>
                    s.Category?.Name.Equals(catName, StringComparison.OrdinalIgnoreCase) == true &&
                    s.Name.Equals(subName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                subcategory = subcategories.FirstOrDefault(s =>
                    s.Name.Equals(subSearch, StringComparison.OrdinalIgnoreCase));
            }

            if (subcategory == null)
            {
                errors.Add($"Row {lineNumber}: subcategory '{subSearch}' not found.");
                continue;
            }

            // --- Parse Outflow and Inflow (both optional, but at least one required) ---
            decimal? outflow = null;
            decimal? inflow = null;

            if (cols.Length > 4 && decimal.TryParse(cols[4].Trim(), out var o) && o > 0)
                outflow = o;
            if (cols.Length > 5 && decimal.TryParse(cols[5].Trim(), out var inf) && inf > 0)
                inflow = inf;

            if (!outflow.HasValue && !inflow.HasValue)
            {
                errors.Add($"Row {lineNumber}: must have a value in Outflow or Inflow (or both).");
                continue;
            }

            toImport.Add(new Transaction
            {
                Date = date,
                Payee = payee,
                SubcategoryId = subcategory.Id,
                BankAccountId = account.Id,
                Outflow = outflow,
                Inflow = inflow
            });
        }

        // If any rows had errors, show them all and import nothing.
        // This is safer than partial imports — the user can fix the file and retry.
        if (errors.Any())
        {
            ViewBag.Errors = errors;
            return View();
        }

        // AddRange inserts all records in one database operation — much faster than adding one at a time
        _db.Transactions.AddRange(toImport);
        await _db.SaveChangesAsync();

        // TempData stores a short-lived message that survives exactly one redirect.
        // It's how you pass a "success" message to the next page.
        TempData["Success"] = $"Successfully imported {toImport.Count} transaction(s).";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns(int? selectedSubcategoryId = null, int? selectedBankAccountId = null)
    {
        // Build grouped subcategory list: "Category > Subcategory"
        var subcategories = await _db.Subcategories
            .Include(s => s.Category)
            .OrderBy(s => s.Category!.Name)
            .ThenBy(s => s.Name)
            .ToListAsync();

        ViewBag.Subcategories = subcategories.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.Category?.Name} > {s.Name}",
            Selected = s.Id == selectedSubcategoryId
        }).ToList();

        var accounts = await _db.BankAccounts.OrderBy(a => a.Name).ToListAsync();
        ViewBag.BankAccounts = new SelectList(accounts, "Id", "Name", selectedBankAccountId);
    }
}
