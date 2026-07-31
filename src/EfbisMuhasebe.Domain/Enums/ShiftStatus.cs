namespace EfbisMuhasebe.Domain.Enums;

public enum ShiftStatus
{
    Planned = 1,     // Planlandı
    Active = 2,      // Aktif (Şu an çalışıyor)
    Completed = 3,   // Tamamlandı
    Absent = 4,      // Gelmedi / Devamsız
    Cancelled = 5    // İptal
}
