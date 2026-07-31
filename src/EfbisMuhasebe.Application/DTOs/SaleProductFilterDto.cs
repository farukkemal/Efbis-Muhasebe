namespace EfbisMuhasebe.Application.DTOs;

/// <summary>Satışta Olan Ürünler ekranı filtre parametreleri</summary>
public class SaleProductFilterDto
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public bool? IsAvailableForSale { get; set; }
    public int? Status { get; set; }           // ProductStatus int
    public int? StockStatusFilter { get; set; } // StockStatus int
    public bool OnlyBelowMinStock { get; set; } = false;
    public bool OnlyOutOfStock { get; set; } = false;
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "ProductName";
    public bool Ascending { get; set; } = true;
}
