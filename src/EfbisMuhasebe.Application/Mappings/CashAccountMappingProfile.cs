using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Entities;

namespace EfbisMuhasebe.Application.Mappings;

public class CashAccountMappingProfile : Profile
{
    public CashAccountMappingProfile()
    {
        CreateMap<CashAccount, CashAccountDto>();
        CreateMap<CreateCashAccountDto, CashAccount>();
        CreateMap<UpdateCashAccountDto, CashAccount>();

        CreateMap<CashTransaction, CashTransactionDto>()
            .ForMember(d => d.AccountName, opt => opt.MapFrom(s => s.CashAccount != null ? s.CashAccount.AccountName : ""))
            .ForMember(d => d.CustomerTitle, opt => opt.MapFrom(s => s.Customer != null ? s.Customer.Title : ""))
            .ForMember(d => d.TypeText, opt => opt.MapFrom(s => s.TransactionType.ToString()));
            
        CreateMap<CreateCashTransactionDto, CashTransaction>();
    }
}
