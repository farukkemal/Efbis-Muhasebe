using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Infrastructure.Repositories;

public class WarehouseRepository : GenericRepository<Warehouse>, IWarehouseRepository
{
    public WarehouseRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<Warehouse> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize,
        string? searchTerm = null,
        WarehouseStatus? status = null,
        string? sortBy = null, bool ascending = true)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.WarehouseCode.Contains(searchTerm) || x.Name.Contains(searchTerm));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalCount = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            query = sortBy.ToLower() switch
            {
                "warehousecode" => ascending ? query.OrderBy(x => x.WarehouseCode) : query.OrderByDescending(x => x.WarehouseCode),
                "name" => ascending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name),
                "city" => ascending ? query.OrderBy(x => x.City) : query.OrderByDescending(x => x.City),
                "status" => ascending ? query.OrderBy(x => x.Status) : query.OrderByDescending(x => x.Status),
                _ => query.OrderByDescending(x => x.Id)
            };
        }
        else
        {
            query = query.OrderByDescending(x => x.Id);
        }

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }

    public async Task<Warehouse?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.WarehouseCode == code);
    }

    public async Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null)
    {
        if (excludeId.HasValue)
            return !await _dbSet.AnyAsync(x => x.WarehouseCode == code && x.Id != excludeId.Value);

        return !await _dbSet.AnyAsync(x => x.WarehouseCode == code);
    }

    public async Task<Warehouse?> GetDefaultWarehouseAsync()
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.IsDefault);
    }

    public async Task<IEnumerable<Warehouse>> GetAllActiveAsync()
    {
        return await _dbSet.Where(x => x.Status == WarehouseStatus.Active).ToListAsync();
    }
}
