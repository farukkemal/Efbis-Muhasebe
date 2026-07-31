using System;
using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Entities;

public class SalaryPayment : BaseEntity
{
    public string PaymentCode { get; set; } = string.Empty;  // MAS-2026-07-001
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal GrossSalary { get; set; }       // Brüt maaş
    public decimal NetSalary { get; set; }         // Net maaş
    public decimal TaxDeduction { get; set; }      // Gelir Vergisi kesintisi
    public decimal SgkDeduction { get; set; }      // SGK işçi payı
    public decimal OtherDeductions { get; set; }   // Diğer kesintiler
    public decimal BonusAmount { get; set; }       // Prim/Bonus
    public decimal TotalPayment { get; set; }      // Toplam ödenen tutar
    public DateTime? PaymentDate { get; set; }     // Ödeme tarihi
    public SalaryPaymentStatus Status { get; set; } = SalaryPaymentStatus.Pending;
    public string? Description { get; set; }
    public int? CashAccountId { get; set; }        // Ödemenin yapıldığı hesap
    public string PeriodText => $"{Year} {GetMonthName(Month)}";
    
    // Navigation
    public Employee? Employee { get; set; }
    public CashAccount? CashAccount { get; set; }
    
    private static string GetMonthName(int month) => month switch
    {
        1 => "Ocak", 2 => "Şubat", 3 => "Mart", 4 => "Nisan",
        5 => "Mayıs", 6 => "Haziran", 7 => "Temmuz", 8 => "Ağustos",
        9 => "Eylül", 10 => "Ekim", 11 => "Kasım", 12 => "Aralık",
        _ => "?"
    };
}
