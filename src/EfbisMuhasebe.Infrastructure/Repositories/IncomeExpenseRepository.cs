using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Infrastructure.Repositories;

public class IncomeExpenseRepository : GenericRepository<IncomeExpense>, IIncomeExpenseRepository
{
    public IncomeExpenseRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<IncomeExpense> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, int? type, string? category, int? cashAccountId,
        DateTime? startDate, DateTime? endDate, string? searchTerm, string? sortBy, bool ascending)
    {
        var query = _dbSet.Include(x => x.CashAccount).AsQueryable();

        if (type.HasValue) query = query.Where(x => x.Type == (IncomeExpenseType)type.Value);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.CategoryName == category);
        if (cashAccountId.HasValue) query = query.Where(x => x.CashAccountId == cashAccountId.Value);
        if (startDate.HasValue) query = query.Where(x => x.TransactionDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(x => x.TransactionDate <= endDate.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm)) 
            query = query.Where(x => x.TransactionCode.Contains(searchTerm) || (x.Description != null && x.Description.Contains(searchTerm)));

        var totalCount = await query.CountAsync();
        query = query.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.Id);
        
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<IncomeExpenseDashboardStats> GetDashboardStatsAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _dbSet.AsQueryable();
        
        if (startDate.HasValue) query = query.Where(x => x.TransactionDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(x => x.TransactionDate <= endDate.Value);

        var data = await query.ToListAsync();
        
        var totalIn = data.Where(x => x.Type == IncomeExpenseType.Income).Sum(x => x.Amount);
        var totalOut = data.Where(x => x.Type == IncomeExpenseType.Expense).Sum(x => x.Amount);
        
        var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthData = data.Where(x => x.TransactionDate >= thisMonthStart).ToList();
        var monthIn = monthData.Where(x => x.Type == IncomeExpenseType.Income).Sum(x => x.Amount);
        var monthOut = monthData.Where(x => x.Type == IncomeExpenseType.Expense).Sum(x => x.Amount);
        
        return new IncomeExpenseDashboardStats(totalIn, totalOut, totalIn - totalOut, monthIn, monthOut, data.Count);
    }

    public async Task<Dictionary<int, (decimal Income, decimal Expense)>> GetMonthlySummaryAsync(int year)
    {
        var dict = new Dictionary<int, (decimal Income, decimal Expense)>();
        for (int m = 1; m <= 12; m++) dict[m] = (0, 0);

        // 1. Gelir & Gider işlemleri
        var data = await _dbSet
            .Where(x => x.TransactionDate.Year == year)
            .GroupBy(x => new { x.TransactionDate.Month, x.Type })
            .Select(g => new { 
                Month = g.Key.Month, 
                Type = g.Key.Type, 
                Total = g.Sum(x => x.Amount) 
            })
            .ToListAsync();
            
        foreach (var item in data)
        {
            var current = dict[item.Month];
            if (item.Type == IncomeExpenseType.Income)
                dict[item.Month] = (current.Income + item.Total, current.Expense);
            else
                dict[item.Month] = (current.Income, current.Expense + item.Total);
        }

        // 2. Satış ve Alış Faturaları
        var invData = await _context.Invoices
            .Where(i => i.InvoiceDate.Year == year && i.Status != InvoiceStatus.Cancelled)
            .GroupBy(i => new { i.InvoiceDate.Month, i.InvoiceType })
            .Select(g => new {
                Month = g.Key.Month,
                Type = g.Key.InvoiceType,
                Total = g.Sum(i => i.GrandTotal)
            })
            .ToListAsync();

        foreach (var item in invData)
        {
            var current = dict[item.Month];
            if (item.Type == InvoiceType.Sales)
                dict[item.Month] = (current.Income + item.Total, current.Expense);
            else if (item.Type == InvoiceType.Purchase)
                dict[item.Month] = (current.Income, current.Expense + item.Total);
        }

        return dict;
    }
}
