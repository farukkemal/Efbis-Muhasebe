using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Entities;

/// <summary>
/// Ana ürün entity'si.
/// Stok, Satış, Satın Alma, Fatura, Barkod, Cari Hesap modülleri
/// ile entegre çalışacak şekilde genişletilebilir tasarlanmıştır.
/// </summary>
public class Product : BaseEntity
{
    // ─── Temel Bilgiler ─────────────────────────────────────────────────────────
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int? CategoryId { get; set; }
    public ProductType ProductType { get; set; } = ProductType.StockedProduct;
    public Unit Unit { get; set; } = Unit.Piece;

    // ─── Alış Bilgileri ─────────────────────────────────────────────────────────
    public decimal PurchasePrice { get; set; } = 0;
    public VatRate PurchaseVatRate { get; set; } = VatRate.Twenty;
    public bool PurchaseVatIncluded { get; set; } = false; // false = KDV Hariç, true = KDV Dahil
    public DiscountType DiscountType { get; set; } = DiscountType.None;
    public decimal DiscountValue { get; set; } = 0;

    // ─── Satış Bilgileri ────────────────────────────────────────────────────────
    public decimal SalePrice { get; set; } = 0;
    public VatRate SaleVatRate { get; set; } = VatRate.Twenty;
    public bool SaleVatIncluded { get; set; } = false;

    // ─── Özel Vergiler (Opsiyonel) ──────────────────────────────────────────────
    public SpecialTaxType SpecialTaxType { get; set; } = SpecialTaxType.None; // ÖTV
    public decimal? SpecialTaxValue { get; set; }                             // ÖTV Oranı/Tutarı
    public decimal? CommunicationTaxRate { get; set; }                        // ÖİV Oranı

    // ─── Diğer Bilgiler ──────────────────────────────────────────────────────────
    public string? Description { get; set; }
    public decimal InitialStock { get; set; } = 0;
    public decimal CurrentStock { get; set; } = 0;
    public decimal MinimumStock { get; set; } = 0;

    // ─── Durum ───────────────────────────────────────────────────────────────────
    public ProductStatus Status { get; set; } = ProductStatus.Active;

    // ─── Satış Yönetimi ─────────────────────────────────────────────────────────
    /// <summary>Ürün satışa açık mı? Satış modülleri bu alanı okur.</summary>
    public bool IsAvailableForSale { get; set; } = true;
    public DateTime? SaleStatusUpdatedDate { get; set; }
    public string? SaleStatusUpdatedBy { get; set; }

    // ─── Navigation Properties ───────────────────────────────────────────────────
    public Category? Category { get; set; }

    // ─── Hesaplanan Özellikler (Domain Logic) ────────────────────────────────────

    /// <summary>
    /// Stok durumunu otomatik hesaplar.
    /// Stok > Min → Sufficient | Stok == Min → Low | Stok &lt; Min → Critical
    /// </summary>
    public StockStatus StockStatus
    {
        get
        {
            if (CurrentStock == 0) return StockStatus.OutOfStock;
            if (CurrentStock > MinimumStock) return StockStatus.Sufficient;
            if (CurrentStock == MinimumStock) return StockStatus.Low;
            return StockStatus.Critical;
        }
    }

    /// <summary>
    /// Kâr marjını yüzde olarak hesaplar (gelecek kâr analizi modülü için).
    /// </summary>
    public decimal ProfitMarginPercent
    {
        get
        {
            if (PurchasePrice == 0) return 0;
            return Math.Round(((SalePrice - PurchasePrice) / PurchasePrice) * 100, 2);
        }
    }

    /// <summary>
    /// KDV dahil alış fiyatı
    /// </summary>
    public decimal PurchasePriceWithVat
    {
        get
        {
            if (PurchaseVatIncluded) return PurchasePrice;
            return PurchasePrice * (1 + (int)PurchaseVatRate / 100m);
        }
    }

    /// <summary>
    /// KDV dahil satış fiyatı
    /// </summary>
    public decimal SalePriceWithVat
    {
        get
        {
            if (SaleVatIncluded) return SalePrice;
            return SalePrice * (1 + (int)SaleVatRate / 100m);
        }
    }
}
