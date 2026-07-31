namespace EfbisMuhasebe.Application.DTOs;

/// <summary>Satış ekranı dashboard özet kartları için DTO</summary>
public class SalesDashboardDto
{
    public int TotalProducts { get; set; }
    public int AvailableForSale { get; set; }
    public int NotAvailableForSale { get; set; }
    public int PassiveProducts { get; set; }
    public int CriticalStock { get; set; }
    public int OutOfStock { get; set; }
}
