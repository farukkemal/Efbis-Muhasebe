using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Interfaces;

public record EmployeeDashboardStats(
    int TotalEmployees,
    int ActiveEmployees,
    int OnLeaveEmployees,
    decimal TotalMonthlySalary,
    int WarehouseStaffCount,
    int CashierStaffCount,
    int SalesStaffCount,
    int ConsultantStaffCount
);

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<(IEnumerable<Employee> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize,
        string? searchTerm = null,
        EmployeeDepartment? department = null,
        EmployeeStatus? status = null,
        int? warehouseId = null,
        string? sortBy = null, bool ascending = true);

    Task<Employee?> GetByCodeAsync(string code);
    Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null);
    Task<EmployeeDashboardStats> GetDashboardStatsAsync();
}
