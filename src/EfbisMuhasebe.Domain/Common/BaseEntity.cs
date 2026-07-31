namespace EfbisMuhasebe.Domain.Common;

/// <summary>
/// Tüm entity'lerin türetileceği temel sınıf.
/// Audit alanları, soft-delete desteği ve Multi-Tenant (TenantId) veri izolasyonu içerir.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 0; // Multi-tenant izolasyon anahtarı (0: SaveChanges'ta oturum açan aktif tenant'a otomatik atanır)
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; } = false;
}
