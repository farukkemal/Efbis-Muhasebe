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

public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<Employee> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize,
        string? searchTerm,
        EmployeeDepartment? department,
        EmployeeStatus? status,
        int? warehouseId,
        string? sortBy, bool ascending)
    {
        var query = _dbSet
            .Include(e => e.Warehouse)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(e => e.EmployeeCode.Contains(term) ||
                                     e.FirstName.Contains(term) ||
                                     e.LastName.Contains(term) ||
                                     e.Title.Contains(term));
        }

        if (department.HasValue)
        {
            query = query.Where(e => e.Department == department.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(e => e.WarehouseId == warehouseId.Value);
        }

        query = ascending
            ? query.OrderBy(e => e.EmployeeCode)
            : query.OrderByDescending(e => e.EmployeeCode);

        var totalCount = await query.CountAsync();

        var page = pageNumber > 0 ? pageNumber : 1;
        var size = pageSize > 0 ? pageSize : 15;

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Employee?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .Include(e => e.Warehouse)
            .FirstOrDefaultAsync(e => !e.IsDeleted && e.EmployeeCode == code);
    }

    public async Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null)
    {
        if (excludeId.HasValue)
            return !await _dbSet.AnyAsync(e => !e.IsDeleted && e.EmployeeCode == code && e.Id != excludeId.Value);

        return !await _dbSet.AnyAsync(e => !e.IsDeleted && e.EmployeeCode == code);
    }

    public async Task<EmployeeDashboardStats> GetDashboardStatsAsync()
    {
        var baseQuery = _dbSet.Where(e => !e.IsDeleted);

        var total = await baseQuery.CountAsync();
        var active = await baseQuery.CountAsync(e => e.Status == EmployeeStatus.Active);
        var leave = await baseQuery.CountAsync(e => e.Status == EmployeeStatus.OnLeave);
        var totalSalary = await baseQuery.Where(e => e.Status == EmployeeStatus.Active).SumAsync(e => e.Salary);

        var whCount = await baseQuery.CountAsync(e => e.Department == EmployeeDepartment.Warehouse);
        var cashierCount = await baseQuery.CountAsync(e => e.Department == EmployeeDepartment.Cashier);
        var salesCount = await baseQuery.CountAsync(e => e.Department == EmployeeDepartment.Sales);
        var consultantCount = await baseQuery.CountAsync(e => e.Department == EmployeeDepartment.CustomerService);

        return new EmployeeDashboardStats(total, active, leave, totalSalary, whCount, cashierCount, salesCount, consultantCount);
    }

    public override async Task<Employee?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(e => e.Warehouse)
            .FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == id);
    }
}
