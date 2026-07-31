using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

/// <summary>Kategori servisi arayüzü</summary>
public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<IEnumerable<CategoryDto>> GetAllWithDetailsAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message)> CreateAsync(CategoryDto dto);
    Task<(bool Success, string Message)> UpdateAsync(CategoryDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
