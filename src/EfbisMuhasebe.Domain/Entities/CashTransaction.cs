using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;
using System;

namespace EfbisMuhasebe.Domain.Entities;

public class CashTransaction : BaseEntity
{
    public string TransactionCode { get; set; } = string.Empty;
    public int CashAccountId { get; set; }
    public CashTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public int? CustomerId { get; set; }
    public int? InvoiceId { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public CashAccount? CashAccount { get; set; }
    public Customer? Customer { get; set; }
}
