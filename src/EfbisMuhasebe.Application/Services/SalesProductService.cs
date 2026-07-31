using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;

namespace EfbisMuhasebe.Application.Services;

/// <summary>
/// Satışta Olan Ürünler servis implementasyonu.
/// Bu servis yalnızca satış politikalarını yönetir.
/// Stok, alış fiyatı ve ürün silme işlemleri bu serviste yoktur.
/// </summary>
public class SalesProductService : ISalesProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SalesProductService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // ─── Liste & Dashboard ────────────────────────────────────────────────────

    public async Task<PagedResultDto<SaleProductDto>> GetPagedAsync(SaleProductFilterDto filter)
    {
        var (items, totalCount) = await _unitOfWork.Products.GetSalesPagedAsync(
            filter.PageNumber,
            filter.PageSize,
            filter.SearchTerm,
            filter.CategoryId,
            filter.IsAvailableForSale,
            filter.Status,
            filter.StockStatusFilter,
            filter.OnlyBelowMinStock,
            filter.OnlyOutOfStock,
            filter.MinPrice,
            filter.MaxPrice,
            filter.SortBy,
            filter.Ascending);

        return new PagedResultDto<SaleProductDto>
        {
            Items = _mapper.Map<IEnumerable<SaleProductDto>>(items),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<SalesDashboardDto> GetDashboardStatsAsync()
    {
        var stats = await _unitOfWork.Products.GetSalesDashboardStatsAsync();
        return new SalesDashboardDto
        {
            TotalProducts = stats.TotalProducts,
            AvailableForSale = stats.AvailableForSale,
            NotAvailableForSale = stats.NotAvailableForSale,
            PassiveProducts = stats.PassiveProducts,
            CriticalStock = stats.CriticalStock,
            OutOfStock = stats.OutOfStock
        };
    }

    public async Task<SaleProductDto?> GetByIdAsync(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        return product is null ? null : _mapper.Map<SaleProductDto>(product);
    }

    // ─── Tek kayıt işlemleri ──────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> UpdateSaleStatusAsync(UpdateSaleStatusDto dto)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
        if (product is null)
            return (false, "Ürün bulunamadı.");

        // İş Kuralı: Pasif ürün satışa açılamaz
        if (dto.IsAvailableForSale && product.Status == ProductStatus.Passive)
            return (false, "Pasif durumdaki ürün satışa açılamaz. Önce ürünü aktife alın.");

        var updated = await _unitOfWork.Products.UpdateSaleStatusAsync(
            dto.ProductId, dto.IsAvailableForSale, dto.UpdatedBy);

        if (!updated)
            return (false, "Satış durumu güncellenemedi.");

        var statusText = dto.IsAvailableForSale ? "satışa açıldı" : "satıştan kaldırıldı";
        return (true, $"'{product.ProductName}' ürünü {statusText}.");
    }

    public async Task<(bool Success, string Message)> UpdateSalePriceAsync(UpdateSalePriceDto dto)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
        if (product is null)
            return (false, "Ürün bulunamadı.");

        if (dto.SalePrice < 0)
            return (false, "Satış fiyatı negatif olamaz.");

        product.SalePrice = dto.SalePrice;
        product.SaleVatRate = (VatRate)dto.SaleVatRate;
        product.SaleVatIncluded = dto.SaleVatIncluded;
        product.UpdatedDate = DateTime.UtcNow;

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync();

        return (true, $"'{product.ProductName}' satış fiyatı güncellendi.");
    }

    // ─── Toplu işlemler ───────────────────────────────────────────────────────

    public async Task<(bool Success, string Message, int AffectedCount)> BulkUpdateSaleStatusAsync(BulkSaleUpdateDto dto)
    {
        if (dto.ProductIds is null || !dto.ProductIds.Any())
            return (false, "En az bir ürün seçilmelidir.", 0);

        // İş Kuralı: Satışa açılmak isteniyorsa pasif ürünleri filtrele
        if (dto.IsAvailableForSale)
        {
            var passiveIds = new List<int>();
            foreach (var id in dto.ProductIds)
            {
                var p = await _unitOfWork.Products.GetByIdAsync(id);
                if (p?.Status == ProductStatus.Passive) passiveIds.Add(id);
            }

            var eligibleIds = dto.ProductIds.Except(passiveIds).ToList();
            if (!eligibleIds.Any())
                return (false, "Seçilen ürünlerin tamamı pasif durumdadır. Satışa açılamaz.", 0);

            var count = await _unitOfWork.Products.BulkUpdateSaleStatusAsync(
                eligibleIds, dto.IsAvailableForSale, dto.UpdatedBy);

            var msg = passiveIds.Any()
                ? $"{count} ürün satışa açıldı. {passiveIds.Count} pasif ürün atlandı."
                : $"{count} ürün satışa açıldı.";
            return (true, msg, count);
        }
        else
        {
            var count = await _unitOfWork.Products.BulkUpdateSaleStatusAsync(
                dto.ProductIds, dto.IsAvailableForSale, dto.UpdatedBy);
            return (true, $"{count} ürün satıştan kaldırıldı.", count);
        }
    }

    public async Task<(bool Success, string Message, int AffectedCount)> BulkUpdateStatusAsync(BulkStatusUpdateDto dto)
    {
        if (dto.ProductIds is null || !dto.ProductIds.Any())
            return (false, "En az bir ürün seçilmelidir.", 0);

        var targetStatus = (ProductStatus)dto.Status;
        var count = await _unitOfWork.Products.BulkUpdateStatusAsync(dto.ProductIds, targetStatus, dto.UpdatedBy);

        // Pasife alınan ürünler otomatik satıştan kaldırılır
        if (targetStatus == ProductStatus.Passive)
        {
            await _unitOfWork.Products.BulkUpdateSaleStatusAsync(dto.ProductIds, false, dto.UpdatedBy);
        }

        var statusText = targetStatus == ProductStatus.Active ? "aktifleştirildi" : "pasife alındı";
        return (true, $"{count} ürün {statusText}.", count);
    }

    public async Task<(bool Success, string Message, int AffectedCount)> BulkUpdatePriceAsync(BulkPriceUpdateDto dto)
    {
        if (dto.ProductIds is null || !dto.ProductIds.Any())
            return (false, "En az bir ürün seçilmelidir.", 0);
        if (dto.NewPrice < 0)
            return (false, "Fiyat negatif olamaz.", 0);

        var count = await _unitOfWork.Products.BulkUpdateSalePriceAsync(dto.ProductIds, dto.NewPrice, dto.UpdatedBy);
        return (true, $"{count} ürünün satış fiyatı güncellendi.", count);
    }

    public async Task<(bool Success, string Message, int AffectedCount)> BulkUpdateCategoryAsync(BulkCategoryUpdateDto dto)
    {
        if (dto.ProductIds is null || !dto.ProductIds.Any())
            return (false, "En az bir ürün seçilmelidir.", 0);

        var count = await _unitOfWork.Products.BulkUpdateCategoryAsync(dto.ProductIds, dto.CategoryId, dto.UpdatedBy);
        return (true, $"{count} ürünün kategorisi güncellendi.", count);
    }
}
