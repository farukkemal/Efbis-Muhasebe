using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

/// <summary>
/// Satışta Olan Ürünler modülü servis arayüzü.
/// Yalnızca satış politikalarını yönetir; stok ve alış işlemleri bu serviste yoktur.
/// </summary>
public interface ISalesProductService
{
    // ─── Liste & Dashboard ────────────────────────────────────────────────────
    Task<PagedResultDto<SaleProductDto>> GetPagedAsync(SaleProductFilterDto filter);
    Task<SalesDashboardDto> GetDashboardStatsAsync();
    Task<SaleProductDto?> GetByIdAsync(int id);

    // ─── Tek kayıt işlemleri ──────────────────────────────────────────────────
    /// <summary>
    /// Satış durumunu değiştirir.
    /// Pasif ürün satışa açılamaz — iş kuralı burada uygulanır.
    /// </summary>
    Task<(bool Success, string Message)> UpdateSaleStatusAsync(UpdateSaleStatusDto dto);

    /// <summary>Satış fiyatını ve KDV bilgisini günceller.</summary>
    Task<(bool Success, string Message)> UpdateSalePriceAsync(UpdateSalePriceDto dto);

    // ─── Toplu işlemler ───────────────────────────────────────────────────────
    Task<(bool Success, string Message, int AffectedCount)> BulkUpdateSaleStatusAsync(BulkSaleUpdateDto dto);
    Task<(bool Success, string Message, int AffectedCount)> BulkUpdateStatusAsync(BulkStatusUpdateDto dto);
    Task<(bool Success, string Message, int AffectedCount)> BulkUpdatePriceAsync(BulkPriceUpdateDto dto);
    Task<(bool Success, string Message, int AffectedCount)> BulkUpdateCategoryAsync(BulkCategoryUpdateDto dto);
}
