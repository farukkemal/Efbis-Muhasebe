using EfbisMuhasebe.Domain.Entities;

namespace EfbisMuhasebe.Domain.Interfaces;

/// <summary>Kategori repository arayüzü</summary>
public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> GetAllActiveAsync();
    Task<IEnumerable<Category>> GetAllWithDetailsAsync();
    Task<bool> IsNameUniqueAsync(string name, int? excludeId = null);
    Task<bool> HasProductsAsync(int categoryId);
}
