using BudgetTracker.Data;
using BudgetTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Controllers;

public class CategoriesController : Controller
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /Categories
    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories
            .Include(c => c.Subcategories)
            .OrderBy(c => c.SortOrder)   // respect custom drag-and-drop order
            .ThenBy(c => c.Name)         // alphabetical as a tiebreaker
            .ToListAsync();

        return View(categories);
    }

    // GET: /Categories/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        if (!ModelState.IsValid) return View(category);

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Categories/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();
        return View(category);
    }

    // POST: /Categories/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category category)
    {
        if (id != category.Id) return BadRequest();
        if (!ModelState.IsValid) return View(category);

        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Categories/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories
            .Include(c => c.Subcategories)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();
        return View(category);
    }

    // POST: /Categories/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category != null)
        {
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Categories/ReorderCategories
    // Called by JavaScript after a drag-and-drop. Receives the category IDs in their new order.
    // [FromBody] means the data comes from the request body as JSON, not a form field.
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReorderCategories([FromBody] List<int> orderedIds)
    {
        // Assign each category a SortOrder equal to its position in the new list (0, 1, 2, ...)
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var category = await _db.Categories.FindAsync(orderedIds[i]);
            if (category != null)
                category.SortOrder = i;
        }
        await _db.SaveChangesAsync();
        return Ok(); // returns HTTP 200 with no body — just "it worked"
    }

    // POST: /Categories/ReorderSubcategories
    // Same idea, but for subcategories within a category.
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReorderSubcategories([FromBody] List<int> orderedIds)
    {
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var sub = await _db.Subcategories.FindAsync(orderedIds[i]);
            if (sub != null)
                sub.SortOrder = i;
        }
        await _db.SaveChangesAsync();
        return Ok();
    }
}
