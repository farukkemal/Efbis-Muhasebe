namespace EfbisMuhasebe.Domain.Enums;

public enum UserRole
{
    SuperAdmin = 0,     // Sistem Sahibi / Multi-Tenant Genel Yönetim
    Admin = 1,          // Tenant Admin (Müşteri Şirket Yöneticisi)
    StoreManager = 2,   // Mağaza / Şube Müdürü
    Accountant = 3,     // Muhasebe Uzmanı
    Staff = 4           // Müşteri Şirket Personeli (Tenant User)
}
