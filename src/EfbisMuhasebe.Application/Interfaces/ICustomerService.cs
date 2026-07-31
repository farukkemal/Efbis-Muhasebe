using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

/// <summary>Cari hesaplar servisi arayüzü</summary>
public interface ICustomerService
{
    Task<PagedResultDto<CustomerDto>> GetPagedAsync(CustomerFilterDto filter);
    Task<CustomerDashboardDto> GetDashboardStatsAsync();
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<UpdateCustomerDto?> GetForEditAsync(int id);
    Task<(bool Success, string Message, int? CustomerId)> CreateAsync(CreateCustomerDto dto);
    Task<(bool Success, string Message)> UpdateAsync(UpdateCustomerDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<(bool Success, string Message)> ToggleStatusAsync(int id);
    Task<(bool Success, string Message, int AffectedCount)> BulkUpdateStatusAsync(IEnumerable<int> ids, Domain.Enums.CustomerStatus status);
}
