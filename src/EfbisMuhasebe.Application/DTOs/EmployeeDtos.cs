using System;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string TCKN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public EmployeeDepartment Department { get; set; }
    public string DepartmentText { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
    public string FormattedHireDate { get; set; } = string.Empty;
    public EmployeeStatus Status { get; set; }
    public string StatusText => Status switch
    {
        EmployeeStatus.Active => "Aktif",
        EmployeeStatus.OnLeave => "İzinli",
        EmployeeStatus.Terminated => "Ayrıldı",
        _ => "Bilinmiyor"
    };
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
}

public class CreateEmployeeDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TCKN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public EmployeeDepartment Department { get; set; } = EmployeeDepartment.Sales;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; } = "İstanbul";
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public int? WarehouseId { get; set; }
}

public class UpdateEmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TCKN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public EmployeeDepartment Department { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
    public EmployeeStatus Status { get; set; }
    public int? WarehouseId { get; set; }
}

public class EmployeeFilterDto
{
    public string? SearchTerm { get; set; }
    public EmployeeDepartment? Department { get; set; }
    public EmployeeStatus? Status { get; set; }
    public int? WarehouseId { get; set; }
    private int _page = 1;
    public int Page { get => _page; set => _page = value > 0 ? value : 1; }
    public int PageNumber { get => _page; set => _page = value > 0 ? value : 1; }
    public int PageSize { get; set; } = 15;
    public string? SortBy { get; set; }
    public bool Ascending { get; set; } = true;
}

public class EmployeeDashboardDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int OnLeaveEmployees { get; set; }
    public decimal TotalMonthlySalary { get; set; }
    public int WarehouseStaffCount { get; set; }
    public int CashierStaffCount { get; set; }
    public int SalesStaffCount { get; set; }
    public int ConsultantStaffCount { get; set; }
}
