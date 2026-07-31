using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using System.Globalization;

namespace EfbisMuhasebe.Application.Services;

public class IncomeExpenseService : IIncomeExpenseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public IncomeExpenseService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<IncomeExpenseDto>> GetPagedAsync(IncomeExpenseFilterDto filter)
    {
        var result = await _unitOfWork.IncomeExpenses.GetPagedAsync(
            filter.PageNumber, filter.PageSize, filter.Type, filter.Category, filter.CashAccountId,
            filter.StartDate, filter.EndDate, filter.SearchTerm, filter.SortBy, filter.Ascending);

        var dtos = _mapper.Map<List<IncomeExpenseDto>>(result.Items);
        return new PagedResultDto<IncomeExpenseDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<(bool Success, string Message, int? Id)> CreateAsync(CreateIncomeExpenseDto dto)
    {
        var entity = _mapper.Map<IncomeExpense>(dto);
        var prefix = dto.Type == IncomeExpenseType.Income ? "GLR" : "GDR";
        entity.TransactionCode = $"{prefix}-{DateTime.Now:yyyyMMddHHmmssfff}";

        if (dto.CashAccountId.HasValue)
        {
            var account = await _unitOfWork.CashAccounts.GetByIdAsync(dto.CashAccountId.Value);
            if (account != null)
            {
                if (dto.Type == IncomeExpenseType.Income)
                    account.Balance += dto.Amount;
                else
                    account.Balance -= dto.Amount;
                
                _unitOfWork.CashAccounts.Update(account);
            }
        }

        await _unitOfWork.IncomeExpenses.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return (true, "İşlem başarıyla kaydedildi.", entity.Id);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _unitOfWork.IncomeExpenses.GetByIdAsync(id);
        if (entity == null) return (false, "Kayıt bulunamadı.");

        if (entity.CashAccountId.HasValue)
        {
            var account = await _unitOfWork.CashAccounts.GetByIdAsync(entity.CashAccountId.Value);
            if (account != null)
            {
                if (entity.Type == IncomeExpenseType.Income)
                    account.Balance -= entity.Amount;
                else
                    account.Balance += entity.Amount;
                
                _unitOfWork.CashAccounts.Update(account);
            }
        }

        entity.IsDeleted = true;
        _unitOfWork.IncomeExpenses.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return (true, "Kayıt başarıyla silindi.");
    }

    public async Task<IncomeExpenseDashboardDto> GetDashboardAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var stats = await _unitOfWork.IncomeExpenses.GetDashboardStatsAsync(startDate, endDate);
        return new IncomeExpenseDashboardDto
        {
            TotalIncome = stats.TotalIncome,
            TotalExpense = stats.TotalExpense,
            NetProfit = stats.NetProfit,
            MonthlyIncome = stats.MonthlyIncome,
            MonthlyExpense = stats.MonthlyExpense,
            TransactionCount = stats.TransactionCount
        };
    }

    public async Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year)
    {
        var data = await _unitOfWork.IncomeExpenses.GetMonthlySummaryAsync(year);
        var summary = new MonthlySummaryDto();
        var culture = new CultureInfo("tr-TR");
        
        for (int i = 1; i <= 12; i++)
        {
            summary.Labels.Add(new DateTime(year, i, 1).ToString("MMMM", culture));
            if (data.TryGetValue(i, out var monthData))
            {
                summary.Incomes.Add(monthData.Income);
                summary.Expenses.Add(monthData.Expense);
            }
            else
            {
                summary.Incomes.Add(0);
                summary.Expenses.Add(0);
            }
        }
        return summary;
    }
}
