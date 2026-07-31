using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Interfaces;

/// <summary>
/// Cari Hesap'a özgü repository arayüzü.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        CustomerType? customerType = null,
        CustomerStatus? status = null,
        BalanceStatus? balanceStatus = null,
        string? city = null,
        string? sortBy = null,
        bool ascending = true);

    Task<Customer?> GetByCodeAsync(string customerCode);
    Task<bool> IsCodeUniqueAsync(string customerCode, int? excludeId = null);
    Task<CustomerDashboardStats> GetDashboardStatsAsync();
    Task<int> BulkUpdateStatusAsync(IEnumerable<int> ids, CustomerStatus status);
}

/// <summary>
/// Cari hesaplar dashboard istatistikleri
/// </summary>
public record CustomerDashboardStats(
    int TotalCustomers,
    int CustomersOnly,
    int SuppliersOnly,
    int BothCount,
    int PassiveCount,
    decimal TotalReceivables, // Toplam Alacaklarımız (Balance > 0 toplamı)
    decimal TotalPayables,    // Toplam Borçlarımız (Balance < 0 toplamının mutlak değeri)
    decimal NetBalance        // Net Bakiye
);
