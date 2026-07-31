using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.Mappings;

public class SalaryPaymentMappingProfile : Profile
{
    public SalaryPaymentMappingProfile()
    {
        CreateMap<SalaryPayment, SalaryPaymentDto>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee != null ? $"{src.Employee.FirstName} {src.Employee.LastName}".Trim() : string.Empty))
            .ForMember(dest => dest.EmployeeCode, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.EmployeeCode : string.Empty))
            .ForMember(dest => dest.DepartmentText, opt => opt.MapFrom(src => src.Employee != null ? GetDepartmentText(src.Employee.Department) : string.Empty))
            .ForMember(dest => dest.PeriodText, opt => opt.MapFrom(src => src.PeriodText))
            .ForMember(dest => dest.FormattedPaymentDate, opt => opt.MapFrom(src => src.PaymentDate.HasValue ? src.PaymentDate.Value.ToString("dd.MM.yyyy") : "—"));

        CreateMap<CreateSalaryPaymentDto, SalaryPayment>();
    }

    private static string GetDepartmentText(EmployeeDepartment dept)
    {
        return dept switch
        {
            EmployeeDepartment.Warehouse => "Depo & Lojistik",
            EmployeeDepartment.Cashier => "Kasa Birimi",
            EmployeeDepartment.Sales => "Reyon & Satış",
            EmployeeDepartment.CustomerService => "Müşteri Danışmanı",
            EmployeeDepartment.Management => "Yönetim & İdari",
            _ => dept.ToString()
        };
    }
}
