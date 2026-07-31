namespace EfbisMuhasebe.Domain.Enums;

public enum CashAccountType
{
    Kasa = 1,       // Fiziksel Nakit Kasası
    Banka = 2,      // Ticari Banka Mevduat Hesabı
    POS = 3,        // POS Terminal Kasası
    KrediKarti = 4  // Kredi Kartı & Sanal POS Tahsilat Hesabı
}
