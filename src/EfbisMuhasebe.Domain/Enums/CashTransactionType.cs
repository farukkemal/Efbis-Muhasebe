namespace EfbisMuhasebe.Domain.Enums;

public enum CashTransactionType
{
    Collection = 1, // Tahsilat
    Payment = 2, // Tediye
    Transfer = 3, // Virman
    EFT = 4,
    BankTransferIn = 5, // Havale Giriş
    BankTransferOut = 6 // Havale Çıkış
}
