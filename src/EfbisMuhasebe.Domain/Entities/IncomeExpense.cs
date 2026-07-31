using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;
using System;

namespace EfbisMuhasebe.Domain.Entities;

public class IncomeExpense : BaseEntity
{
    public string TransactionCode { get; set; } = string.Empty;
    public IncomeExpenseType Type { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public int? CashAccountId { get; set; }
    public int? CustomerId { get; set; }

    // Navigation
    public CashAccount? CashAccount { get; set; }
    public Customer? Customer { get; set; }
}
