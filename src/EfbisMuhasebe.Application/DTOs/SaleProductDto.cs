using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

/// <summary>
/// Satışta Olan Ürünler listesi için DTO.
/// Sadece satış yönetimiyle ilgili alanları içerir; alış, stok değiştirme alanları hariç.
/// </summary>
public class SaleProductDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Unit Unit { get; set; }
    public string UnitDisplay { get; set; } = string.Empty;
    public ProductType ProductType { get; set; }
    public string ProductTypeDisplay { get; set; } = string.Empty;

    // Satış bilgileri
    public decimal SalePrice { get; set; }
    public VatRate SaleVatRate { get; set; }
    public decimal SalePriceWithVat { get; set; }

    // Alış (salt okunur — gösterim için)
    public decimal PurchasePrice { get; set; }
    public decimal ProfitMarginPercent { get; set; }

    // Stok (salt okunur)
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public StockStatus StockStatus { get; set; }
    public string StockStatusDisplay { get; set; } = string.Empty;

    // Satış durumu
    public bool IsAvailableForSale { get; set; }
    public DateTime? SaleStatusUpdatedDate { get; set; }
    public string? SaleStatusUpdatedBy { get; set; }

    // Ürün durumu
    public ProductStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;

    // Audit
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
