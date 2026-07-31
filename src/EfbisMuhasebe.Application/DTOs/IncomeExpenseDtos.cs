using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Enums;
using System;
using System.Collections.Generic;

namespace EfbisMuhasebe.Application.DTOs;

public class IncomeExpenseDto
{
    public int Id { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public IncomeExpenseType Type { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
    public int? CashAccountId { get; set; }
    public string? AccountName { get; set; }
}

public class CreateIncomeExpenseDto
{
    public IncomeExpenseType Type { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public int? CashAccountId { get; set; }
}

public class IncomeExpenseFilterDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int? Type { get; set; }
    public string? Category { get; set; }
    public int? CashAccountId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool Ascending { get; set; } = false;
}

public class IncomeExpenseDashboardDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpense { get; set; }
    public int TransactionCount { get; set; }
}

public class MonthlySummaryDto
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Incomes { get; set; } = new();
    public List<decimal> Expenses { get; set; } = new();
}
