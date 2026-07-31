using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

/// <summary>
/// Ürün listesi için DTO — görüntüleme amaçlı.
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public ProductType ProductType { get; set; }
    public string ProductTypeDisplay { get; set; } = string.Empty;
    public Unit Unit { get; set; }
    public string UnitDisplay { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public StockStatus StockStatus { get; set; }
    public string StockStatusDisplay { get; set; } = string.Empty;
    public ProductStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public decimal ProfitMarginPercent { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
