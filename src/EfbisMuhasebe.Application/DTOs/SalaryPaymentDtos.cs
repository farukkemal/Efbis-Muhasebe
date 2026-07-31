using System;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

public class SalaryPaymentDto
{
    public int Id { get; set; }
    public string PaymentCode { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }
    public decimal TaxDeduction { get; set; }
    public decimal SgkDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal TotalPayment { get; set; }
    public DateTime? PaymentDate { get; set; }
    public SalaryPaymentStatus Status { get; set; }
    public string? Description { get; set; }
    public int? CashAccountId { get; set; }
    
    // Additional mapped properties
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentText { get; set; } = string.Empty;
    public string PeriodText { get; set; } = string.Empty;
    
    public string StatusText => Status switch
    {
        SalaryPaymentStatus.Pending => "Beklemede",
        SalaryPaymentStatus.Paid => "Ödendi",
        SalaryPaymentStatus.Cancelled => "İptal Edildi",
        SalaryPaymentStatus.Partial => "Kısmi Ödeme",
        _ => "Bilinmiyor"
    };

    public string FormattedPaymentDate => PaymentDate?.ToString("dd.MM.yyyy") ?? "-";
}

public class CreateSalaryPaymentDto
{
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }
    public decimal TaxDeduction { get; set; }
    public decimal SgkDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal BonusAmount { get; set; }
    public string? Description { get; set; }
    public int? CashAccountId { get; set; }
}

public class UpdateSalaryPaymentDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }
    public decimal TaxDeduction { get; set; }
    public decimal SgkDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal BonusAmount { get; set; }
    public SalaryPaymentStatus Status { get; set; }
    public string? Description { get; set; }
    public int? CashAccountId { get; set; }
}

public class GeneratePayrollDto
{
    public int Year { get; set; }
    public int Month { get; set; }
}

public class SalaryPaymentFilterDto
{
    public string? SearchTerm { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public EmployeeDepartment? Department { get; set; }
    public SalaryPaymentStatus? Status { get; set; }
    
    // Page/PageNumber dual property pattern for binding
    private int _pageNumber = 1;
    public int Page 
    { 
        get => _pageNumber; 
        set => _pageNumber = value > 0 ? value : 1; 
    }
    public int PageNumber 
    { 
        get => _pageNumber; 
        set => _pageNumber = value > 0 ? value : 1; 
    }
    
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool Ascending { get; set; } = true;
}

public class SalaryDashboardDto
{
    public int TotalRecords { get; set; }
    public int PaidCount { get; set; }
    public int PendingCount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalPendingAmount { get; set; }
    public decimal AverageSalary { get; set; }
}
