using System;
using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Entities;

public class Employee : BaseEntity
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TCKN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public EmployeeDepartment Department { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; } = "İstanbul";
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public int? WarehouseId { get; set; }

    // Navigation
    public Warehouse? Warehouse { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
