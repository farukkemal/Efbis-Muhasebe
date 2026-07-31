using EfbisMuhasebe.Domain.Entities;

namespace EfbisMuhasebe.Domain.Interfaces;

/// <summary>
/// Ürün'e özgü repository işlemleri.
/// Stok, Satın Alma, Satış modülleri ile entegrasyon için genişletilebilir.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    // ─── Tüm Ürünler sorguları ───────────────────────────────────────────────
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        int? categoryId = null,
        int? productType = null,
        int? status = null,
        string? sortBy = null,
        bool ascending = true);

    Task<Product?> GetByProductCodeAsync(string productCode);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
    Task<IEnumerable<Product>> GetCriticalStockProductsAsync();
    Task<IEnumerable<Product>> GetWithCategoryAsync();
    Task<bool> IsProductCodeUniqueAsync(string productCode, int? excludeId = null);
    Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeId = null);

    // ─── Satışta Olan Ürünler sorguları ─────────────────────────────────────
    Task<(IEnumerable<Product> Items, int TotalCount)> GetSalesPagedAsync(
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
        bool ascending = true);

    Task<SalesDashboardStats> GetSalesDashboardStatsAsync();
    Task<bool> UpdateSaleStatusAsync(int id, bool isAvailable, string? updatedBy = null);
    Task<int> BulkUpdateSaleStatusAsync(IEnumerable<int> ids, bool isAvailable, string? updatedBy = null);
    Task<int> BulkUpdateStatusAsync(IEnumerable<int> ids, Domain.Enums.ProductStatus status, string? updatedBy = null);
    Task<int> BulkUpdateSalePriceAsync(IEnumerable<int> ids, decimal newPrice, string? updatedBy = null);
    Task<int> BulkUpdateCategoryAsync(IEnumerable<int> ids, int categoryId, string? updatedBy = null);
}

/// <summary>
/// Satış ekranı dashboard istatistikleri — tek sorguda hesaplanır.
/// </summary>
public record SalesDashboardStats(
    int TotalProducts,
    int AvailableForSale,
    int NotAvailableForSale,
    int PassiveProducts,
    int CriticalStock,
    int OutOfStock
);
