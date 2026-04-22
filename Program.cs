using BudgetTracker.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register the database context with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Auto-create the SQLite database and tables on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Add SortOrder columns if this is an existing database that doesn't have them yet.
    // SQLite will throw an exception if the column already exists, so we catch and ignore that.
    // This is a simple upgrade path — a production app would use proper EF Migrations instead.
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD COLUMN SortOrder INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Subcategories ADD COLUMN SortOrder INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Subcategories ADD COLUMN TargetAmount REAL NULL"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Subcategories ADD COLUMN TargetPeriod INTEGER NULL"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Subcategories ADD COLUMN TargetCustomDays INTEGER NULL"); } catch { }

    // Create the BudgetSettings table if it doesn't exist yet.
    // This stores a single row (Id=1) with app-wide settings like expected income.
    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS BudgetSettings (
                Id INTEGER NOT NULL PRIMARY KEY,
                ExpectedMonthlyIncome REAL NULL
            )");
    }
    catch { }

    // Seed the one-and-only settings row. INSERT OR IGNORE means this is safe to run repeatedly.
    try { db.Database.ExecuteSqlRaw("INSERT OR IGNORE INTO BudgetSettings (Id, ExpectedMonthlyIncome) VALUES (1, NULL)"); } catch { }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Budget}/{action=Index}/{id?}");

app.Run();
