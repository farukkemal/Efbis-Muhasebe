using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.Mappings;

public class EmployeeMappingProfile : Profile
{
    public EmployeeMappingProfile()
    {
        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
            .ForMember(dest => dest.FormattedHireDate, opt => opt.MapFrom(src => src.HireDate.ToString("dd.MM.yyyy")))
            .ForMember(dest => dest.DepartmentText, opt => opt.MapFrom(src => GetDepartmentText(src.Department)));

        CreateMap<CreateEmployeeDto, Employee>();
        CreateMap<UpdateEmployeeDto, Employee>();
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
