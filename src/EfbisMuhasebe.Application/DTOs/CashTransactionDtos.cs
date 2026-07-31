using EfbisMuhasebe.Domain.Enums;
using System;

namespace EfbisMuhasebe.Application.DTOs;

public class CashTransactionDto
{
    public int Id { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public int CashAccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public CashTransactionType TransactionType { get; set; }
    public string TypeText { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerTitle { get; set; }
    public int? InvoiceId { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
}

public class CreateCashTransactionDto
{
    public int CashAccountId { get; set; }
    public CashTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public int? CustomerId { get; set; }
    public int? TargetAccountId { get; set; } // For Transfer/Virman
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}

public class CashTransactionFilterDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int? CashAccountId { get; set; }
    public int? TransactionType { get; set; }
    public int? CustomerId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool Ascending { get; set; } = false;
}

public class CashDashboardDto
{
    public decimal TotalCashBalance { get; set; }
    public decimal TotalBankBalance { get; set; }
    public decimal TotalPosBalance { get; set; }
    public int CashAccountCount { get; set; }
    public int BankAccountCount { get; set; }
    public int PosAccountCount { get; set; }
    public decimal TodayCollections { get; set; }
    public decimal TodayPayments { get; set; }
    public decimal MonthlyCollections { get; set; }
    public decimal MonthlyPayments { get; set; }
}
