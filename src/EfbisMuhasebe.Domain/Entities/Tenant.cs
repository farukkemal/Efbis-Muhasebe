using EfbisMuhasebe.Domain.Common;

namespace EfbisMuhasebe.Domain.Entities;

/// <summary>
/// Multi-Tenant Şirket / Kiracı Entity Sınıfı.
/// Her müşteri firmayı temsil eder.
/// </summary>
public class Tenant : BaseEntity
{
    public string TenantCode { get; set; } = string.Empty; // Örn: TEN-001, TEN-002
    public string CompanyName { get; set; } = string.Empty; // Örn: Hilton Taksim A.Ş.
    public string TradeTitle { get; set; } = string.Empty; // Ticari unvan
    public string TaxNumber { get; set; } = string.Empty; // VKN
    public string TaxOffice { get; set; } = string.Empty; // Vergi Dairesi
    public string? Sector { get; set; } // Perakende, Konaklama & Otelcilik
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; } = true;
}
