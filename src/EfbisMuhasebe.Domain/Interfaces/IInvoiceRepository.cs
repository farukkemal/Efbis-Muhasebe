using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Interfaces;

public record InvoiceDashboardStats(int TotalSalesCount, int TotalPurchaseCount, decimal TotalSalesAmount, decimal TotalPurchaseAmount, int DraftCount, int OverdueCount);

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<(IEnumerable<Invoice> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, InvoiceType? invoiceType = null, InvoiceStatus? status = null, int? customerId = null, DateTime? startDate = null, DateTime? endDate = null, string? sortBy = null, bool ascending = true);
    Task<Invoice?> GetByIdWithItemsAsync(int id);
    Task<Invoice?> GetByInvoiceNumberAsync(string number);
    Task<InvoiceDashboardStats> GetDashboardStatsAsync();
    Task<string> GetNextInvoiceNumberAsync(InvoiceType type);
}
