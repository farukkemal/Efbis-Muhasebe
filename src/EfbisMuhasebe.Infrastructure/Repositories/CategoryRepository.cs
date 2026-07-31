using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Infrastructure.Repositories;

/// <summary>Kategori repository implementasyonu</summary>
public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Category>> GetAllActiveAsync()
        => await _dbSet.OrderBy(c => c.Name).ToListAsync();

    public async Task<IEnumerable<Category>> GetAllWithDetailsAsync()
        => await _dbSet
            .Include(c => c.Parent)
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<bool> IsNameUniqueAsync(string name, int? excludeId = null)
    {
        var query = _dbSet.Where(c => c.Name == name);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return !await query.AnyAsync();
    }

    public async Task<bool> HasProductsAsync(int categoryId)
        => await _context.Products.AnyAsync(p => p.CategoryId == categoryId);
}
