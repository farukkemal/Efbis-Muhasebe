namespace EfbisMuhasebe.Domain.Enums;

public enum ShiftType
{
    Morning = 1,     // Sabah Vardiyası (09:00-17:00)
    Afternoon = 2,   // Öğle Vardiyası (13:00-21:00)
    Evening = 3,     // Akşam Vardiyası (17:00-01:00)
    FullDay = 4,     // Tam Gün (09:00-21:00)
    HalfDay = 5      // Yarım Gün (09:00-13:00)
}
