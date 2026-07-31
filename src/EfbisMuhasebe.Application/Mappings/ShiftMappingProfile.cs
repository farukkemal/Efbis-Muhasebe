using System;
using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.Mappings;

public class ShiftMappingProfile : Profile
{
    public ShiftMappingProfile()
    {
        CreateMap<Shift, ShiftDto>()
            .ForMember(d => d.EmployeeName, opt => opt.MapFrom(s => s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}" : string.Empty))
            .ForMember(d => d.EmployeeCode, opt => opt.MapFrom(s => s.Employee != null ? s.Employee.EmployeeCode : string.Empty))
            .ForMember(d => d.DepartmentText, opt => opt.MapFrom(s => s.Employee != null ? GetDepartmentText(s.Employee.Department) : string.Empty))
            .ForMember(d => d.ShiftTypeText, opt => opt.MapFrom(s => GetShiftTypeText(s.ShiftType)))
            .ForMember(d => d.StatusText, opt => opt.MapFrom(s => GetStatusText(s.Status)))
            .ForMember(d => d.FormattedDate, opt => opt.MapFrom(s => s.ShiftDate.ToString("dd.MM.yyyy")))
            .ForMember(d => d.FormattedStartTime, opt => opt.MapFrom(s => s.StartTime.ToString(@"hh\:mm")))
            .ForMember(d => d.FormattedEndTime, opt => opt.MapFrom(s => s.EndTime.ToString(@"hh\:mm")))
            .ForMember(d => d.FormattedActualStart, opt => opt.MapFrom(s => s.ActualStartTime.HasValue ? s.ActualStartTime.Value.ToString(@"hh\:mm") : "—"))
            .ForMember(d => d.FormattedActualEnd, opt => opt.MapFrom(s => s.ActualEndTime.HasValue ? s.ActualEndTime.Value.ToString(@"hh\:mm") : "—"));

        CreateMap<CreateShiftDto, Shift>()
            .ForMember(d => d.StartTime, opt => opt.MapFrom(s => string.IsNullOrEmpty(s.StartTime) ? default : TimeSpan.Parse(s.StartTime)))
            .ForMember(d => d.EndTime, opt => opt.MapFrom(s => string.IsNullOrEmpty(s.EndTime) ? default : TimeSpan.Parse(s.EndTime)));

        CreateMap<UpdateShiftDto, Shift>()
            .ForMember(d => d.StartTime, opt => opt.MapFrom(s => string.IsNullOrEmpty(s.StartTime) ? default : TimeSpan.Parse(s.StartTime)))
            .ForMember(d => d.EndTime, opt => opt.MapFrom(s => string.IsNullOrEmpty(s.EndTime) ? default : TimeSpan.Parse(s.EndTime)))
            .ForMember(d => d.ActualStartTime, opt => opt.MapFrom(s => string.IsNullOrEmpty(s.ActualStartTime) ? (TimeSpan?)null : TimeSpan.Parse(s.ActualStartTime)))
            .ForMember(d => d.ActualEndTime, opt => opt.MapFrom(s => string.IsNullOrEmpty(s.ActualEndTime) ? (TimeSpan?)null : TimeSpan.Parse(s.ActualEndTime)));
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

    private static string GetShiftTypeText(ShiftType type)
    {
        return type switch
        {
            ShiftType.Morning => "Sabah Vardiyası",
            ShiftType.Afternoon => "Öğle Vardiyası",
            ShiftType.Evening => "Akşam Vardiyası",
            ShiftType.FullDay => "Tam Gün",
            ShiftType.HalfDay => "Yarım Gün",
            _ => type.ToString()
        };
    }

    private static string GetStatusText(ShiftStatus status)
    {
        return status switch
        {
            ShiftStatus.Planned => "Planlandı",
            ShiftStatus.Active => "Aktif",
            ShiftStatus.Completed => "Tamamlandı",
            ShiftStatus.Absent => "Gelmedi / Devamsız",
            ShiftStatus.Cancelled => "İptal",
            _ => status.ToString()
        };
    }
}
