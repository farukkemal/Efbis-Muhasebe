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

public class SalaryPaymentRepository : GenericRepository<SalaryPayment>, ISalaryPaymentRepository
{
    public SalaryPaymentRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<SalaryPayment?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<(IEnumerable<SalaryPayment> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? searchTerm, int? year, int? month,
        EmployeeDepartment? department, SalaryPaymentStatus? status,
        string? sortBy, bool ascending)
    {
        var query = _dbSet
            .Include(x => x.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(x =>
                x.PaymentCode.ToLower().Contains(term) ||
                (x.Employee != null && (x.Employee.FirstName + " " + x.Employee.LastName).ToLower().Contains(term)) ||
                (x.Employee != null && x.Employee.EmployeeCode.ToLower().Contains(term)));
        }

        if (year.HasValue) query = query.Where(x => x.Year == year.Value);
        if (month.HasValue) query = query.Where(x => x.Month == month.Value);
        if (department.HasValue) query = query.Where(x => x.Employee != null && x.Employee.Department == department.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        var totalCount = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            query = sortBy.ToLower() switch
            {
                "paymentdate" => ascending ? query.OrderBy(x => x.PaymentDate) : query.OrderByDescending(x => x.PaymentDate),
                "totalpayment" => ascending ? query.OrderBy(x => x.TotalPayment) : query.OrderByDescending(x => x.TotalPayment),
                "netsalary" => ascending ? query.OrderBy(x => x.NetSalary) : query.OrderByDescending(x => x.NetSalary),
                _ => ascending ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id)
            };
        }
        else
        {
            query = query.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ThenByDescending(x => x.Id);
        }

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<SalaryPayment>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _dbSet
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .ToListAsync();
    }

    public async Task<IEnumerable<SalaryPayment>> GetByMonthAsync(int year, int month)
    {
        return await _dbSet
            .Include(x => x.Employee)
            .Where(x => x.Year == year && x.Month == month)
            .ToListAsync();
    }

    public async Task<SalaryDashboardStats> GetDashboardStatsAsync(int? year, int? month)
    {
        var query = _dbSet.AsQueryable();

        if (year.HasValue) query = query.Where(x => x.Year == year.Value);
        if (month.HasValue) query = query.Where(x => x.Month == month.Value);

        var totalRecords = await query.CountAsync();
        var paidCount = await query.CountAsync(x => x.Status == SalaryPaymentStatus.Paid);
        var pendingCount = await query.CountAsync(x => x.Status == SalaryPaymentStatus.Pending);
        var totalPaidAmount = await query.Where(x => x.Status == SalaryPaymentStatus.Paid).SumAsync(x => (decimal?)x.TotalPayment) ?? 0m;
        var totalPendingAmount = await query.Where(x => x.Status == SalaryPaymentStatus.Pending).SumAsync(x => (decimal?)x.TotalPayment) ?? 0m;
        var averageSalary = totalRecords > 0 ? (await query.AverageAsync(x => (decimal?)x.NetSalary) ?? 0m) : 0m;

        return new SalaryDashboardStats(totalRecords, paidCount, pendingCount, totalPaidAmount, totalPendingAmount, averageSalary);
    }

    public async Task<bool> ExistsForMonthAsync(int employeeId, int year, int month)
    {
        return await _dbSet.AnyAsync(x => x.EmployeeId == employeeId && x.Year == year && x.Month == month);
    }
}
