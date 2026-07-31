using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Interfaces;

public record ShiftDashboardStats(int TodayShifts, int ActiveNow, int CompletedToday, int AbsentToday, decimal TotalOvertimeHours, int PlannedThisWeek);

public interface IShiftRepository : IRepository<Shift>
{
    Task<(IEnumerable<Shift> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        string? searchTerm = null, 
        ShiftType? shiftType = null, 
        ShiftStatus? status = null, 
        int? employeeId = null, 
        EmployeeDepartment? department = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        string? sortBy = null, 
        bool ascending = true);

    Task<IEnumerable<Shift>> GetByEmployeeIdAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);
    
    Task<IEnumerable<Shift>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    
    Task<ShiftDashboardStats> GetDashboardStatsAsync(DateTime? date = null);
    
    Task<bool> ExistsAsync(int employeeId, DateTime date, ShiftType type);
}
