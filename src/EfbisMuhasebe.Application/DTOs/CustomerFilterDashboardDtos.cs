using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

/// <summary>Cari hesaplar filtreleme parametreleri</summary>
public class CustomerFilterDto
{
    public string? SearchTerm { get; set; }
    public CustomerType? CustomerType { get; set; }
    public CustomerStatus? Status { get; set; }
    public BalanceStatus? BalanceStatus { get; set; }
    public string? City { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "Title";
    public bool Ascending { get; set; } = true;
}

/// <summary>Cari hesaplar dashboard istatistik DTO</summary>
public class CustomerDashboardDto
{
    public int TotalCustomers { get; set; }
    public int CustomersOnly { get; set; }
    public int SuppliersOnly { get; set; }
    public int BothCount { get; set; }
    public int PassiveCount { get; set; }
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }
    public decimal NetBalance { get; set; }
}
