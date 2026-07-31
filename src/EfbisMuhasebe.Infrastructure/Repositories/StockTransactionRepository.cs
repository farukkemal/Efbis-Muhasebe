using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Infrastructure.Repositories;

public class StockTransactionRepository : GenericRepository<StockTransaction>, IStockTransactionRepository
{
    public StockTransactionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<StockTransaction> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? searchTerm, TransactionType? transactionType, 
        int? productId, int? warehouseId, DateTime? startDate, DateTime? endDate, 
        string? sortBy, bool ascending)
    {
        var query = _dbSet
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .Include(x => x.Customer)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.TransactionCode.Contains(searchTerm) || 
                                     (x.Product != null && x.Product.ProductName.Contains(searchTerm)));
        }

        if (transactionType.HasValue)
        {
            query = query.Where(x => x.TransactionType == transactionType.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(x => x.ProductId == productId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.TransactionDate <= endDate.Value);
        }

        query = query.OrderByDescending(x => x.TransactionDate);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<StockTransaction?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.TransactionCode == code);
    }

    public async Task<StockTransactionDashboardStats> GetDashboardStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

        var baseQuery = _dbSet.Where(x => !x.IsDeleted);

        var totalIn = await baseQuery.CountAsync(x => x.TransactionType == TransactionType.StockIn);
        var totalOut = await baseQuery.CountAsync(x => x.TransactionType == TransactionType.StockOut);
        var totalTransfer = await baseQuery.CountAsync(x => x.TransactionType == TransactionType.Transfer);
        var totalWaste = await baseQuery.CountAsync(x => x.TransactionType == TransactionType.Waste);
        
        var todayTx = await baseQuery.CountAsync(x => x.TransactionDate >= today);
        var monthlyTx = await baseQuery.CountAsync(x => x.TransactionDate >= firstDayOfMonth);

        return new StockTransactionDashboardStats(totalIn, totalOut, totalTransfer, totalWaste, todayTx, monthlyTx);
    }

    public async Task<IEnumerable<StockTransaction>> GetByProductIdAsync(int productId)
    {
        return await _dbSet
            .Include(x => x.Warehouse)
            .Where(x => !x.IsDeleted && x.ProductId == productId)
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync();
    }

    public override async Task<StockTransaction?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == id);
    }
}
