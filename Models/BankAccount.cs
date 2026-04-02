using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.Models;

public enum AccountType
{
    Checking,
    Savings,
    Credit,
    Investment
}

public class BankAccount
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal StartingBalance { get; set; }

    [Required]
    public AccountType AccountType { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
