using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.Models;

public class Transaction
{
    public int Id { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required]
    [StringLength(200)]
    public string Payee { get; set; } = string.Empty;

    [Required]
    public int SubcategoryId { get; set; }
    public Subcategory? Subcategory { get; set; }

    [Required]
    public int BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1_000_000, ErrorMessage = "Outflow must be between 0 and 1,000,000")]
    public decimal? Outflow { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1_000_000, ErrorMessage = "Inflow must be between 0 and 1,000,000")]
    public decimal? Inflow { get; set; }
}
