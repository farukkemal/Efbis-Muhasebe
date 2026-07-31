using System.Collections.Generic;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

public interface IEmployeeService
{
    Task<(IEnumerable<EmployeeDto> Items, int TotalCount)> GetPagedAsync(EmployeeFilterDto filter);
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto);
    Task UpdateAsync(int id, UpdateEmployeeDto dto);
    Task DeleteAsync(int id);
    Task<EmployeeDashboardDto> GetDashboardAsync();
}
