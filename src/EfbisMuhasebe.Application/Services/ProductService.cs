using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using FluentValidation;

namespace EfbisMuhasebe.Application.Services;

/// <summary>
/// Ürün iş mantığı servisi.
/// </summary>
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;

    public ProductService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResultDto<ProductDto>> GetPagedProductsAsync(ProductFilterDto filter)
    {
        var (items, totalCount) = await _unitOfWork.Products.GetPagedAsync(
            filter.PageNumber,
            filter.PageSize,
            filter.SearchTerm,
            filter.CategoryId,
            filter.ProductType,
            filter.Status,
            filter.SortBy,
            filter.Ascending);

        var dtos = _mapper.Map<IEnumerable<ProductDto>>(items);

        return new PagedResultDto<ProductDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        return product is null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<UpdateProductDto?> GetForEditAsync(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        return product is null ? null : _mapper.Map<UpdateProductDto>(product);
    }

    public async Task<(bool Success, string Message, int? ProductId)> CreateAsync(CreateProductDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return (false, errors, null);
        }

        var product = _mapper.Map<Product>(dto);

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Ürün başarıyla oluşturuldu.", product.Id);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(UpdateProductDto dto)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return (false, errors);
        }

        var product = await _unitOfWork.Products.GetByIdAsync(dto.Id);
        if (product is null)
            return (false, "Ürün bulunamadı.");

        _mapper.Map(dto, product);
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Ürün başarıyla güncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product is null)
            return (false, "Ürün bulunamadı.");

        // Soft delete
        product.IsDeleted = true;
        product.UpdatedDate = DateTime.UtcNow;
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Ürün başarıyla silindi.");
    }

    public async Task<(bool Success, string Message)> ToggleStatusAsync(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product is null)
            return (false, "Ürün bulunamadı.");

        product.Status = product.Status == ProductStatus.Active
            ? ProductStatus.Passive
            : ProductStatus.Active;
        product.UpdatedDate = DateTime.UtcNow;

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync();

        var statusText = product.Status == ProductStatus.Active ? "aktif" : "pasif";
        return (true, $"Ürün {statusText} durumuna alındı.");
    }

    public async Task<IEnumerable<ProductDto>> GetCriticalStockProductsAsync()
    {
        var products = await _unitOfWork.Products.GetCriticalStockProductsAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }
}
