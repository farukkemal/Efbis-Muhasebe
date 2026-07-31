using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;
using System.Collections.Generic;

namespace EfbisMuhasebe.Domain.Entities;

public class CashAccount : BaseEntity
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public CashAccountType AccountType { get; set; }
    public string? BankName { get; set; }
    public string? Iban { get; set; }
    public decimal Balance { get; set; } = 0;
    public string Currency { get; set; } = "TRY";
    public CashAccountStatus Status { get; set; } = CashAccountStatus.Active;
    public string? Description { get; set; }

    // Navigation
    public ICollection<CashTransaction> Transactions { get; set; } = new List<CashTransaction>();
}
