using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.Mappings;

public class CustomerMappingProfile : Profile
{
    public CustomerMappingProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(dest => dest.CustomerTypeDisplay,
                opt => opt.MapFrom(src => GetCustomerTypeDisplay(src.CustomerType)))
            .ForMember(dest => dest.BalanceStatusDisplay,
                opt => opt.MapFrom(src => GetBalanceStatusDisplay(src.BalanceStatus)))
            .ForMember(dest => dest.StatusDisplay,
                opt => opt.MapFrom(src => src.Status == CustomerStatus.Active ? "Aktif" : "Pasif"));

        CreateMap<CreateCustomerDto, Customer>()
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.InitialBalance))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CustomerStatus.Active))
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

        CreateMap<UpdateCustomerDto, Customer>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Balance, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        CreateMap<Customer, UpdateCustomerDto>();
    }

    private static string GetCustomerTypeDisplay(CustomerType type) => type switch
    {
        CustomerType.Customer => "Müşteri",
        CustomerType.Supplier => "Tedarikçi",
        CustomerType.Both     => "Hem Müşteri Hem Tedarikçi",
        _ => type.ToString()
    };

    private static string GetBalanceStatusDisplay(BalanceStatus status) => status switch
    {
        BalanceStatus.Zero   => "Bakiyesiz",
        BalanceStatus.Debit  => "Borçlu (Alacağımız)",
        BalanceStatus.Credit => "Alacaklı (Borcumuz)",
        _ => status.ToString()
    };
}
