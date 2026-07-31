using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Interfaces;

public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<(IEnumerable<Warehouse> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize,
        string? searchTerm = null,
        WarehouseStatus? status = null,
        string? sortBy = null, bool ascending = true);
    Task<Warehouse?> GetByCodeAsync(string code);
    Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null);
    Task<Warehouse?> GetDefaultWarehouseAsync();
    Task<IEnumerable<Warehouse>> GetAllActiveAsync();
}
