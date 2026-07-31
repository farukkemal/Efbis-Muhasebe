using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Interfaces;

namespace EfbisMuhasebe.Application.Services;

/// <summary>Kategori servisi</summary>
public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await _unitOfWork.Categories.GetAllActiveAsync();
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<IEnumerable<CategoryDto>> GetAllWithDetailsAsync()
    {
        var categories = await _unitOfWork.Categories.GetAllWithDetailsAsync();
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        return category is null ? null : _mapper.Map<CategoryDto>(category);
    }

    public async Task<(bool Success, string Message)> CreateAsync(CategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return (false, "Kategori adı zorunludur.");

        var isUnique = await _unitOfWork.Categories.IsNameUniqueAsync(dto.Name);
        if (!isUnique)
            return (false, "Bu kategori adı zaten kullanılmaktadır.");

        var category = new Category
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            ParentId = dto.ParentId > 0 ? dto.ParentId : null,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return (true, $"'{category.Name}' kategorisi oluşturuldu.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(CategoryDto dto)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(dto.Id);
        if (category is null) return (false, "Kategori bulunamadı.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return (false, "Kategori adı zorunludur.");

        var isUnique = await _unitOfWork.Categories.IsNameUniqueAsync(dto.Name, dto.Id);
        if (!isUnique)
            return (false, "Bu kategori adı başka bir kategoride kullanılmaktadır.");

        // Kendi kendisinin üst kategorisi olamaz
        if (dto.ParentId.HasValue && dto.ParentId.Value == dto.Id)
            return (false, "Bir kategori kendi kendisinin üst kategorisi olamaz.");

        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim();
        category.ParentId = dto.ParentId > 0 ? dto.ParentId : null;
        category.UpdatedDate = DateTime.UtcNow;

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync();

        return (true, $"'{category.Name}' kategorisi güncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category is null) return (false, "Kategori bulunamadı.");

        // İş Kuralı: Bağlı ürünü olan kategori direkt silinemez
        var hasProducts = await _unitOfWork.Categories.HasProductsAsync(id);
        if (hasProducts)
            return (false, $"'{category.Name}' kategorisine bağlı ürünler bulunduğu için silinemez. Önce bu ürünlerin kategorisini değiştirin.");

        category.IsDeleted = true;
        category.UpdatedDate = DateTime.UtcNow;

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync();

        return (true, $"'{category.Name}' kategorisi silindi.");
    }
}
