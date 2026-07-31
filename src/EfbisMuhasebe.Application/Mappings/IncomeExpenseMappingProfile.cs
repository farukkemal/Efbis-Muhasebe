using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Interfaces;

namespace EfbisMuhasebe.Application.Mappings;

public class IncomeExpenseMappingProfile : Profile
{
    public IncomeExpenseMappingProfile()
    {
        CreateMap<IncomeExpense, IncomeExpenseDto>()
            .ForMember(d => d.AccountName, opt => opt.MapFrom(s => s.CashAccount != null ? s.CashAccount.AccountName : ""));
            
        CreateMap<CreateIncomeExpenseDto, IncomeExpense>();
        
        CreateMap<IncomeExpenseDashboardStats, IncomeExpenseDashboardDto>();
    }
}
