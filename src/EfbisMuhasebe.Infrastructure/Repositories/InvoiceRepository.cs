using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Infrastructure.Repositories;

public class InvoiceRepository : GenericRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<Invoice> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, InvoiceType? invoiceType = null, InvoiceStatus? status = null, int? customerId = null, DateTime? startDate = null, DateTime? endDate = null, string? sortBy = null, bool ascending = true)
    {
        var query = _dbSet.Include(x => x.Customer).AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.InvoiceNumber.Contains(searchTerm) || (x.Customer != null && x.Customer.Title.Contains(searchTerm)));
        }
        if (invoiceType.HasValue) query = query.Where(x => x.InvoiceType == invoiceType.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (startDate.HasValue) query = query.Where(x => x.InvoiceDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(x => x.InvoiceDate <= endDate.Value);

        var totalCount = await query.CountAsync();

        query = sortBy?.ToLower() switch
        {
            "date" => ascending ? query.OrderBy(x => x.InvoiceDate) : query.OrderByDescending(x => x.InvoiceDate),
            "amount" => ascending ? query.OrderBy(x => x.GrandTotal) : query.OrderByDescending(x => x.GrandTotal),
            _ => query.OrderByDescending(x => x.Id)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<Invoice?> GetByIdWithItemsAsync(int id)
    {
        return await _dbSet
            .Include(x => x.Customer)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string number)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.InvoiceNumber == number && !x.IsDeleted);
    }

    public async Task<InvoiceDashboardStats> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var query = _dbSet.Where(x => !x.IsDeleted);
        
        var totalSalesCount = await query.CountAsync(x => x.InvoiceType == InvoiceType.Sales);
        var totalPurchaseCount = await query.CountAsync(x => x.InvoiceType == InvoiceType.Purchase);
        
        var totalSalesAmount = await query.Where(x => x.InvoiceType == InvoiceType.Sales && x.Status != InvoiceStatus.Cancelled).SumAsync(x => x.GrandTotal);
        var totalPurchaseAmount = await query.Where(x => x.InvoiceType == InvoiceType.Purchase && x.Status != InvoiceStatus.Cancelled).SumAsync(x => x.GrandTotal);
        
        var draftCount = await query.CountAsync(x => x.Status == InvoiceStatus.Draft);
        var overdueCount = await query.CountAsync(x => x.Status != InvoiceStatus.Paid && x.Status != InvoiceStatus.Cancelled && x.DueDate.HasValue && x.DueDate.Value < now);

        return new InvoiceDashboardStats(totalSalesCount, totalPurchaseCount, totalSalesAmount, totalPurchaseAmount, draftCount, overdueCount);
    }

    public async Task<string> GetNextInvoiceNumberAsync(InvoiceType type)
    {
        var prefix = type == InvoiceType.Sales ? "SFT" : "AFT";
        var year = DateTime.UtcNow.Year;
        var prefixYear = $"{prefix}-{year}-";
        
        var maxInvoice = await _dbSet
            .Where(x => x.InvoiceNumber.StartsWith(prefixYear))
            .OrderByDescending(x => x.InvoiceNumber)
            .FirstOrDefaultAsync();
            
        int nextNum = 1;
        if (maxInvoice != null)
        {
            var parts = maxInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
            {
                nextNum = lastNum + 1;
            }
        }
        
        return $"{prefixYear}{nextNum:D3}";
    }
}
