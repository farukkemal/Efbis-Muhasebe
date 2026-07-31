using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

public interface IInvoiceService
{
    Task<(IEnumerable<InvoiceDto> Items, int TotalCount)> GetPagedAsync(InvoiceFilterDto filter);
    Task<InvoiceDetailDto?> GetByIdAsync(int id);
    Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto);
    Task<bool> UpdateStatusAsync(int id, UpdateInvoiceStatusDto dto);
    Task<InvoiceDashboardDto> GetDashboardAsync();
}
