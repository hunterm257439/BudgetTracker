using BudgetTracker.Data;
using BudgetTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Controllers;

public class BankAccountsController : Controller
{
    private readonly AppDbContext _db;

    public BankAccountsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /BankAccounts
    public async Task<IActionResult> Index()
    {
        var accounts = await _db.BankAccounts
            .Include(a => a.Transactions)
            .ToListAsync();

        return View(accounts);
    }

    // GET: /BankAccounts/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var account = await _db.BankAccounts
            .Include(a => a.Transactions)
                .ThenInclude(t => t.Subcategory)
                    .ThenInclude(s => s!.Category)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (account == null) return NotFound();

        return View(account);
    }

    // GET: /BankAccounts/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /BankAccounts/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BankAccount account)
    {
        if (!ModelState.IsValid) return View(account);

        _db.BankAccounts.Add(account);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /BankAccounts/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var account = await _db.BankAccounts.FindAsync(id);
        if (account == null) return NotFound();
        return View(account);
    }

    // POST: /BankAccounts/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BankAccount account)
    {
        if (id != account.Id) return BadRequest();
        if (!ModelState.IsValid) return View(account);

        _db.BankAccounts.Update(account);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /BankAccounts/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var account = await _db.BankAccounts.FindAsync(id);
        if (account == null) return NotFound();
        return View(account);
    }

    // POST: /BankAccounts/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var account = await _db.BankAccounts.FindAsync(id);
        if (account != null)
        {
            _db.BankAccounts.Remove(account);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
