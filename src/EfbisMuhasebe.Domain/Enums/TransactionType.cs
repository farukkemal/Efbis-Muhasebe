namespace EfbisMuhasebe.Domain.Enums;

public enum TransactionType
{
    StockIn = 1,      // Stok Girişi
    StockOut = 2,     // Stok Çıkışı
    Transfer = 3,     // Depo Transferi
    Count = 4,        // Sayım Farkı
    Waste = 5         // Fire
}
