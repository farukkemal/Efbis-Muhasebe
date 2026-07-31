using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;

namespace EfbisMuhasebe.Application.Services;

public class SalaryPaymentService : ISalaryPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SalaryPaymentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<SalaryPaymentDto> Items, int TotalCount)> GetPagedAsync(SalaryPaymentFilterDto filter)
    {
        var (items, totalCount) = await _unitOfWork.SalaryPayments.GetPagedAsync(
            filter.PageNumber,
            filter.PageSize,
            filter.SearchTerm,
            filter.Year,
            filter.Month,
            filter.Department,
            filter.Status,
            filter.SortBy,
            filter.Ascending
        );

        var dtos = _mapper.Map<IEnumerable<SalaryPaymentDto>>(items);
        return (dtos, totalCount);
    }

    public async Task<SalaryPaymentDto?> GetByIdAsync(int id)
    {
        var entity = await _unitOfWork.SalaryPayments.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<SalaryPaymentDto>(entity);
    }

    public async Task<SalaryPaymentDto> CreateAsync(CreateSalaryPaymentDto dto)
    {
        var entity = _mapper.Map<SalaryPayment>(dto);
        
        var countForMonth = (await _unitOfWork.SalaryPayments.GetByMonthAsync(dto.Year, dto.Month)).Count();
        entity.PaymentCode = $"MAS-{dto.Year}-{dto.Month:D2}-{(countForMonth + 1):D3}";
        entity.TotalPayment = entity.NetSalary + entity.BonusAmount;
        
        await _unitOfWork.SalaryPayments.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        
        return _mapper.Map<SalaryPaymentDto>(entity);
    }

    public async Task<bool> UpdateAsync(int id, UpdateSalaryPaymentDto dto)
    {
        var entity = await _unitOfWork.SalaryPayments.GetByIdAsync(id);
        if (entity == null) return false;

        entity.GrossSalary = dto.GrossSalary;
        entity.NetSalary = dto.NetSalary;
        entity.TaxDeduction = dto.TaxDeduction;
        entity.SgkDeduction = dto.SgkDeduction;
        entity.OtherDeductions = dto.OtherDeductions;
        entity.BonusAmount = dto.BonusAmount;
        entity.TotalPayment = dto.NetSalary + dto.BonusAmount;
        entity.Status = dto.Status;
        entity.Description = dto.Description;
        entity.CashAccountId = dto.CashAccountId;
        if (dto.Status == SalaryPaymentStatus.Paid && !entity.PaymentDate.HasValue)
        {
            entity.PaymentDate = DateTime.UtcNow;
        }

        _unitOfWork.SalaryPayments.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _unitOfWork.SalaryPayments.GetByIdAsync(id);
        if (entity == null) return false;

        _unitOfWork.SalaryPayments.Remove(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsPaidAsync(int id)
    {
        var entity = await _unitOfWork.SalaryPayments.GetByIdAsync(id);
        if (entity == null || entity.Status != SalaryPaymentStatus.Pending)
            return false;

        entity.Status = SalaryPaymentStatus.Paid;
        entity.PaymentDate = DateTime.UtcNow;
        
        _unitOfWork.SalaryPayments.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelAsync(int id)
    {
        var entity = await _unitOfWork.SalaryPayments.GetByIdAsync(id);
        if (entity == null || entity.Status != SalaryPaymentStatus.Pending)
            return false;

        entity.Status = SalaryPaymentStatus.Cancelled;
        
        _unitOfWork.SalaryPayments.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<int> GenerateMonthlyPayrollAsync(int year, int month)
    {
        var allEmployees = await _unitOfWork.Employees.GetAllAsync();
        var activeEmployees = allEmployees.Where(e => e.Status == EmployeeStatus.Active).ToList();
        
        int generatedCount = 0;
        var existingPayments = (await _unitOfWork.SalaryPayments.GetByMonthAsync(year, month)).ToList();
        int seq = existingPayments.Count + 1;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var emp in activeEmployees)
            {
                if (existingPayments.Any(p => p.EmployeeId == emp.Id))
                    continue;

                decimal salary = emp.Salary;
                
                var payment = new SalaryPayment
                {
                    EmployeeId = emp.Id,
                    Year = year,
                    Month = month,
                    GrossSalary = Math.Round(salary * 1.42m, 2),
                    NetSalary = salary,
                    TaxDeduction = Math.Round(salary * 0.15m, 2),
                    SgkDeduction = Math.Round(salary * 0.14m, 2),
                    BonusAmount = 0m,
                    OtherDeductions = 0m,
                    Status = SalaryPaymentStatus.Pending,
                    PaymentCode = $"MAS-{year}-{month:D2}-{seq:D3}"
                };
                payment.TotalPayment = payment.NetSalary + payment.BonusAmount;

                await _unitOfWork.SalaryPayments.AddAsync(payment);
                generatedCount++;
                seq++;
            }
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return generatedCount;
    }

    public async Task<int> BulkPayAsync(int year, int month)
    {
        var payments = await _unitOfWork.SalaryPayments.GetByMonthAsync(year, month);
        var pendingPayments = payments.Where(p => p.Status == SalaryPaymentStatus.Pending).ToList();
        
        if (!pendingPayments.Any())
            return 0;

        foreach (var payment in pendingPayments)
        {
            payment.Status = SalaryPaymentStatus.Paid;
            payment.PaymentDate = DateTime.UtcNow;
            _unitOfWork.SalaryPayments.Update(payment);
        }

        await _unitOfWork.SaveChangesAsync();
        return pendingPayments.Count;
    }

    public async Task<SalaryDashboardDto> GetDashboardAsync(int? year, int? month)
    {
        var stats = await _unitOfWork.SalaryPayments.GetDashboardStatsAsync(year, month);
        
        return new SalaryDashboardDto
        {
            TotalRecords = stats.TotalRecords,
            PaidCount = stats.PaidCount,
            PendingCount = stats.PendingCount,
            TotalPaidAmount = stats.TotalPaidAmount,
            TotalPendingAmount = stats.TotalPendingAmount,
            AverageSalary = stats.AverageSalary
        };
    }
}
