using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

/// <summary>Ürün güncelleme DTO</summary>
public class UpdateProductDto
{
    public int Id { get; set; }

    // Temel
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int? CategoryId { get; set; }
    public ProductType ProductType { get; set; } = ProductType.StockedProduct;
    public Unit Unit { get; set; } = Unit.Piece;

    // Alış
    public decimal PurchasePrice { get; set; } = 0;
    public VatRate PurchaseVatRate { get; set; } = VatRate.Twenty;
    public bool PurchaseVatIncluded { get; set; } = false;
    public DiscountType DiscountType { get; set; } = DiscountType.None;
    public decimal DiscountValue { get; set; } = 0;

    // Satış
    public decimal SalePrice { get; set; } = 0;
    public VatRate SaleVatRate { get; set; } = VatRate.Twenty;
    public bool SaleVatIncluded { get; set; } = false;

    // Özel Vergiler
    public SpecialTaxType SpecialTaxType { get; set; } = SpecialTaxType.None;
    public decimal? SpecialTaxValue { get; set; }
    public decimal? CommunicationTaxRate { get; set; }

    // Diğer
    public string? Description { get; set; }
    public decimal MinimumStock { get; set; } = 0;
    public ProductStatus Status { get; set; } = ProductStatus.Active;
}
