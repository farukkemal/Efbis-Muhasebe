using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Infrastructure.Repositories;

/// <summary>
/// Ürün repository implementasyonu.
/// Hem Tüm Ürünler hem de Satışta Olan Ürünler modüllerini destekler.
/// </summary>
public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    // ─── Tüm Ürünler sorguları ────────────────────────────────────────────────

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        int? categoryId = null,
        int? productType = null,
        int? status = null,
        string? sortBy = null,
        bool ascending = true)
    {
        var query = _dbSet.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(term) ||
                p.ProductCode.ToLower().Contains(term) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(term)));
        }
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);
        if (productType.HasValue)
            query = query.Where(p => (int)p.ProductType == productType.Value);
        if (status.HasValue)
            query = query.Where(p => (int)p.Status == status.Value);

        var totalCount = await query.CountAsync();

        query = (sortBy?.ToLower()) switch
        {
            "productname"   => ascending ? query.OrderBy(p => p.ProductName)   : query.OrderByDescending(p => p.ProductName),
            "productcode"   => ascending ? query.OrderBy(p => p.ProductCode)   : query.OrderByDescending(p => p.ProductCode),
            "purchaseprice" => ascending ? query.OrderBy(p => p.PurchasePrice) : query.OrderByDescending(p => p.PurchasePrice),
            "saleprice"     => ascending ? query.OrderBy(p => p.SalePrice)     : query.OrderByDescending(p => p.SalePrice),
            "currentstock"  => ascending ? query.OrderBy(p => p.CurrentStock)  : query.OrderByDescending(p => p.CurrentStock),
            "createddate"   => ascending ? query.OrderBy(p => p.CreatedDate)   : query.OrderByDescending(p => p.CreatedDate),
            _               => query.OrderBy(p => p.ProductName)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    // ─── Satışta Olan Ürünler sorguları ──────────────────────────────────────

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetSalesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        int? categoryId = null,
        bool? isAvailableForSale = null,
        int? status = null,
        int? stockStatusFilter = null,
        bool onlyBelowMinStock = false,
        bool onlyOutOfStock = false,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        bool ascending = true)
    {
        var query = _dbSet.Include(p => p.Category).AsQueryable();

        // Arama
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(term) ||
                p.ProductCode.ToLower().Contains(term) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(term)) ||
                (p.Category != null && p.Category.Name.ToLower().Contains(term)));
        }

        // Filtreler
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);
        if (isAvailableForSale.HasValue)
            query = query.Where(p => p.IsAvailableForSale == isAvailableForSale.Value);
        if (status.HasValue)
            query = query.Where(p => (int)p.Status == status.Value);

        // Stok durumu filtresi (DB'de computed olmadığı için burada hesaplar)
        if (stockStatusFilter.HasValue)
        {
            query = stockStatusFilter.Value switch
            {
                1 => query.Where(p => p.CurrentStock > 0 && p.CurrentStock > p.MinimumStock), // Sufficient
                2 => query.Where(p => p.CurrentStock > 0 && p.CurrentStock == p.MinimumStock), // Low
                3 => query.Where(p => p.CurrentStock > 0 && p.CurrentStock < p.MinimumStock),  // Critical
                4 => query.Where(p => p.CurrentStock == 0),                                    // OutOfStock
                _ => query
            };
        }
        if (onlyBelowMinStock)
            query = query.Where(p => p.CurrentStock < p.MinimumStock);
        if (onlyOutOfStock)
            query = query.Where(p => p.CurrentStock == 0);

        // Fiyat aralığı
        if (minPrice.HasValue)
            query = query.Where(p => p.SalePrice >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => p.SalePrice <= maxPrice.Value);

        var totalCount = await query.CountAsync();

        // Sıralama
        query = (sortBy?.ToLower()) switch
        {
            "productname"        => ascending ? query.OrderBy(p => p.ProductName)           : query.OrderByDescending(p => p.ProductName),
            "productcode"        => ascending ? query.OrderBy(p => p.ProductCode)           : query.OrderByDescending(p => p.ProductCode),
            "saleprice"          => ascending ? query.OrderBy(p => p.SalePrice)             : query.OrderByDescending(p => p.SalePrice),
            "currentstock"       => ascending ? query.OrderBy(p => p.CurrentStock)          : query.OrderByDescending(p => p.CurrentStock),
            "updateddate"        => ascending ? query.OrderBy(p => p.UpdatedDate)           : query.OrderByDescending(p => p.UpdatedDate),
            "salestatusupdated"  => ascending ? query.OrderBy(p => p.SaleStatusUpdatedDate) : query.OrderByDescending(p => p.SaleStatusUpdatedDate),
            _                    => query.OrderBy(p => p.ProductName)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<SalesDashboardStats> GetSalesDashboardStatsAsync()
    {
        var all = await _dbSet.ToListAsync();

        return new SalesDashboardStats(
            TotalProducts:        all.Count,
            AvailableForSale:     all.Count(p => p.IsAvailableForSale),
            NotAvailableForSale:  all.Count(p => !p.IsAvailableForSale),
            PassiveProducts:      all.Count(p => p.Status == Domain.Enums.ProductStatus.Passive),
            CriticalStock:        all.Count(p => p.CurrentStock > 0 && p.CurrentStock < p.MinimumStock),
            OutOfStock:           all.Count(p => p.CurrentStock == 0)
        );
    }

    public async Task<bool> UpdateSaleStatusAsync(int id, bool isAvailable, string? updatedBy = null)
    {
        var product = await _dbSet.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return false;

        product.IsAvailableForSale = isAvailable;
        product.SaleStatusUpdatedDate = DateTime.UtcNow;
        product.SaleStatusUpdatedBy = updatedBy;
        product.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkUpdateSaleStatusAsync(IEnumerable<int> ids, bool isAvailable, string? updatedBy = null)
    {
        var idList = ids.ToList();
        var products = await _dbSet.Where(p => idList.Contains(p.Id)).ToListAsync();

        foreach (var p in products)
        {
            p.IsAvailableForSale = isAvailable;
            p.SaleStatusUpdatedDate = DateTime.UtcNow;
            p.SaleStatusUpdatedBy = updatedBy;
            p.UpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return products.Count;
    }

    public async Task<int> BulkUpdateStatusAsync(IEnumerable<int> ids, Domain.Enums.ProductStatus status, string? updatedBy = null)
    {
        var idList = ids.ToList();
        var products = await _dbSet.Where(p => idList.Contains(p.Id)).ToListAsync();

        foreach (var p in products)
        {
            p.Status = status;
            p.UpdatedDate = DateTime.UtcNow;
            // Pasife alınınca otomatik satıştan kaldır
            if (status == Domain.Enums.ProductStatus.Passive)
            {
                p.IsAvailableForSale = false;
                p.SaleStatusUpdatedDate = DateTime.UtcNow;
                p.SaleStatusUpdatedBy = updatedBy;
            }
        }

        await _context.SaveChangesAsync();
        return products.Count;
    }

    public async Task<int> BulkUpdateSalePriceAsync(IEnumerable<int> ids, decimal newPrice, string? updatedBy = null)
    {
        var idList = ids.ToList();
        var products = await _dbSet.Where(p => idList.Contains(p.Id)).ToListAsync();

        foreach (var p in products)
        {
            p.SalePrice = newPrice;
            p.UpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return products.Count;
    }

    public async Task<int> BulkUpdateCategoryAsync(IEnumerable<int> ids, int categoryId, string? updatedBy = null)
    {
        var idList = ids.ToList();
        var products = await _dbSet.Where(p => idList.Contains(p.Id)).ToListAsync();

        foreach (var p in products)
        {
            p.CategoryId = categoryId;
            p.UpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return products.Count;
    }

    // ─── Yardımcı metotlar ────────────────────────────────────────────────────

    public async Task<Product?> GetByProductCodeAsync(string productCode)
        => await _dbSet.FirstOrDefaultAsync(p => p.ProductCode == productCode);

    public async Task<Product?> GetByBarcodeAsync(string barcode)
        => await _dbSet.FirstOrDefaultAsync(p => p.Barcode == barcode);

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        => await _dbSet.Include(p => p.Category).Where(p => p.CategoryId == categoryId).ToListAsync();

    public async Task<IEnumerable<Product>> GetCriticalStockProductsAsync()
        => await _dbSet.Include(p => p.Category)
            .Where(p => p.CurrentStock < p.MinimumStock)
            .OrderBy(p => p.CurrentStock)
            .ToListAsync();

    public async Task<IEnumerable<Product>> GetWithCategoryAsync()
        => await _dbSet.Include(p => p.Category).ToListAsync();

    public async Task<bool> IsProductCodeUniqueAsync(string productCode, int? excludeId = null)
    {
        var query = _dbSet.Where(p => p.ProductCode == productCode);
        if (excludeId.HasValue) query = query.Where(p => p.Id != excludeId.Value);
        return !await query.AnyAsync();
    }

    public async Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeId = null)
    {
        var query = _dbSet.Where(p => p.Barcode == barcode);
        if (excludeId.HasValue) query = query.Where(p => p.Id != excludeId.Value);
        return !await query.AnyAsync();
    }
}
