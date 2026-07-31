using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

public interface IWarehouseService
{
    Task<(IEnumerable<WarehouseDto> Items, int TotalCount)> GetPagedAsync(WarehouseFilterDto filter);
    Task<WarehouseDto?> GetByIdAsync(int id);
    Task<WarehouseDto> CreateAsync(CreateWarehouseDto createDto);
    Task UpdateAsync(int id, UpdateWarehouseDto updateDto);
    Task DeleteAsync(int id);
    Task<IEnumerable<WarehouseDto>> GetAllActiveAsync();
}
