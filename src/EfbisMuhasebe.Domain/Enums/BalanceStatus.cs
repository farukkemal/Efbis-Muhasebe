namespace EfbisMuhasebe.Domain.Enums;

/// <summary>
/// Bakiye durumu — bakiye tutarına göre otomatik hesaplanır.
/// > 0 → Debit (Borçlu / Alacağımız var)
/// < 0 → Credit (Alacaklı / Borcumuz var)
/// == 0 → Zero (Bakiyesiz)
/// </summary>
public enum BalanceStatus
{
    Zero = 1,    // Bakiyesiz (0 ₺)
    Debit = 2,   // Borçlu (Bize borcu var > 0)
    Credit = 3   // Alacaklı (Ona borcumuz var < 0)
}
