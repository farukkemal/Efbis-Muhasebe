using System.Collections.Generic;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

public interface ISalaryPaymentService
{
    Task<(IEnumerable<SalaryPaymentDto> Items, int TotalCount)> GetPagedAsync(SalaryPaymentFilterDto filter);
    Task<SalaryPaymentDto?> GetByIdAsync(int id);
    Task<SalaryPaymentDto> CreateAsync(CreateSalaryPaymentDto dto);
    Task<bool> UpdateAsync(int id, UpdateSalaryPaymentDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> MarkAsPaidAsync(int id);
    Task<bool> CancelAsync(int id);
    Task<int> GenerateMonthlyPayrollAsync(int year, int month);
    Task<int> BulkPayAsync(int year, int month);
    Task<SalaryDashboardDto> GetDashboardAsync(int? year, int? month);
}
