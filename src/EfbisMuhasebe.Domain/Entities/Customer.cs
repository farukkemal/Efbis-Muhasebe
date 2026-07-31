using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Entities;

/// <summary>
/// Cari Hesap (Müşteri & Tedarikçi) Entity'si.
/// Ön muhasebe sisteminin cari kart yönetimi, fatura, tahsilat, tediye ve sipariş
/// modülleri ile tam entegre çalışır.
/// </summary>
public class Customer : BaseEntity
{
    // ─── Temel Bilgiler ─────────────────────────────────────────────────────────
    public string CustomerCode { get; set; } = string.Empty; // Cari Kodu (Örn: MŞT-001, TDR-001)
    public string Title { get; set; } = string.Empty;        // Firma Unvanı / Adı Soyadı
    public string? AuthorizedPerson { get; set; }            // Yetkili Kişi
    public CustomerType CustomerType { get; set; } = CustomerType.Customer;

    // ─── Vergi Bilgileri ────────────────────────────────────────────────────────
    public string? TaxOffice { get; set; }                   // Vergi Dairesi
    public string? TaxNumber { get; set; }                    // Vergi No veya TC Kimlik No

    // ─── İletişim Bilgileri ─────────────────────────────────────────────────────
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gsm { get; set; }

    // ─── Adres Bilgileri ────────────────────────────────────────────────────────
    public string? Address { get; set; }
    public string? City { get; set; }                        // İl
    public string? District { get; set; }                    // İlçe

    // ─── Finansal Bilgiler ──────────────────────────────────────────────────────
    /// <summary>
    /// Güncel Bakiye (TL).
    /// > 0 ise Müşterinin Bize Borcu Var (Alacağımız)
    /// < 0 ise Bizim Tedarikçiye Borcumuz Var (Borcumuz)
    /// </summary>
    public decimal Balance { get; set; } = 0;
    public decimal RiskLimit { get; set; } = 0;               // Kredi / Risk Limiti (Opsiyonel)

    // ─── Durum ───────────────────────────────────────────────────────────────────
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public string? Notes { get; set; }

    // ─── Domain Logic (Calculated Properties) ────────────────────────────────────

    /// <summary>
    /// Bakiye durumunu otomatik hesaplar.
    /// </summary>
    public BalanceStatus BalanceStatus
    {
        get
        {
            if (Balance > 0) return BalanceStatus.Debit;   // Borçlu
            if (Balance < 0) return BalanceStatus.Credit;  // Alacaklı
            return BalanceStatus.Zero;                      // Bakiyesiz
        }
    }
}
