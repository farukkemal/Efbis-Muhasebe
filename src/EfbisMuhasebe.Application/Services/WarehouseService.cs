using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Interfaces;

namespace EfbisMuhasebe.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public WarehouseService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<WarehouseDto> Items, int TotalCount)> GetPagedAsync(WarehouseFilterDto filter)
    {
        var result = await _unitOfWork.Warehouses.GetPagedAsync(
            filter.Page, 
            filter.PageSize, 
            filter.SearchTerm, 
            filter.Status, 
            filter.SortBy, 
            filter.Ascending);
            
        var itemsDto = _mapper.Map<IEnumerable<WarehouseDto>>(result.Items);
        return (itemsDto, result.TotalCount);
    }

    public async Task<WarehouseDto?> GetByIdAsync(int id)
    {
        var entity = await _unitOfWork.Warehouses.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<WarehouseDto>(entity);
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto createDto)
    {
        var isUnique = await _unitOfWork.Warehouses.IsCodeUniqueAsync(createDto.WarehouseCode);
        if (!isUnique)
            throw new Exception("Depo Kodu zaten kullanımda.");

        var entity = _mapper.Map<Warehouse>(createDto);
        
        if (entity.IsDefault)
        {
            var defaultWarehouse = await _unitOfWork.Warehouses.GetDefaultWarehouseAsync();
            if (defaultWarehouse != null)
            {
                defaultWarehouse.IsDefault = false;
                _unitOfWork.Warehouses.Update(defaultWarehouse);
            }
        }

        await _unitOfWork.Warehouses.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<WarehouseDto>(entity);
    }

    public async Task UpdateAsync(int id, UpdateWarehouseDto updateDto)
    {
        var entity = await _unitOfWork.Warehouses.GetByIdAsync(id);
        if (entity == null)
            throw new Exception("Depo bulunamadı.");

        var isUnique = await _unitOfWork.Warehouses.IsCodeUniqueAsync(updateDto.WarehouseCode, id);
        if (!isUnique)
            throw new Exception("Depo Kodu zaten kullanımda.");

        _mapper.Map(updateDto, entity);

        if (entity.IsDefault)
        {
            var defaultWarehouse = await _unitOfWork.Warehouses.GetDefaultWarehouseAsync();
            if (defaultWarehouse != null && defaultWarehouse.Id != id)
            {
                defaultWarehouse.IsDefault = false;
                _unitOfWork.Warehouses.Update(defaultWarehouse);
            }
        }

        _unitOfWork.Warehouses.Update(entity);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _unitOfWork.Warehouses.GetByIdAsync(id);
        if (entity == null)
            throw new Exception("Depo bulunamadı.");

        if (entity.IsDefault)
            throw new Exception("Varsayılan depo silinemez.");

        _unitOfWork.Warehouses.Remove(entity);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<WarehouseDto>> GetAllActiveAsync()
    {
        var items = await _unitOfWork.Warehouses.GetAllActiveAsync();
        return _mapper.Map<IEnumerable<WarehouseDto>>(items);
    }
}
