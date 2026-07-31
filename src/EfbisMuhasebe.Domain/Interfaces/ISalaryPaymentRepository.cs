using System.Collections.Generic;
using System.Threading.Tasks;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Interfaces;

public record SalaryDashboardStats(int TotalRecords, int PaidCount, int PendingCount, decimal TotalPaidAmount, decimal TotalPendingAmount, decimal AverageSalary);

public interface ISalaryPaymentRepository : IRepository<SalaryPayment>
{
    Task<(IEnumerable<SalaryPayment> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm, int? year, int? month, EmployeeDepartment? department, SalaryPaymentStatus? status, string? sortBy, bool ascending);
    Task<IEnumerable<SalaryPayment>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<SalaryPayment>> GetByMonthAsync(int year, int month);
    Task<SalaryDashboardStats> GetDashboardStatsAsync(int? year, int? month);
    Task<bool> ExistsForMonthAsync(int employeeId, int year, int month);
}
