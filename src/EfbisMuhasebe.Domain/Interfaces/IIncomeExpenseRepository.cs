using EfbisMuhasebe.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace EfbisMuhasebe.Domain.Interfaces;

public record IncomeExpenseDashboardStats(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetProfit,
    decimal MonthlyIncome,
    decimal MonthlyExpense,
    int TransactionCount
);

public interface IIncomeExpenseRepository : IRepository<IncomeExpense>
{
    Task<(IEnumerable<IncomeExpense> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, int? type, string? category, int? cashAccountId,
        DateTime? startDate, DateTime? endDate, string? searchTerm, string? sortBy, bool ascending);
        
    Task<IncomeExpenseDashboardStats> GetDashboardStatsAsync(DateTime? startDate, DateTime? endDate);
    
    Task<Dictionary<int, (decimal Income, decimal Expense)>> GetMonthlySummaryAsync(int year);
}
