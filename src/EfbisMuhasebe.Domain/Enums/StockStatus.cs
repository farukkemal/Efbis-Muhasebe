namespace EfbisMuhasebe.Domain.Enums;

/// <summary>
/// Stok durumu — sistem tarafından otomatik hesaplanır.
/// Kural: Stok == 0 → OutOfStock | Stok > Min → Sufficient | Stok == Min → Low | Stok &lt; Min → Critical
/// </summary>
public enum StockStatus
{
    Sufficient = 1,  // 🟢 Yeterli
    Low = 2,         // 🟡 Az Stok
    Critical = 3,    // 🔴 Kritik Stok
    OutOfStock = 4   // ⚫ Stok Yok
}
