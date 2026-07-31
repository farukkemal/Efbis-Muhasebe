using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

public interface IIncomeExpenseService
{
    Task<PagedResultDto<IncomeExpenseDto>> GetPagedAsync(IncomeExpenseFilterDto filter);
    Task<(bool Success, string Message, int? Id)> CreateAsync(CreateIncomeExpenseDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<IncomeExpenseDashboardDto> GetDashboardAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year);
}
