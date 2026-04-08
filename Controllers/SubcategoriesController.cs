using BudgetTracker.Data;
using BudgetTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Controllers;

public class SubcategoriesController : Controller
{
    private readonly AppDbContext _db;

    public SubcategoriesController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /Subcategories/Create?categoryId=5
    public async Task<IActionResult> Create(int? categoryId)
    {
        await PopulateCategoriesDropdown(categoryId);
        return View();
    }

    // POST: /Subcategories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Subcategory subcategory)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesDropdown(subcategory.CategoryId);
            return View(subcategory);
        }

        // Put the new subcategory at the end of its category. Nullable cast lets EF Core
        // translate MAX to SQL; returns null (→ 0) when no subcategories exist yet.
        subcategory.SortOrder = (await _db.Subcategories
            .Where(s => s.CategoryId == subcategory.CategoryId)
            .MaxAsync(s => (int?)s.SortOrder) ?? 0) + 1;

        _db.Subcategories.Add(subcategory);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Categories");
    }

    // GET: /Subcategories/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var subcategory = await _db.Subcategories.FindAsync(id);
        if (subcategory == null) return NotFound();

        await PopulateCategoriesDropdown(subcategory.CategoryId);
        return View(subcategory);
    }

    // POST: /Subcategories/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Subcategory subcategory)
    {
        if (id != subcategory.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesDropdown(subcategory.CategoryId);
            return View(subcategory);
        }

        _db.Subcategories.Update(subcategory);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Categories");
    }

    // GET: /Subcategories/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var subcategory = await _db.Subcategories
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subcategory == null) return NotFound();
        return View(subcategory);
    }

    // POST: /Subcategories/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var subcategory = await _db.Subcategories.FindAsync(id);
        if (subcategory != null)
        {
            _db.Subcategories.Remove(subcategory);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index", "Categories");
    }

    private async Task PopulateCategoriesDropdown(int? selectedId = null)
    {
        var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedId);
    }
}
