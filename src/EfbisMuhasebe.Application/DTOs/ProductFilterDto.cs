namespace EfbisMuhasebe.Application.DTOs;

/// <summary>Ürün listesi filtre ve sayfalama parametreleri</summary>
public class ProductFilterDto
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public int? ProductType { get; set; }
    public int? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "ProductName";
    public bool Ascending { get; set; } = true;
}
