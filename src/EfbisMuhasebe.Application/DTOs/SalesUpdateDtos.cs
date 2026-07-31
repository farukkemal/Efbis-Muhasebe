namespace EfbisMuhasebe.Application.DTOs;

/// <summary>Toplu satış durumu güncelleme isteği</summary>
public class BulkSaleUpdateDto
{
    public List<int> ProductIds { get; set; } = new();
    public bool IsAvailableForSale { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>Toplu ürün durumu güncelleme isteği</summary>
public class BulkStatusUpdateDto
{
    public List<int> ProductIds { get; set; } = new();
    public int Status { get; set; } // 1=Active, 2=Passive
    public string? UpdatedBy { get; set; }
}

/// <summary>Toplu satış fiyatı güncelleme isteği</summary>
public class BulkPriceUpdateDto
{
    public List<int> ProductIds { get; set; } = new();
    public decimal NewPrice { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>Toplu kategori güncelleme isteği</summary>
public class BulkCategoryUpdateDto
{
    public List<int> ProductIds { get; set; } = new();
    public int CategoryId { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>Tek ürün satış durumu güncelleme isteği</summary>
public class UpdateSaleStatusDto
{
    public int ProductId { get; set; }
    public bool IsAvailableForSale { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>Tek ürün satış fiyatı güncelleme isteği</summary>
public class UpdateSalePriceDto
{
    public int ProductId { get; set; }
    public decimal SalePrice { get; set; }
    public int SaleVatRate { get; set; }
    public bool SaleVatIncluded { get; set; }
}
