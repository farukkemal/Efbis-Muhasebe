using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Interfaces;

public record StockTransactionDashboardStats(int TotalIn, int TotalOut, int TotalTransfer, int TotalWaste, int TodayTransactions, int MonthlyTransactions);

public interface IStockTransactionRepository : IRepository<StockTransaction>
{
    Task<(IEnumerable<StockTransaction> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? searchTerm, TransactionType? transactionType, 
        int? productId, int? warehouseId, DateTime? startDate, DateTime? endDate, 
        string? sortBy, bool ascending);
    
    Task<StockTransaction?> GetByCodeAsync(string code);
    
    Task<StockTransactionDashboardStats> GetDashboardStatsAsync();
    
    Task<IEnumerable<StockTransaction>> GetByProductIdAsync(int productId);
}
