namespace EfbisMuhasebe.Domain.Enums;

public enum SalaryPaymentStatus
{
    Pending = 1,    // Beklemede
    Paid = 2,       // Ödendi
    Cancelled = 3,  // İptal Edildi
    Partial = 4     // Kısmi Ödeme
}
