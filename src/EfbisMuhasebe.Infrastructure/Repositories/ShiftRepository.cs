using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Infrastructure.Repositories;

public class ShiftRepository : GenericRepository<Shift>, IShiftRepository
{
    public ShiftRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<Shift> Items, int TotalCount)> GetPagedAsync(
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
        bool ascending = true)
    {
        var query = _dbSet
            .Include(x => x.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(x => 
                x.ShiftCode.ToLower().Contains(searchTerm) || 
                (x.Employee != null && (x.Employee.FirstName.ToLower().Contains(searchTerm) || x.Employee.LastName.ToLower().Contains(searchTerm)))
            );
        }

        if (shiftType.HasValue)
            query = query.Where(x => x.ShiftType == shiftType.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (employeeId.HasValue)
            query = query.Where(x => x.EmployeeId == employeeId.Value);

        if (department.HasValue)
            query = query.Where(x => x.Employee != null && x.Employee.Department == department.Value);

        if (startDate.HasValue)
            query = query.Where(x => x.ShiftDate >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(x => x.ShiftDate <= endDate.Value.Date);

        var totalCount = await query.CountAsync();

        query = sortBy?.ToLower() switch
        {
            "shiftcode" => ascending ? query.OrderBy(x => x.ShiftCode) : query.OrderByDescending(x => x.ShiftCode),
            "date" => ascending ? query.OrderBy(x => x.ShiftDate) : query.OrderByDescending(x => x.ShiftDate),
            "employee" => ascending ? query.OrderBy(x => x.Employee!.FirstName) : query.OrderByDescending(x => x.Employee!.FirstName),
            "status" => ascending ? query.OrderBy(x => x.Status) : query.OrderByDescending(x => x.Status),
            _ => query.OrderByDescending(x => x.ShiftDate).ThenBy(x => x.StartTime)
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<Shift>> GetByEmployeeIdAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _dbSet
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId);

        if (startDate.HasValue)
            query = query.Where(x => x.ShiftDate >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(x => x.ShiftDate <= endDate.Value.Date);

        return await query.OrderBy(x => x.ShiftDate).ToListAsync();
    }

    public async Task<IEnumerable<Shift>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(x => x.Employee)
            .Where(x => x.ShiftDate >= startDate.Date && x.ShiftDate <= endDate.Date)
            .OrderBy(x => x.ShiftDate)
            .ToListAsync();
    }

    public async Task<ShiftDashboardStats> GetDashboardStatsAsync(DateTime? date = null)
    {
        var targetDate = (date ?? DateTime.Now).Date;
        
        var startOfWeek = targetDate.AddDays(-(int)targetDate.DayOfWeek + (int)DayOfWeek.Monday);
        if (targetDate.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-7);
        var endOfWeek = startOfWeek.AddDays(7);

        var todayShifts = await _dbSet.Where(x => x.ShiftDate == targetDate).ToListAsync();
        var weekShifts = await _dbSet.Where(x => x.ShiftDate >= startOfWeek && x.ShiftDate < endOfWeek).CountAsync();

        return new ShiftDashboardStats(
            TodayShifts: todayShifts.Count,
            ActiveNow: todayShifts.Count(x => x.Status == ShiftStatus.Active),
            CompletedToday: todayShifts.Count(x => x.Status == ShiftStatus.Completed),
            AbsentToday: todayShifts.Count(x => x.Status == ShiftStatus.Absent),
            TotalOvertimeHours: todayShifts.Sum(x => x.OvertimeHours),
            PlannedThisWeek: weekShifts
        );
    }

    public async Task<bool> ExistsAsync(int employeeId, DateTime date, ShiftType type)
    {
        return await _dbSet.AnyAsync(x => x.EmployeeId == employeeId && x.ShiftDate == date.Date && x.ShiftType == type);
    }
}
