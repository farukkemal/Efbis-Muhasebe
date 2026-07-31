using EfbisMuhasebe.Domain.Enums;
using System;

namespace EfbisMuhasebe.Application.DTOs;

public class CashAccountDto
{
    public int Id { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public CashAccountType AccountType { get; set; }
    public string? BankName { get; set; }
    public string? Iban { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public CashAccountStatus Status { get; set; }
    public string? Description { get; set; }
}

public class CreateCashAccountDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public CashAccountType AccountType { get; set; }
    public string? BankName { get; set; }
    public string? Iban { get; set; }
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = "TRY";
    public CashAccountStatus Status { get; set; } = CashAccountStatus.Active;
    public string? Description { get; set; }
}

public class UpdateCashAccountDto
{
    public int Id { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public CashAccountType AccountType { get; set; }
    public string? BankName { get; set; }
    public string? Iban { get; set; }
    public string Currency { get; set; } = "TRY";
    public CashAccountStatus Status { get; set; }
    public string? Description { get; set; }
}

public class CashAccountFilterDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public string? SearchTerm { get; set; }
    public int? Type { get; set; }
    public int? Status { get; set; }
    public string? SortBy { get; set; }
    public bool Ascending { get; set; } = true;
}
