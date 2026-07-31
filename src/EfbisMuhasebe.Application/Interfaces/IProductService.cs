using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

/// <summary>Ürün servisi arayüzü</summary>
public interface IProductService
{
    Task<PagedResultDto<ProductDto>> GetPagedProductsAsync(ProductFilterDto filter);
    Task<ProductDto?> GetByIdAsync(int id);
    Task<UpdateProductDto?> GetForEditAsync(int id);
    Task<(bool Success, string Message, int? ProductId)> CreateAsync(CreateProductDto dto);
    Task<(bool Success, string Message)> UpdateAsync(UpdateProductDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<(bool Success, string Message)> ToggleStatusAsync(int id);
    Task<IEnumerable<ProductDto>> GetCriticalStockProductsAsync();
}
